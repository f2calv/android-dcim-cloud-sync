using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>
/// Configures the Azure Blob Storage backup destination.
/// </summary>
/// <remarks>
/// Set exactly one of <see cref="ConnectionString"/>, for development against Azurite or an account key, or
/// <see cref="BlobServiceUri"/>, which authenticates with a signed-in user's access token.
/// </remarks>
public sealed record AzureBlobBackupConfig : IValidatableObject
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "AzureBlobBackup";

    /// <summary>Gets the storage connection string, or <see langword="null"/> when a signed-in user's token is used.</summary>
    /// <remarks>
    /// Development only: <c>UseDevelopmentStorage=true</c> targets Azurite. Supply real values from an environment variable
    /// or other secret store, never tracked or packaged configuration.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>Gets the blob service endpoint, such as <c>https://account.blob.core.windows.net/</c>, used with a token.</summary>
    public Uri? BlobServiceUri { get; set; }

    /// <summary>Gets the blob container that receives backups.</summary>
    [Required, RegularExpression("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")]
    public string ContainerName { get; set; } = "media";

    /// <summary>Gets the blob name prefix for photos, without slashes, or an empty string for none.</summary>
    public string PhotoPrefix { get; set; } = "photos";

    /// <summary>Gets the blob name prefix for videos, without slashes, or an empty string for none.</summary>
    public string VideoPrefix { get; set; } = "videos";

    /// <summary>Gets the access tier for new backups, or <see langword="null"/> for the account default.</summary>
    [AllowedValues(null, "Hot", "Cool", "Cold", "Archive")]
    public string? AccessTier { get; set; }

    /// <summary>Gets a value indicating whether a missing container is created before the first upload.</summary>
    /// <remarks>Enable for Azurite; leave disabled where the identity should not hold container-creation rights.</remarks>
    public bool ContainerCreationEnabled { get; set; }

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasConnectionString = !string.IsNullOrWhiteSpace(ConnectionString);
        if (hasConnectionString == (BlobServiceUri is not null))
        {
            yield return new ValidationResult(
                "Set either a connection string or a blob service URL, not both.",
                [nameof(ConnectionString), nameof(BlobServiceUri)]);
        }

        if (BlobServiceUri is { } uri && (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps || uri.AbsolutePath != "/"))
        {
            yield return new ValidationResult(
                "The blob service URL must be an https endpoint without a path, such as https://account.blob.core.windows.net/.",
                [nameof(BlobServiceUri)]);
        }
    }
}
