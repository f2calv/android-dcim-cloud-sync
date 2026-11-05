using System.Buffers;
using System.Security.Cryptography;

namespace DcimCloudSync.Services;

/// <summary>
/// Streams a media item once to compute its SHA-256 and length.
/// </summary>
internal static class MediaHash
{
    private const int BufferSize = 81_920;

    /// <summary>
    /// Reads an item to its end with a pooled buffer.
    /// </summary>
    /// <param name="mediaSource">The source that opens the item.</param>
    /// <param name="item">The item to hash.</param>
    /// <param name="cancellationToken">Cancels opening and reading.</param>
    /// <returns>The lowercase hexadecimal SHA-256 and the number of bytes read.</returns>
    public static async Task<(string Sha256, long Length)> ComputeAsync(
        IMediaSource mediaSource,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        var stream = await mediaSource.OpenReadAsync(item, cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                var length = 0L;
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                    length += read;
                }

                return (Convert.ToHexStringLower(hash.GetHashAndReset()), length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
