namespace DcimCloudSync.Abstractions;

/// <summary>
/// Stores backups of media items, one per item, and never overwrites an existing backup.
/// </summary>
/// <remarks>
/// Implementations map each item to a destination name; callers never see provider types. Failures other than an
/// existing backup surface as <see cref="Exceptions.BackupDestinationException"/>.
/// </remarks>
public interface IBackupDestination
{
    /// <summary>
    /// Finds the existing backup of an item.
    /// </summary>
    /// <param name="item">The item to look up.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The backup record, or <see langword="null"/> when the item has no backup.</returns>
    public Task<BackupRecord?> FindBackupAsync(MediaItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the backup of an item unless one already exists.
    /// </summary>
    /// <param name="item">The item being backed up.</param>
    /// <param name="content">The item's content, read sequentially and owned by the caller. Read it to its end before
    /// committing the backup: the read that reports the end may throw <see cref="Exceptions.MediaChangedException"/>, and
    /// then nothing may be committed.</param>
    /// <param name="sha256">The lowercase hexadecimal SHA-256 of the content, recorded with the backup.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    /// <returns><see langword="true"/> when the backup was created; <see langword="false"/> when a backup already existed and
    /// was left unchanged.</returns>
    public Task<bool> TryCreateBackupAsync(MediaItem item, Stream content, string sha256, CancellationToken cancellationToken);
}
