# DcimCloudSync

The .NET MAUI application project for Android DCIM Cloud Sync. Android is the product platform; the
Windows target exists only for debugging services on a workstation. It is not a working media
backup client.

## Usage

Follow the [root development instructions](../../README.md#local-development) from the repository
root. On launch, the single **Status** page explains that backup functionality is absent. On
Windows, the page also shows a **Media inventory** section with **Scan media** and **Cancel**
buttons, an activity indicator and a result announced to screen readers. On Android, that section
is hidden because no media source is registered.

## Application Composition

| Source contract | Responsibility |
| --- | --- |
| [`MauiProgram`](MauiProgram.cs) | Creates the MAUI host, registers the page and calls the platform's service registration; adds debug logging in Debug builds only |
| [`MauiProgram.Windows`](Platforms/Windows/MauiProgram.Windows.cs) | Windows-only registration of configuration, `TimeProvider`, the folder media source and the inventory service |
| [`App`](App.xaml.cs) | Resolves the page from dependency injection for each window |
| [`AppShell`](AppShell.xaml.cs) | Single-page shell with its flyout disabled; hosts the injected page at the `main` route |
| [`MainPage`](Pages/MainPage.xaml.cs) | Status text and, when a media inventory service is available, the scan controls; scans run off the UI thread |
| [`WinUI.App`](Platforms/Windows/App.xaml.cs) | WinUI entry point that starts the MAUI app on Windows |

Android registers no product services yet. The media contracts and services come from
[DcimCloudSync.Core](../DcimCloudSync.Core/README.md).

## Platforms and Permissions

The [project definition](DcimCloudSync.csproj) always targets `net10.0-android`, minimum Android API
33 (Android 13). It does not set a target SDK API explicitly; that is resolved by the installed
Android workload. When the build runs on Windows, it also targets `net10.0-windows10.0.19041.0`,
minimum Windows 10 version 1809 (`10.0.17763.0`), as an unpackaged app (`WindowsPackageType=None`)
started through the **Windows Machine** [launch profile](Properties/launchSettings.json). CI runs on
Linux, so it builds and publishes the Android target only. iOS and macOS are not targeted.

The [manifest](Platforms/Android/AndroidManifest.xml) disables application-data backup
(`android:allowBackup="false"`) and cleartext traffic (`android:usesCleartextTraffic="false"`).
`INTERNET` and `ACCESS_NETWORK_STATE` are the only declared permissions. The current page uses
neither; it does not request media access or inspect scoped storage.

No foreground service, scheduled background job, notification flow, boot receiver or Android
media discovery is implemented. Closing or restarting the app cannot resume a backup because no
queue or backup state exists. There is no local or remote deletion behavior.

## Configuration and Dependencies

Android has no runtime configuration, authentication flow, secret storage or destination setting.
Do not supply cloud credentials to this app.

On Windows, environment variables prefixed `DcimCloudSync__` are added to configuration.
`DcimCloudSync__FileSystemMediaSource__RootPath` overrides the scanned folder, which defaults to
`DcimCloudSync` inside the user's Pictures folder. Data-annotation validation runs when a scan first
reads the value, because MAUI has no host start to trigger `ValidateOnStart`.

The [central package versions](../../Directory.Packages.props) supply these direct dependencies:

| Package | Version | Targets | Use |
| --- | --- | --- | --- |
| `Microsoft.Maui.Controls` | `10.0.120` | All | MAUI application, Shell and XAML controls |
| `Microsoft.Extensions.Logging.Debug` | `10.0.14` | All | Debug-build logging provider |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | `10.0.14` | Windows | Environment-variable configuration |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.14` | Windows | Binds the media source configuration |
| `Microsoft.Extensions.Options.DataAnnotations` | `10.0.14` | Windows | Validates the media source configuration |

| Project reference | Use |
| --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/DcimCloudSync.Core.csproj) | Media contracts, folder media source and inventory scan |

The project is not packable as a NuGet library. Device behavior and accessibility require separate
interactive validation.
