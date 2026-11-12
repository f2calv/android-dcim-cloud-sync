namespace CasCap.Models;

/// <summary>
/// Reports how much of a backup run has completed, and how far the current item has been read.
/// </summary>
/// <param name="ItemCount">The number of items started so far, including the current one.</param>
/// <param name="UploadedCount">The number of items uploaded so far.</param>
/// <param name="UploadedBytes">The number of bytes uploaded so far.</param>
/// <param name="Phase">The step being performed on the current item.</param>
/// <param name="CurrentItemName">The current item's file name, for display on the device only; never log it.</param>
/// <param name="CurrentItemBytes">The bytes of the current item read so far in this phase.</param>
/// <param name="CurrentItemLength">The current item's size reported by the source.</param>
public sealed record BackupProgress(
    int ItemCount,
    int UploadedCount,
    long UploadedBytes,
    BackupItemPhase Phase = BackupItemPhase.None,
    string? CurrentItemName = null,
    long CurrentItemBytes = 0,
    long CurrentItemLength = 0);
