using CasCap.Services;
using Microsoft.Extensions.Configuration;

namespace CasCap;

/// <summary>
/// Configures and creates the MAUI application host.
/// </summary>
public static partial class MauiProgram
{
    /// <summary>
    /// Creates the configured MAUI application.
    /// </summary>
    /// <returns>The configured application host.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<IBackupLog, FileBackupLog>();
        builder.Services.AddTransient<LogPage>();
        builder.Services.AddOptions<BackupConfig>()
            .Bind(builder.Configuration.GetSection(BackupConfig.ConfigurationSectionName));
        AddPlatformServices(builder);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Registers the services implemented by the current platform, if it implements any.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    static partial void AddPlatformServices(MauiAppBuilder builder);
}
