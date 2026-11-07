using DcimCloudSync.Abstractions;

namespace DcimCloudSync.Tests;

/// <summary>
/// Keeps backup log entries in memory, newest first, without a size limit.
/// </summary>
internal sealed class InMemoryBackupLog : IBackupLog
{
    private readonly List<BackupLogEntry> _entries = [];

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public void Add(BackupLogEntry entry)
    {
        _entries.Insert(0, entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public IReadOnlyList<BackupLogEntry> GetEntries() => [.. _entries];

    /// <inheritdoc/>
    public void Clear()
    {
        _entries.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
