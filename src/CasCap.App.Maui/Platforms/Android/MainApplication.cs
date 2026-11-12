using Android.App;
using Android.Runtime;
using CasCap.Services;

namespace CasCap;

/// <summary>
/// Creates the MAUI host for the Android application process, whether the user or WorkManager started it.
/// </summary>
/// <param name="handle">The native application handle.</param>
/// <param name="ownership">The ownership policy for the native handle.</param>
[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc/>
    public override void OnCreate()
    {
        base.OnCreate();

        // Re-apply on every start so schedules follow the saved settings, including after an app update.
        var services = IPlatformApplication.Current!.Services;
        var enabled = services.GetRequiredService<IBackupSettingsStore>().Load().AutomaticBackup
            && services.GetRequiredService<BackupService>().IsAvailable;
        services.GetRequiredService<IBackgroundBackupScheduler>().Apply(enabled);
    }

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
