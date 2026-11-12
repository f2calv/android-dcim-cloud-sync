using Microsoft.Extensions.Options;

namespace CasCap.Services;

/// <summary>
/// Copies every discovered media item to the backup destination once, without overwriting or deleting anything.
/// </summary>
/// <remarks>
/// Each item's existing backup is looked up first. In <see cref="BackupCheckMode.Quick"/> mode, a backup whose recorded
/// source size and modification time match the item counts as backed up without reading the item. Otherwise the item is
/// hashed and compared, and a missing backup is uploaded; the upload re-hashes the content as it streams and fails before
/// commit when the item changed after hashing. A rerun uploads only missing items, so an interrupted run resumes without
/// local state. Runs are serialized, so a run started while another is in progress, such as a background run and a manual
/// one, waits for it. With a change-tracking source and a checkpoint store, a run that read every item it discovered records
/// a checkpoint, and <see cref="BackUpChangesAsync"/> then reads only items changed since. Logs contain counts and outcomes
/// only, never item names or paths; the optional on-device <see cref="IBackupLog"/> also records file names.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The clock for run durations and log entries.</param>
/// <param name="mediaSource">The media source; implement <see cref="IChangeTrackingMediaSource"/> to allow incremental runs.</param>
/// <param name="backupDestination">The backup destination.</param>
/// <param name="checkpointStore">Persists the incremental checkpoint, or <see langword="null"/> to always run in full.</param>
/// <param name="config">Selects the check mode, or <see langword="null"/> for <see cref="BackupCheckMode.Quick"/>.</param>
/// <param name="backupLog">Records events for the user on the device, or <see langword="null"/>.</param>
public sealed partial class BackupService(
    ILogger<BackupService> logger,
    TimeProvider timeProvider,
    IMediaSource mediaSource,
    IBackupDestination backupDestination,
    IBackupCheckpointStore? checkpointStore = null,
    IOptionsMonitor<BackupConfig>? config = null,
    IBackupLog? backupLog = null) : IDisposable
{
    private readonly SemaphoreSlim _runLock = new(1, 1);

    /// <summary>
    /// Gets a value indicating whether the backup destination is configured, so a run can start.
    /// </summary>
    public bool IsAvailable => backupDestination.IsConfigured;

    /// <summary>
    /// Backs up every item visible to the media source, after any run already in progress completes.
    /// </summary>
    /// <param name="progress">Receives a report as each item starts, at least every mebibyte read, and after it, or
    /// <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the wait for another run, and the run between and during reads and uploads.</param>
    /// <returns>A summary that identifies no individual item.</returns>
    /// <exception cref="Exceptions.BackupDestinationException">The destination failed; the run stops.</exception>
    public Task<BackupSummary> BackUpAsync(IProgress<BackupProgress>? progress, CancellationToken cancellationToken) =>
        RunExclusiveAsync(incremental: false, progress, cancellationToken);

    /// <summary>
    /// Backs up the items added or modified since the last recorded checkpoint, or every item when there is none.
    /// </summary>
    /// <param name="progress">Receives a report as each item starts, at least every mebibyte read, and after it, or
    /// <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the wait for another run, and the run between and during reads and uploads.</param>
    /// <returns>A summary that identifies no individual item.</returns>
    /// <exception cref="Exceptions.BackupDestinationException">The destination failed; the run stops.</exception>
    public Task<BackupSummary> BackUpChangesAsync(IProgress<BackupProgress>? progress, CancellationToken cancellationToken) =>
        RunExclusiveAsync(incremental: true, progress, cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _runLock.Dispose();

    private async Task<BackupSummary> RunExclusiveAsync(bool incremental, IProgress<BackupProgress>? progress, CancellationToken cancellationToken)
    {
        await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var trackingSource = checkpointStore is null ? null : mediaSource as IChangeTrackingMediaSource;
            var startToken = trackingSource?.GetChangeToken();
            var sinceToken = incremental ? checkpointStore?.Load() : null;
            var items = trackingSource is not null && sinceToken is not null
                ? trackingSource.DiscoverChangedAsync(sinceToken, cancellationToken)
                : mediaSource.DiscoverAsync(cancellationToken);
            var mode = config?.CurrentValue.CheckMode ?? BackupCheckMode.Quick;
            Record(BackupLogLevel.Information, $"Backup started: {(sinceToken is null ? "all items" : "changed items")}, {mode} check.");

            BackupSummary summary;
            try
            {
                summary = await RunAsync(items, sinceToken is not null, mode, progress, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Record(BackupLogLevel.Warning, "Backup canceled.");
                throw;
            }
            catch (BackupDestinationException ex)
            {
                Record(BackupLogLevel.Error, $"Backup stopped: {ex.Message}");
                throw;
            }

            Record(
                summary.UnreadableCount + summary.ConflictCount > 0 ? BackupLogLevel.Warning : BackupLogLevel.Information,
                $"Backup finished: {summary.ItemCount} checked, {summary.UploadedCount} uploaded, {summary.AlreadyBackedUpCount} already backed up, "
                    + $"{summary.ConflictCount} conflicts, {summary.UnverifiedCount} unverified, {summary.ChangedCount} changed, "
                    + $"{summary.UnreadableCount} unreadable in {summary.Elapsed.TotalSeconds:N1} s.");

            // An unreadable item would be skipped for good if the checkpoint moved past it.
            if (startToken is not null && summary.UnreadableCount == 0)
                checkpointStore!.Save(startToken);
            return summary;
        }
        finally
        {
            _ = _runLock.Release();
        }
    }

    private async Task<BackupSummary> RunAsync(
        IAsyncEnumerable<MediaItem> items,
        bool incremental,
        BackupCheckMode mode,
        IProgress<BackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var counts = new Dictionary<BackupItemOutcome, int>();
        var itemCount = 0;
        var unreadableCount = 0;
        var uploadedBytes = 0L;

        await foreach (var item in items.ConfigureAwait(false))
        {
            itemCount++;
            var run = new ItemProgress(progress, itemCount, counts.GetValueOrDefault(BackupItemOutcome.Uploaded), uploadedBytes, item);
            try
            {
                var (outcome, length) = await BackUpItemAsync(item, mode, run, cancellationToken).ConfigureAwait(false);
                counts[outcome] = counts.GetValueOrDefault(outcome) + 1;
                if (outcome == BackupItemOutcome.Uploaded)
                    uploadedBytes += length;

                LogItemProcessed(logger, nameof(BackupService), itemCount, outcome);
                Record(outcome is BackupItemOutcome.Conflict ? BackupLogLevel.Warning : BackupLogLevel.Information, Describe(outcome), item);
            }
            catch (Exception ex) when (IsMediaChanged(ex))
            {
                counts[BackupItemOutcome.Changed] = counts.GetValueOrDefault(BackupItemOutcome.Changed) + 1;
                LogItemProcessed(logger, nameof(BackupService), itemCount, BackupItemOutcome.Changed);
                Record(BackupLogLevel.Warning, Describe(BackupItemOutcome.Changed), item);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadableCount++;
                LogItemUnreadable(logger, nameof(BackupService), itemCount, ex.GetType().Name);
                Record(BackupLogLevel.Warning, $"Could not be read ({ex.GetType().Name}); the next run retries it.", item);
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
            incremental,
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

    // TODO: New items are read twice, once to hash and once to upload, because the hash is recorded as metadata when the
    // upload starts. A destination that sets the hash at commit, such as Azure staged blocks with metadata on Put Block List,
    // could hash during a single upload read; deferred until throughput on large videos shows the second read matters.
    private async Task<(BackupItemOutcome Outcome, long Length)> BackUpItemAsync(
        MediaItem item,
        BackupCheckMode mode,
        ItemProgress run,
        CancellationToken cancellationToken)
    {
        run.Report(BackupItemPhase.Checking, 0);
        var existing = await backupDestination.FindBackupAsync(item, cancellationToken).ConfigureAwait(false);
        if (existing is not null && mode == BackupCheckMode.Quick && MatchesSource(existing, item))
            return (BackupItemOutcome.AlreadyBackedUp, item.SizeBytes);

        run.Report(BackupItemPhase.Hashing, 0);
        var (sha256, length) = await MediaHash.ComputeAsync(
            mediaSource, item, cancellationToken, read => run.Report(BackupItemPhase.Hashing, read)).ConfigureAwait(false);

        if (existing is null)
        {
            run.Report(BackupItemPhase.Uploading, 0);
            var content = await mediaSource.OpenReadAsync(item, cancellationToken).ConfigureAwait(false);
            await using (content.ConfigureAwait(false))
            {
                // The progress stream disposes the verifying stream; the verifying stream leaves the content to its owner.
                var trackedContent = new ProgressReportingStream(
                    new HashVerifyingStream(content, sha256), read => run.Report(BackupItemPhase.Uploading, read));
                await using (trackedContent.ConfigureAwait(false))
                {
                    if (await backupDestination.TryCreateBackupAsync(item, trackedContent, sha256, cancellationToken).ConfigureAwait(false))
                        return (BackupItemOutcome.Uploaded, length);
                }
            }

            // Another writer created the backup between the lookup and the upload.
            existing = await backupDestination.FindBackupAsync(item, cancellationToken).ConfigureAwait(false);
        }

        var outcome = existing?.Sha256 switch
        {
            null => BackupItemOutcome.Unverified,
            var recorded when string.Equals(recorded, sha256, StringComparison.OrdinalIgnoreCase) => BackupItemOutcome.AlreadyBackedUp,
            _ => BackupItemOutcome.Conflict,
        };
        return (outcome, length);
    }

    private static bool MatchesSource(BackupRecord existing, MediaItem item) =>
        existing.SourceSizeBytes == item.SizeBytes
        && existing.SourceModifiedUtc?.ToUnixTimeSeconds() == item.ModifiedUtc.ToUnixTimeSeconds();

    private static string Describe(BackupItemOutcome outcome) => outcome switch
    {
        BackupItemOutcome.Uploaded => "Uploaded.",
        BackupItemOutcome.AlreadyBackedUp => "Already backed up.",
        BackupItemOutcome.Conflict => "A different file with this name is already backed up; neither was changed.",
        BackupItemOutcome.Unverified => "A backup exists without a recorded hash; it was left unchanged.",
        BackupItemOutcome.Changed => "Changed while it was being backed up; the next run retries it.",
        _ => outcome.ToString(),
    };

    private void Record(BackupLogLevel level, string message, MediaItem? item = null) =>
        backupLog?.Add(new BackupLogEntry(timeProvider.GetUtcNow(), level, message, item?.DisplayName, item?.SizeBytes));

    private static bool IsMediaChanged(Exception exception) => exception switch
    {
        MediaChangedException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(IsMediaChanged),
        _ => exception.InnerException is { } inner && IsMediaChanged(inner),
    };

    /// <summary>
    /// Reports progress for one item, carrying the run totals from before it started.
    /// </summary>
    private sealed class ItemProgress(IProgress<BackupProgress>? progress, int itemNumber, int uploadedCount, long uploadedBytes, MediaItem item)
    {
        public void Report(BackupItemPhase phase, long bytes) =>
            progress?.Report(new BackupProgress(itemNumber, uploadedCount, uploadedBytes, phase, item.DisplayName, bytes, item.SizeBytes));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ClassName} processed ItemNumber={ItemNumber} Outcome={Outcome}")]
    private static partial void LogItemProcessed(ILogger logger, string className, int itemNumber, BackupItemOutcome outcome);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ClassName} could not read ItemNumber={ItemNumber} ExceptionType={ExceptionType}")]
    private static partial void LogItemUnreadable(ILogger logger, string className, int itemNumber, string exceptionType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{ClassName} backup completed Incremental={Incremental} ItemCount={ItemCount} UploadedCount={UploadedCount} AlreadyBackedUpCount={AlreadyBackedUpCount} ConflictCount={ConflictCount} UnverifiedCount={UnverifiedCount} ChangedCount={ChangedCount} UnreadableCount={UnreadableCount} Elapsed={Elapsed}")]
    private static partial void LogRunCompleted(
        ILogger logger,
        string className,
        bool incremental,
        int itemCount,
        int uploadedCount,
        int alreadyBackedUpCount,
        int conflictCount,
        int unverifiedCount,
        int changedCount,
        int unreadableCount,
        TimeSpan elapsed);
}
