using Android.App;
using Android.Runtime;

namespace DcimCloudSync;

/// <summary>
/// Creates the MAUI host for the Android application process.
/// </summary>
[Application]
public sealed class MainApplication : MauiApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainApplication"/> class.
    /// </summary>
    /// <param name="handle">The native application handle.</param>
    /// <param name="ownership">The ownership policy for the native handle.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
