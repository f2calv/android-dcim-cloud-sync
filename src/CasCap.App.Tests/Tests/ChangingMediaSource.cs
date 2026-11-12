using DcimCloudSync.Abstractions;

namespace DcimCloudSync.Tests;

/// <summary>
/// Serves one in-memory item whose content changes after the first open, simulating an edit between hashing and upload.
/// </summary>
/// <param name="relativePath">The item's relative path.</param>
/// <param name="first">The content returned by the first open.</param>
/// <param name="later">The content returned by every later open.</param>
internal sealed class ChangingMediaSource(string relativePath, byte[] first, byte[] later) : IMediaSource
{
    private int _openCount;

    /// <inheritdoc/>
    public async IAsyncEnumerable<MediaItem> DiscoverAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield return new MediaItem
        {
            SourceId = relativePath,
            RelativePath = relativePath,
            DisplayName = Path.GetFileName(relativePath),
            MimeType = "image/jpeg",
            SizeBytes = first.Length,
            ModifiedUtc = DateTimeOffset.UnixEpoch,
        };
    }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(Interlocked.Increment(ref _openCount) == 1 ? first : later, writable: false));
}
