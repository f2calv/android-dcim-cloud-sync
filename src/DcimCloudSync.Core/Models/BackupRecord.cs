namespace DcimCloudSync.Models;

/// <summary>
/// Describes an existing backup of a media item.
/// </summary>
/// <param name="Sha256">The recorded lowercase hexadecimal SHA-256, or <see langword="null"/> when the backup carries none,
/// for example because another tool created it.</param>
/// <param name="SourceSizeBytes">The item size recorded at upload, or <see langword="null"/> when none was recorded.</param>
/// <param name="SourceModifiedUtc">The item modification time recorded at upload, to the second, or <see langword="null"/>
/// when none was recorded.</param>
public sealed record BackupRecord(string? Sha256, long? SourceSizeBytes = null, DateTimeOffset? SourceModifiedUtc = null);
