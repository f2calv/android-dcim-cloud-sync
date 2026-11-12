using System.Security.Cryptography;

namespace CasCap.Services;

/// <summary>
/// Passes content through unchanged while hashing it, and fails at the end when the SHA-256 differs from the expected one.
/// </summary>
/// <remarks>
/// The check runs on the first read that reports the end of the content, before the reader can commit what it read.
/// The stream is forward-only, so a reader cannot rewind and hash the content twice. The inner stream stays owned by the
/// caller.
/// </remarks>
/// <param name="inner">The content to read.</param>
/// <param name="expectedSha256">The lowercase hexadecimal SHA-256 the content must have.</param>
internal sealed class HashVerifyingStream(Stream inner, string expectedSha256) : Stream
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private bool _verified;

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        var read = inner.Read(buffer);
        Observe(buffer[..read], buffer.Length);
        return read;
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Observe(buffer.Span[..read], buffer.Length);
        return read;
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _hash.Dispose();

        base.Dispose(disposing);
    }

    private void Observe(ReadOnlySpan<byte> data, int requested)
    {
        if (data.Length > 0)
        {
            _hash.AppendData(data);
            return;
        }

        // A zero-length request returns 0 without meaning the content ended.
        if (requested == 0 || _verified)
            return;

        _verified = true;
        var actual = Convert.ToHexStringLower(_hash.GetHashAndReset());
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new MediaChangedException();
    }
}
