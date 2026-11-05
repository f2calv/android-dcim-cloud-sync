using System.ComponentModel.DataAnnotations;

namespace DcimCloudSync.Models;

/// <summary>
/// Configures the Azure Blob Storage backup destination.
/// </summary>
public sealed record AzureBlobBackupConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "AzureBlobBackup";

    /// <summary>Gets the storage connection string.</summary>
    /// <remarks>
    /// Development only: <c>UseDevelopmentStorage=true</c> targets Azurite. Supply real values from an environment variable
    /// or other secret store, never tracked or packaged configuration.
    /// </remarks>
    [Required, MinLength(1)]
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Gets the blob container that receives backups.</summary>
    [Required, RegularExpression("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")]
    public string ContainerName { get; init; } = "media";

    /// <summary>Gets the blob name prefix for photos, without slashes, or an empty string for none.</summary>
    public string PhotoPrefix { get; init; } = "photos";

    /// <summary>Gets the blob name prefix for videos, without slashes, or an empty string for none.</summary>
    public string VideoPrefix { get; init; } = "videos";

    /// <summary>Gets the access tier for new backups, or <see langword="null"/> for the account default.</summary>
    [AllowedValues(null, "Hot", "Cool", "Cold", "Archive")]
    public string? AccessTier { get; init; }

    /// <summary>Gets a value indicating whether a missing container is created before the first upload.</summary>
    /// <remarks>Enable for Azurite; leave disabled where the identity should not hold container-creation rights.</remarks>
    public bool ContainerCreationEnabled { get; init; }
}
