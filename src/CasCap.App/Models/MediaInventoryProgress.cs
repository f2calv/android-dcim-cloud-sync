namespace CasCap.Models;

/// <summary>
/// Reports how much of a media inventory scan has completed.
/// </summary>
/// <param name="ItemCount">The number of items processed so far, including unreadable items.</param>
/// <param name="TotalBytes">The number of bytes read and hashed so far.</param>
public sealed record MediaInventoryProgress(int ItemCount, long TotalBytes);
