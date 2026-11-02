using Android.App;
using Android.Runtime;

namespace DcimCloudSync;

/// <summary>
/// Creates the MAUI host for the Android application process.
/// </summary>
/// <param name="handle">The native application handle.</param>
/// <param name="ownership">The ownership policy for the native handle.</param>
[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
