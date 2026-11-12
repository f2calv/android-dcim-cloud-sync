namespace CasCap.Models;

/// <summary>
/// Configures how backup runs compare items with their existing backups.
/// </summary>
public sealed record BackupConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "Backup";

    /// <summary>Gets or sets how a run decides that an item is already backed up.</summary>
    public BackupCheckMode CheckMode { get; set; } = BackupCheckMode.Quick;
}
