using System.ComponentModel.DataAnnotations;
using Microsoft.Maui.Storage;

namespace DcimCloudSync.Services;

/// <summary>
/// Stores device-entered backup settings in MAUI <see cref="IPreferences"/> and applies them through configuration.
/// </summary>
/// <remarks>Nothing stored here is a secret; tokens stay in the sign-in library's cache.</remarks>
/// <param name="preferences">The preference store.</param>
/// <param name="configuration">The configuration provider to reload after saving.</param>
/// <param name="tokenProvider">The token provider to sign out of.</param>
/// <param name="scheduler">The background backup scheduler to update after saving.</param>
/// <param name="checkpointStore">The incremental checkpoint, cleared when the destination changes.</param>
public sealed class PreferencesBackupSettingsStore(
    IPreferences preferences,
    PreferencesConfigurationProvider configuration,
    IAccessTokenProvider tokenProvider,
    IBackgroundBackupScheduler scheduler,
    IBackupCheckpointStore checkpointStore)
    : IBackupSettingsStore
{
    // Read by the app only, so it is not one of the configuration keys.
    private const string AutomaticBackupKey = "Backup:AutomaticBackup";
    private const string TenantIdKey = EntraConfig.ConfigurationSectionName + ":" + nameof(EntraConfig.TenantId);
    private const string ClientIdKey = EntraConfig.ConfigurationSectionName + ":" + nameof(EntraConfig.ClientId);
    private const string BlobServiceUriKey = AzureBlobBackupConfig.ConfigurationSectionName + ":" + nameof(AzureBlobBackupConfig.BlobServiceUri);
    private const string ContainerNameKey = AzureBlobBackupConfig.ConfigurationSectionName + ":" + nameof(AzureBlobBackupConfig.ContainerName);
    private const string PhotoPrefixKey = AzureBlobBackupConfig.ConfigurationSectionName + ":" + nameof(AzureBlobBackupConfig.PhotoPrefix);
    private const string VideoPrefixKey = AzureBlobBackupConfig.ConfigurationSectionName + ":" + nameof(AzureBlobBackupConfig.VideoPrefix);
    private const string AccessTierKey = AzureBlobBackupConfig.ConfigurationSectionName + ":" + nameof(AzureBlobBackupConfig.AccessTier);

    /// <summary>The configuration keys this store owns.</summary>
    public static readonly IReadOnlyList<string> Keys =
    [
        TenantIdKey,
        ClientIdKey,
        BlobServiceUriKey,
        ContainerNameKey,
        PhotoPrefixKey,
        VideoPrefixKey,
        AccessTierKey,
    ];

    /// <inheritdoc/>
    public BackupSettings Load()
    {
        var defaults = new AzureBlobBackupConfig();
        return new BackupSettings(
            preferences.Get(TenantIdKey, string.Empty),
            preferences.Get(ClientIdKey, string.Empty),
            preferences.Get(BlobServiceUriKey, string.Empty),
            preferences.Get(ContainerNameKey, defaults.ContainerName),
            preferences.Get(PhotoPrefixKey, defaults.PhotoPrefix),
            preferences.Get(VideoPrefixKey, defaults.VideoPrefix),
            preferences.Get(AccessTierKey, string.Empty),
            preferences.Get(AutomaticBackupKey, true));
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> Save(BackupSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var trimmed = settings with
        {
            TenantId = settings.TenantId.Trim(),
            ClientId = settings.ClientId.Trim(),
            BlobServiceUri = settings.BlobServiceUri.Trim(),
            ContainerName = settings.ContainerName.Trim(),
            PhotoPrefix = settings.PhotoPrefix.Trim(),
            VideoPrefix = settings.VideoPrefix.Trim(),
            AccessTier = settings.AccessTier.Trim(),
        };

        var errors = Validate(trimmed);
        if (errors.Count > 0)
            return errors;

        // Items backed up to the previous destination must be checked against the new one.
        var previous = Load();
        if (previous.BlobServiceUri != trimmed.BlobServiceUri
            || previous.ContainerName != trimmed.ContainerName
            || previous.PhotoPrefix != trimmed.PhotoPrefix
            || previous.VideoPrefix != trimmed.VideoPrefix)
            checkpointStore.Clear();

        preferences.Set(TenantIdKey, trimmed.TenantId);
        preferences.Set(ClientIdKey, trimmed.ClientId);
        preferences.Set(BlobServiceUriKey, trimmed.BlobServiceUri);
        preferences.Set(ContainerNameKey, trimmed.ContainerName);
        preferences.Set(PhotoPrefixKey, trimmed.PhotoPrefix);
        preferences.Set(VideoPrefixKey, trimmed.VideoPrefix);
        preferences.Set(AccessTierKey, trimmed.AccessTier);
        preferences.Set(AutomaticBackupKey, trimmed.AutomaticBackup);
        configuration.Reload();
        scheduler.Apply(trimmed.AutomaticBackup);
        return [];
    }

    /// <inheritdoc/>
    public Task SignOutAsync() => tokenProvider.SignOutAsync();

    private static List<string> Validate(BackupSettings settings)
    {
        var entra = new EntraConfig { TenantId = settings.TenantId, ClientId = settings.ClientId };
        var backup = new AzureBlobBackupConfig
        {
            BlobServiceUri = Uri.TryCreate(settings.BlobServiceUri, UriKind.Absolute, out var uri) ? uri : null,
            ContainerName = settings.ContainerName,
            PhotoPrefix = settings.PhotoPrefix,
            VideoPrefix = settings.VideoPrefix,
            AccessTier = settings.AccessTier.Length == 0 ? null : settings.AccessTier,
        };

        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(entra, new ValidationContext(entra), results, validateAllProperties: true);
        _ = Validator.TryValidateObject(backup, new ValidationContext(backup), results, validateAllProperties: true);
        var errors = results.Select(result => result.ErrorMessage ?? "A setting is invalid.").ToList();
        if (!entra.IsConfigured)
            errors.Add("Enter both the directory (tenant) ID and the application (client) ID.");
        return errors;
    }
}
