namespace CasCap.Services;

/// <summary>
/// Reads every item from the media source once and summarizes sizes, duplicates and unreadable items.
/// </summary>
/// <remarks>
/// Exercises the discovery, streaming and SHA-256 hashing path that backup builds on, without uploading anything. Logs
/// contain counts only, never item names or paths; the optional on-device <see cref="IBackupLog"/> also records file names,
/// so the user can compare what was found with what is on the device.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The clock for scan durations and log entries.</param>
/// <param name="mediaSource">The media source.</param>
/// <param name="backupLog">Records found and unreadable items for the user on the device, or <see langword="null"/>.</param>
public sealed partial class MediaInventoryService(
    ILogger<MediaInventoryService> logger,
    TimeProvider timeProvider,
    IMediaSource mediaSource,
    IBackupLog? backupLog = null)
{
    /// <summary>
    /// Discovers, reads and hashes every item visible to the media source.
    /// </summary>
    /// <param name="progress">Receives a report after each item, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the scan between and during reads.</param>
    /// <returns>A summary that identifies no individual item.</returns>
    public async Task<MediaInventorySummary> ScanAsync(
        IProgress<MediaInventoryProgress>? progress,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var hashes = new HashSet<string>(StringComparer.Ordinal);
        var itemCount = 0;
        var totalBytes = 0L;
        var duplicateCount = 0;
        var unreadableCount = 0;

        await foreach (var item in mediaSource.DiscoverAsync(cancellationToken).ConfigureAwait(false))
        {
            itemCount++;
            try
            {
                var (hash, length) = await MediaHash.ComputeAsync(mediaSource, item, cancellationToken).ConfigureAwait(false);
                totalBytes += length;
                if (!hashes.Add(hash))
                    duplicateCount++;

                LogItemHashed(logger, nameof(MediaInventoryService), itemCount, length);
                Record(BackupLogLevel.Information, "Found by scan.", item);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadableCount++;
                LogItemUnreadable(logger, nameof(MediaInventoryService), itemCount, ex.GetType().Name);
                Record(BackupLogLevel.Warning, $"Found by scan but could not be read ({ex.GetType().Name}).", item);
            }

            progress?.Report(new MediaInventoryProgress(itemCount, totalBytes));
        }

        var summary = new MediaInventorySummary(
            itemCount,
            totalBytes,
            duplicateCount,
            unreadableCount,
            timeProvider.GetElapsedTime(startedAt));
        LogScanCompleted(
            logger,
            nameof(MediaInventoryService),
            summary.ItemCount,
            summary.TotalBytes,
            summary.DuplicateCount,
            summary.UnreadableCount,
            summary.Elapsed);
        Record(
            BackupLogLevel.Information,
            $"Scan finished: {summary.ItemCount} items, {summary.DuplicateCount} duplicates, {summary.UnreadableCount} unreadable.");
        return summary;
    }

    private void Record(BackupLogLevel level, string message, MediaItem? item = null) =>
        backupLog?.Add(new BackupLogEntry(timeProvider.GetUtcNow(), level, message, item?.DisplayName, item?.SizeBytes));

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ClassName} hashed ItemNumber={ItemNumber} Length={Length}")]
    private static partial void LogItemHashed(ILogger logger, string className, int itemNumber, long length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ClassName} could not read ItemNumber={ItemNumber} ExceptionType={ExceptionType}")]
    private static partial void LogItemUnreadable(ILogger logger, string className, int itemNumber, string exceptionType);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{ClassName} scan completed ItemCount={ItemCount} TotalBytes={TotalBytes} DuplicateCount={DuplicateCount} UnreadableCount={UnreadableCount} Elapsed={Elapsed}")]
    private static partial void LogScanCompleted(
        ILogger logger,
        string className,
        int itemCount,
        long totalBytes,
        int duplicateCount,
        int unreadableCount,
        TimeSpan elapsed);
}
