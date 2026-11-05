namespace DcimCloudSync.Models;

/// <summary>
/// Summarizes a completed backup run without identifying any item.
/// </summary>
/// <param name="ItemCount">The number of discovered items.</param>
/// <param name="UploadedCount">The number of items uploaded in this run.</param>
/// <param name="AlreadyBackedUpCount">The number of items whose existing backup has the same SHA-256.</param>
/// <param name="ConflictCount">The number of items whose existing backup has a different SHA-256; neither was changed.</param>
/// <param name="UnverifiedCount">The number of items whose existing backup records no SHA-256; it was left unchanged.</param>
/// <param name="ChangedCount">The number of items that changed after hashing; nothing was uploaded for them.</param>
/// <param name="UnreadableCount">The number of items that could not be read.</param>
/// <param name="UploadedBytes">The number of bytes uploaded in this run.</param>
/// <param name="Elapsed">The run duration.</param>
public sealed record BackupSummary(
    int ItemCount,
    int UploadedCount,
    int AlreadyBackedUpCount,
    int ConflictCount,
    int UnverifiedCount,
    int ChangedCount,
    int UnreadableCount,
    long UploadedBytes,
    TimeSpan Elapsed);
