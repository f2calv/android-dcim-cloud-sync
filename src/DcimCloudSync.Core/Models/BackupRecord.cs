namespace DcimCloudSync.Models;

/// <summary>
/// Describes an existing backup of a media item.
/// </summary>
/// <param name="Sha256">The recorded lowercase hexadecimal SHA-256, or <see langword="null"/> when the backup carries none,
/// for example because another tool created it.</param>
public sealed record BackupRecord(string? Sha256);
