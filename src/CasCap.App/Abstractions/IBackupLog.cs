namespace CasCap.Abstractions;

/// <summary>
/// Keeps the most recent backup and scan events on the device, for the user to review.
/// </summary>
/// <remarks>
/// Entries may name files, so implementations store them only in app-private storage and never forward them to logging
/// providers, telemetry or the backup destination.
/// </remarks>
public interface IBackupLog
{
    /// <summary>
    /// Occurs after entries are added or cleared.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Adds an entry, dropping the oldest when the log is full.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(BackupLogEntry entry);

    /// <summary>
    /// Gets the kept entries, newest first.
    /// </summary>
    /// <returns>A snapshot of the entries.</returns>
    public IReadOnlyList<BackupLogEntry> GetEntries();

    /// <summary>
    /// Removes every entry.
    /// </summary>
    public void Clear();
}
