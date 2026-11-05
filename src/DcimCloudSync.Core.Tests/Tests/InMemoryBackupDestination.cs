using DcimCloudSync.Abstractions;

namespace DcimCloudSync.Tests;

/// <summary>
/// Keeps backups in memory so <see cref="BackupService"/> can be tested without a provider.
/// </summary>
internal sealed class InMemoryBackupDestination : IBackupDestination
{
    /// <summary>Gets the stored backups by source identity: the recorded hash and the uploaded bytes.</summary>
    public Dictionary<string, (string? Sha256, byte[] Content)> Backups { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets a backup that appears after the first lookup, simulating a concurrent writer.</summary>
    public (string SourceId, string Sha256)? ConcurrentWrite { get; set; }

    /// <summary>Gets or sets a failure thrown by every operation.</summary>
    public Exception? Failure { get; set; }

    /// <inheritdoc/>
    public Task<BackupRecord?> FindBackupAsync(MediaItem item, CancellationToken cancellationToken)
    {
        if (Failure is not null)
            throw Failure;

        return Task.FromResult(Backups.TryGetValue(item.SourceId, out var backup) ? new BackupRecord(backup.Sha256) : null);
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateBackupAsync(MediaItem item, Stream content, string sha256, CancellationToken cancellationToken)
    {
        if (Failure is not null)
            throw Failure;

        if (ConcurrentWrite is { } write && write.SourceId == item.SourceId)
            Backups[write.SourceId] = (write.Sha256, []);
        if (Backups.ContainsKey(item.SourceId))
            return false;

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Backups[item.SourceId] = (sha256, buffer.ToArray());
        return true;
    }
}
