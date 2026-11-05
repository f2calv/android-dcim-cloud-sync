namespace DcimCloudSync.Models;

/// <summary>
/// Reports how much of a backup run has completed.
/// </summary>
/// <param name="ItemCount">The number of items processed so far.</param>
/// <param name="UploadedCount">The number of items uploaded so far.</param>
/// <param name="UploadedBytes">The number of bytes uploaded so far.</param>
public sealed record BackupProgress(int ItemCount, int UploadedCount, long UploadedBytes);
