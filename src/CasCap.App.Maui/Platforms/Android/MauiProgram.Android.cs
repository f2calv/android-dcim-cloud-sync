using CasCap.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace CasCap;

public static partial class MauiProgram
{
    static partial void AddPlatformServices(MauiAppBuilder builder)
    {
        var settings = new PreferencesConfigurationProvider(Preferences.Default, PreferencesBackupSettingsStore.Keys);
        ((IConfigurationBuilder)builder.Configuration).Add(settings);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(Preferences.Default);

        builder.Services.AddOptions<EntraConfig>()
            .Bind(builder.Configuration.GetSection(EntraConfig.ConfigurationSectionName))
            .ValidateDataAnnotations();
        builder.Services.AddOptions<AzureBlobBackupConfig>()
            .Bind(builder.Configuration.GetSection(AzureBlobBackupConfig.ConfigurationSectionName))
            .ValidateDataAnnotations();
        builder.Services.AddOptions<MediaStoreMediaSourceConfig>()
            .Bind(builder.Configuration.GetSection(MediaStoreMediaSourceConfig.ConfigurationSectionName));

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IMediaAccess, AndroidMediaAccess>();
        builder.Services.AddSingleton<IMediaSource, MediaStoreMediaSource>();
        builder.Services.AddSingleton<IBackupCheckpointStore, PreferencesBackupCheckpointStore>();
        builder.Services.AddSingleton<MediaInventoryService>();
        builder.Services.AddSingleton<IAccessTokenProvider, MsalAccessTokenProvider>();
        builder.Services.AddSingleton<IBackupDestination, AzureBlobBackupDestination>();
        builder.Services.AddSingleton<BackupService>();
        builder.Services.AddSingleton<IBackgroundBackupScheduler, WorkManagerBackupScheduler>();
        builder.Services.AddSingleton<BackupNotifier>();
        builder.Services.AddSingleton<BackgroundBackupRunner>();
        builder.Services.AddSingleton<IBackupSettingsStore, PreferencesBackupSettingsStore>();
        builder.Services.AddTransient<SettingsPage>();
    }
}
