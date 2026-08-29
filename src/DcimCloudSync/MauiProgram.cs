using DcimCloudSync.Diagnostics;
using DcimCloudSync.Pages;
using DcimCloudSync.Platforms.Android.Diagnostics;

namespace DcimCloudSync;

/// <summary>
/// Configures and creates the MAUI application host.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates the configured MAUI application.
    /// </summary>
    /// <returns>The configured application host.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddTransient<IPixel7MediaStoreDiagnostic, AndroidPixel7MediaStoreDiagnostic>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
