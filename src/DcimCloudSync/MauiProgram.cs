namespace DcimCloudSync;

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
