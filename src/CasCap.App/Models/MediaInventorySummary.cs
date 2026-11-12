namespace DcimCloudSync.Models;

/// <summary>
/// Summarizes a completed media inventory scan without identifying any item.
/// </summary>
/// <param name="ItemCount">The number of discovered items, including unreadable items.</param>
/// <param name="TotalBytes">The number of bytes read and hashed.</param>
/// <param name="DuplicateCount">The number of readable items whose SHA-256 matched an earlier item.</param>
/// <param name="UnreadableCount">The number of items that could not be opened or read.</param>
/// <param name="Elapsed">The scan duration.</param>
public sealed record MediaInventorySummary(
    int ItemCount,
    long TotalBytes,
    int DuplicateCount,
    int UnreadableCount,
    TimeSpan Elapsed);
