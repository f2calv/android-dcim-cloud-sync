using DcimCloudSync.Abstractions;
using DcimCloudSync.Models;
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
        builder.Services.AddSingleton<IMediaSource, FileSystemMediaSource>();
        builder.Services.AddSingleton<MediaInventoryService>();
    }
}
