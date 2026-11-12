namespace CasCap.Abstractions;

/// <summary>
/// A media source that can list only the items added or modified since an earlier point, so routine runs avoid reading
/// the whole library.
/// </summary>
public interface IChangeTrackingMediaSource : IMediaSource
{
    /// <summary>
    /// Gets an opaque token for the current state of the media collection.
    /// </summary>
    /// <remarks>Take the token before discovering items, so changes made during a run are listed again next time.</remarks>
    /// <returns>A token to pass to <see cref="DiscoverChangedAsync(string, CancellationToken)"/> later.</returns>
    public string GetChangeToken();

    /// <summary>
    /// Discovers the items added or modified since the token was taken.
    /// </summary>
    /// <remarks>
    /// Parts of the collection the token does not describe, such as a newly inserted storage volume or a rebuilt media
    /// index, are discovered in full, so no item is missed.
    /// </remarks>
    /// <param name="sinceToken">A token from <see cref="GetChangeToken"/>.</param>
    /// <param name="cancellationToken">Cancels discovery between items.</param>
    /// <returns>The changed items, in source order.</returns>
    public IAsyncEnumerable<MediaItem> DiscoverChangedAsync(string sinceToken, CancellationToken cancellationToken);
}
