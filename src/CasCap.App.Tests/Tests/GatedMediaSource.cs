using DcimCloudSync.Abstractions;

namespace DcimCloudSync.Tests;

/// <summary>
/// Holds every discovery open until <see cref="Open"/> is called, so a test can observe overlapping runs.
/// </summary>
internal sealed class GatedMediaSource : IMediaSource
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _discoverCount;

    /// <summary>
    /// Gets the number of discoveries started.
    /// </summary>
    public int DiscoverCount => Volatile.Read(ref _discoverCount);

    /// <summary>
    /// Releases every waiting and future discovery.
    /// </summary>
    public void Open() => _gate.TrySetResult();

    /// <inheritdoc/>
    public async IAsyncEnumerable<MediaItem> DiscoverAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref _discoverCount);
        await _gate.Task.WaitAsync(cancellationToken);
        yield break;
    }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(MediaItem item, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The gated source discovers no items.");
}
