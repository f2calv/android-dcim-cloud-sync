using CasCap.Abstractions;

namespace CasCap.Tests;

/// <summary>
/// Keeps backups in memory so <see cref="BackupService"/> can be tested without a provider.
/// </summary>
internal sealed class InMemoryBackupDestination : IBackupDestination
{
    /// <summary>Gets the stored backups by source identity.</summary>
    public Dictionary<string, StoredBackup> Backups { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets a backup that appears after the first lookup, simulating a concurrent writer.</summary>
    public (string SourceId, string Sha256)? ConcurrentWrite { get; set; }

    /// <summary>Gets or sets a failure thrown by every operation.</summary>
    public Exception? Failure { get; set; }

    /// <inheritdoc/>
    public bool IsConfigured => true;

    /// <inheritdoc/>
    public Task<BackupRecord?> FindBackupAsync(MediaItem item, CancellationToken cancellationToken)
    {
        if (Failure is not null)
            throw Failure;

        return Task.FromResult(Backups.TryGetValue(item.SourceId, out var backup)
            ? new BackupRecord(backup.Sha256, backup.SourceSizeBytes, backup.SourceModifiedUtc)
            : null);
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateBackupAsync(MediaItem item, Stream content, string sha256, CancellationToken cancellationToken)
    {
        if (Failure is not null)
            throw Failure;

        if (ConcurrentWrite is { } write && write.SourceId == item.SourceId)
            Backups[write.SourceId] = new StoredBackup(write.Sha256, []);
        if (Backups.ContainsKey(item.SourceId))
            return false;

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Backups[item.SourceId] = new StoredBackup(sha256, buffer.ToArray(), item.SizeBytes, item.ModifiedUtc);
        return true;
    }

    /// <summary>
    /// One stored backup.
    /// </summary>
    /// <param name="Sha256">The recorded hash, or <see langword="null"/> for none.</param>
    /// <param name="Content">The uploaded bytes.</param>
    /// <param name="SourceSizeBytes">The recorded source size, or <see langword="null"/> for none.</param>
    /// <param name="SourceModifiedUtc">The recorded source modification time, or <see langword="null"/> for none.</param>
    internal sealed record StoredBackup(string? Sha256, byte[] Content, long? SourceSizeBytes = null, DateTimeOffset? SourceModifiedUtc = null);
}
