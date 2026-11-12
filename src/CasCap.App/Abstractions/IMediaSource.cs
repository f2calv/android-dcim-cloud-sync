namespace DcimCloudSync.Abstractions;

/// <summary>
/// Discovers photos and videos and opens their content as read-only streams.
/// </summary>
/// <remarks>
/// Implementations exist per platform: a folder on the development machine, and later Android MediaStore.
/// Callers must not assume that <see cref="MediaItem.SourceId"/> or <see cref="MediaItem.RelativePath"/> maps to
/// a filesystem path, and must never log either value.
/// </remarks>
public interface IMediaSource
{
    /// <summary>
    /// Discovers the media items currently visible to the source.
    /// </summary>
    /// <param name="cancellationToken">Cancels discovery between items.</param>
    /// <returns>The discovered items, in source order.</returns>
    public IAsyncEnumerable<MediaItem> DiscoverAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a previously discovered item for sequential reading.
    /// </summary>
    /// <param name="item">An item returned by <see cref="DiscoverAsync(CancellationToken)"/>.</param>
    /// <param name="cancellationToken">Cancels opening the item.</param>
    /// <returns>A read-only stream owned and disposed by the caller.</returns>
    /// <exception cref="IOException">The item was removed or cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to the item was denied.</exception>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken);
}
