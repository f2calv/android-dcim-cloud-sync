namespace DcimCloudSync.Models;

/// <summary>
/// One entry in the on-device backup log.
/// </summary>
/// <param name="Timestamp">When the event happened.</param>
/// <param name="Level">How serious the event is.</param>
/// <param name="Message">What happened, free of endpoints, accounts and credentials.</param>
/// <param name="FileName">The item's file name, shown on the device only, or <see langword="null"/> for run events.</param>
/// <param name="SizeBytes">The item's size, or <see langword="null"/> for run events.</param>
public sealed record BackupLogEntry(
    DateTimeOffset Timestamp,
    BackupLogLevel Level,
    string Message,
    string? FileName = null,
    long? SizeBytes = null);
