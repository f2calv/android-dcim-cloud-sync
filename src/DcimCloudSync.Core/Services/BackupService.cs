namespace DcimCloudSync.Services;

/// <summary>
/// Copies every discovered media item to the backup destination once, without overwriting or deleting anything.
/// </summary>
/// <remarks>
/// Each item is hashed first, then compared with its existing backup, so a rerun uploads only missing items and an
/// interrupted run resumes without local state. The upload re-hashes the content as it streams and fails before commit when
/// the item changed after hashing. Logs contain counts and outcomes only, never item names or paths.
/// </remarks>
public sealed partial class BackupService(
    ILogger<BackupService> logger,
    TimeProvider timeProvider,
    IMediaSource mediaSource,
    IBackupDestination backupDestination)
{
    /// <summary>
    /// Gets a value indicating whether the backup destination is configured, so a run can start.
    /// </summary>
    public bool IsAvailable => backupDestination.IsConfigured;

    /// <summary>
    /// Backs up every item visible to the media source.
    /// </summary>
    /// <param name="progress">Receives a report after each item, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the run between and during reads and uploads.</param>
    /// <returns>A summary that identifies no individual item.</returns>
    /// <exception cref="Exceptions.BackupDestinationException">The destination failed; the run stops.</exception>
    public async Task<BackupSummary> BackUpAsync(IProgress<BackupProgress>? progress, CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var counts = new Dictionary<BackupItemOutcome, int>();
        var itemCount = 0;
        var unreadableCount = 0;
        var uploadedBytes = 0L;

        await foreach (var item in mediaSource.DiscoverAsync(cancellationToken).ConfigureAwait(false))
        {
            itemCount++;
            try
            {
                var (sha256, length) = await MediaHash.ComputeAsync(mediaSource, item, cancellationToken).ConfigureAwait(false);
                var outcome = await BackUpItemAsync(item, sha256, cancellationToken).ConfigureAwait(false);
                counts[outcome] = counts.GetValueOrDefault(outcome) + 1;
                if (outcome == BackupItemOutcome.Uploaded)
                    uploadedBytes += length;

                LogItemProcessed(logger, nameof(BackupService), itemCount, outcome);
            }
            catch (Exception ex) when (IsMediaChanged(ex))
            {
                counts[BackupItemOutcome.Changed] = counts.GetValueOrDefault(BackupItemOutcome.Changed) + 1;
                LogItemProcessed(logger, nameof(BackupService), itemCount, BackupItemOutcome.Changed);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadableCount++;
                LogItemUnreadable(logger, nameof(BackupService), itemCount, ex.GetType().Name);
            }

            progress?.Report(new BackupProgress(itemCount, counts.GetValueOrDefault(BackupItemOutcome.Uploaded), uploadedBytes));
        }

        var summary = new BackupSummary(
            itemCount,
            counts.GetValueOrDefault(BackupItemOutcome.Uploaded),
            counts.GetValueOrDefault(BackupItemOutcome.AlreadyBackedUp),
            counts.GetValueOrDefault(BackupItemOutcome.Conflict),
            counts.GetValueOrDefault(BackupItemOutcome.Unverified),
            counts.GetValueOrDefault(BackupItemOutcome.Changed),
            unreadableCount,
            uploadedBytes,
            timeProvider.GetElapsedTime(startedAt));
        LogRunCompleted(
            logger,
            nameof(BackupService),
            summary.ItemCount,
            summary.UploadedCount,
            summary.AlreadyBackedUpCount,
            summary.ConflictCount,
            summary.UnverifiedCount,
            summary.ChangedCount,
            summary.UnreadableCount,
            summary.Elapsed);
        return summary;
    }

    // TODO: New items are read twice, once to hash for the lookup and once to upload. A destination that sets the hash at
    // commit, such as Azure staged blocks with metadata on Put Block List, could hash during a single upload read; deferred
    // until throughput on large videos shows the second read matters.
    private async Task<BackupItemOutcome> BackUpItemAsync(MediaItem item, string sha256, CancellationToken cancellationToken)
    {
        var existing = await backupDestination.FindBackupAsync(item, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var content = await mediaSource.OpenReadAsync(item, cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false))
            {
                var verifiedContent = new HashVerifyingStream(content, sha256);
                await using (verifiedContent.ConfigureAwait(false))
                {
                    if (await backupDestination.TryCreateBackupAsync(item, verifiedContent, sha256, cancellationToken).ConfigureAwait(false))
                        return BackupItemOutcome.Uploaded;
                }
            }

            // Another writer created the backup between the lookup and the upload.
            existing = await backupDestination.FindBackupAsync(item, cancellationToken).ConfigureAwait(false);
        }

        return existing?.Sha256 switch
        {
            null => BackupItemOutcome.Unverified,
            var recorded when string.Equals(recorded, sha256, StringComparison.OrdinalIgnoreCase) => BackupItemOutcome.AlreadyBackedUp,
            _ => BackupItemOutcome.Conflict,
        };
    }

    private static bool IsMediaChanged(Exception exception) => exception switch
    {
        MediaChangedException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(IsMediaChanged),
        _ => exception.InnerException is { } inner && IsMediaChanged(inner),
    };

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ClassName} processed ItemNumber={ItemNumber} Outcome={Outcome}")]
    private static partial void LogItemProcessed(ILogger logger, string className, int itemNumber, BackupItemOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ClassName} could not read ItemNumber={ItemNumber} ExceptionType={ExceptionType}")]
    private static partial void LogItemUnreadable(ILogger logger, string className, int itemNumber, string exceptionType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{ClassName} backup completed ItemCount={ItemCount} UploadedCount={UploadedCount} AlreadyBackedUpCount={AlreadyBackedUpCount} ConflictCount={ConflictCount} UnverifiedCount={UnverifiedCount} ChangedCount={ChangedCount} UnreadableCount={UnreadableCount} Elapsed={Elapsed}")]
    private static partial void LogRunCompleted(
        ILogger logger,
        string className,
        int itemCount,
        int uploadedCount,
        int alreadyBackedUpCount,
        int conflictCount,
        int unverifiedCount,
        int changedCount,
        int unreadableCount,
        TimeSpan elapsed);
}
