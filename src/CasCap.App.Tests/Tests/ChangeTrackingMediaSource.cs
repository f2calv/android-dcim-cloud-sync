using System.Globalization;
using System.Text;
using CasCap.Abstractions;

namespace CasCap.Tests;

/// <summary>
/// Serves in-memory items stamped with an increasing version, so tests can list only the items changed since a token.
/// </summary>
internal sealed class ChangeTrackingMediaSource : IChangeTrackingMediaSource
{
    private readonly List<(MediaItem Item, byte[] Content, int Version, bool Unreadable)> _items = [];
    private int _version;

    /// <summary>
    /// Adds an item at the next version.
    /// </summary>
    /// <param name="relativePath">The item's relative path.</param>
    /// <param name="content">The item's content.</param>
    /// <param name="unreadable">Whether opening the item fails as if it could not be read.</param>
    public void Add(string relativePath, string content, bool unreadable = false)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        _version++;
        _items.Add((
            new MediaItem
            {
                SourceId = relativePath,
                RelativePath = relativePath,
                DisplayName = Path.GetFileName(relativePath),
                MimeType = "image/jpeg",
                SizeBytes = bytes.Length,
                ModifiedUtc = DateTimeOffset.UnixEpoch,
            },
            bytes,
            _version,
            unreadable));
    }

    /// <inheritdoc/>
    public string GetChangeToken() => _version.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public IAsyncEnumerable<MediaItem> DiscoverAsync(CancellationToken cancellationToken) => DiscoverSinceAsync(0, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<MediaItem> DiscoverChangedAsync(string sinceToken, CancellationToken cancellationToken) =>
        DiscoverSinceAsync(int.Parse(sinceToken, CultureInfo.InvariantCulture), cancellationToken);

    /// <summary>
    /// Gets the number of times an item was opened for reading.
    /// </summary>
    public int OpenCount { get; private set; }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken)
    {
        OpenCount++;
        var entry = _items.Single(candidate => candidate.Item.SourceId == item.SourceId);
        return entry.Unreadable
            ? throw new IOException("The item cannot be read.")
            : Task.FromResult<Stream>(new MemoryStream(entry.Content, writable: false));
    }

    private async IAsyncEnumerable<MediaItem> DiscoverSinceAsync(
        int sinceVersion,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        foreach (var entry in _items.Where(entry => entry.Version > sinceVersion).ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry.Item;
        }
    }
}
