namespace DcimCloudSync.Services;

/// <summary>
/// Reads every item from the media source once and summarizes sizes, duplicates and unreadable items.
/// </summary>
/// <remarks>
/// Exercises the discovery, streaming and SHA-256 hashing path that backup will build on, without uploading or
/// persisting anything. Logs contain counts only, never item names or paths.
/// </remarks>
public sealed partial class MediaInventoryService(
    ILogger<MediaInventoryService> logger,
    TimeProvider timeProvider,
    IMediaSource mediaSource)
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
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                unreadableCount++;
                LogItemUnreadable(logger, nameof(MediaInventoryService), itemCount, ex.GetType().Name);
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
        return summary;
    }

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
