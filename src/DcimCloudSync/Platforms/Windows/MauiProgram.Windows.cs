using DcimCloudSync.Services;
using Microsoft.Extensions.Configuration;

namespace DcimCloudSync;

public static partial class MauiProgram
{
    private const string EnvironmentVariablePrefix = "DcimCloudSync__";

    static partial void AddPlatformServices(MauiAppBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables(EnvironmentVariablePrefix);
        builder.Services.AddOptionsWithValidateOnStart<FileSystemMediaSourceConfig>()
            .Bind(builder.Configuration.GetSection(FileSystemMediaSourceConfig.ConfigurationSectionName))
            .ValidateDataAnnotations();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IMediaAccess, UnrestrictedMediaAccess>();
        builder.Services.AddSingleton<IMediaSource, FileSystemMediaSource>();
        builder.Services.AddSingleton<MediaInventoryService>();
        AddBackup(builder);
    }

    private static void AddBackup(MauiAppBuilder builder)
    {
        var section = builder.Configuration.GetSection(AzureBlobBackupConfig.ConfigurationSectionName);
        if (string.IsNullOrWhiteSpace(section.Get<AzureBlobBackupConfig>()?.ConnectionString))
            return;

        builder.Services.AddOptionsWithValidateOnStart<AzureBlobBackupConfig>()
            .Bind(section)
            .ValidateDataAnnotations();
        builder.Services.AddSingleton<IBackupDestination, AzureBlobBackupDestination>();
        builder.Services.AddSingleton<BackupService>();
    }
}
