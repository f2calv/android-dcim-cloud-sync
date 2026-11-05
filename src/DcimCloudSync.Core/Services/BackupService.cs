namespace DcimCloudSync.Services;

/// <summary>
/// Copies every discovered media item to the backup destination once, without overwriting or deleting anything.
/// </summary>
/// <remarks>
/// Each item is hashed first, then compared with its existing backup, so a rerun uploads only missing items and an
/// interrupted run resumes without local state. Logs contain counts and outcomes only, never item names or paths.
/// </remarks>
public sealed partial class BackupService(
    ILogger<BackupService> logger,
    TimeProvider timeProvider,
    IMediaSource mediaSource,
    IBackupDestination backupDestination)
{
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
            summary.UnreadableCount,
            summary.Elapsed);
        return summary;
    }

    // TODO: The content is read twice, once to hash and once to upload, so a file modified between the reads is uploaded
    // with the hash of its earlier content. Deferred because camera files are rarely rewritten; hashing during the upload
    // and verifying before commit would close the gap once the destination contract supports a post-upload check.
    private async Task<BackupItemOutcome> BackUpItemAsync(MediaItem item, string sha256, CancellationToken cancellationToken)
    {
        var existing = await backupDestination.FindBackupAsync(item, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var content = await mediaSource.OpenReadAsync(item, cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false))
            {
                if (await backupDestination.TryCreateBackupAsync(item, content, sha256, cancellationToken).ConfigureAwait(false))
                    return BackupItemOutcome.Uploaded;
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ClassName} processed ItemNumber={ItemNumber} Outcome={Outcome}")]
    private static partial void LogItemProcessed(ILogger logger, string className, int itemNumber, BackupItemOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ClassName} could not read ItemNumber={ItemNumber} ExceptionType={ExceptionType}")]
    private static partial void LogItemUnreadable(ILogger logger, string className, int itemNumber, string exceptionType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{ClassName} backup completed ItemCount={ItemCount} UploadedCount={UploadedCount} AlreadyBackedUpCount={AlreadyBackedUpCount} ConflictCount={ConflictCount} UnverifiedCount={UnverifiedCount} UnreadableCount={UnreadableCount} Elapsed={Elapsed}")]
    private static partial void LogRunCompleted(
        ILogger logger,
        string className,
        int itemCount,
        int uploadedCount,
        int alreadyBackedUpCount,
        int conflictCount,
        int unverifiedCount,
        int unreadableCount,
        TimeSpan elapsed);
}
