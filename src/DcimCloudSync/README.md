# DcimCloudSync

The Android-only .NET MAUI application project for Android DCIM Cloud Sync. It provides a static
status screen for local UI and Android development, not a working media backup client.

## Usage

Follow the [root development instructions](../../README.md#local-development) from the repository
root. On launch, the single **Status** page explains that backup functionality is absent.
The screen uses a scroll view, theme-aware styles and semantic heading labels; it has no buttons,
bound view model, media picker or account setup.

## Application Composition

| Source contract | Responsibility |
| --- | --- |
| [`MauiProgram`](MauiProgram.cs) | Creates the MAUI host; adds debug logging in Debug builds only |
| [`App`](App.xaml.cs) | Creates a window containing the application shell |
| [`AppShell`](AppShell.xaml.cs) | Single-page shell with its flyout disabled; [XAML](AppShell.xaml) sets the `main` route |
| [`MainPage`](Pages/MainPage.xaml.cs) | Initializes the static [status-page XAML](Pages/MainPage.xaml) |

There are no registered product services or project references. No provider-neutral API, Azure
adapter, media-access service, database or scheduler exists.

## Platform and Permissions

The [project definition](DcimCloudSync.csproj) targets `net10.0-android` with minimum Android API 33
(Android 13). It does not set a target SDK API explicitly; that is resolved by the installed Android
workload. There are no iOS, macOS or Windows targets.

The [manifest](Platforms/Android/AndroidManifest.xml) disables application-data backup
(`android:allowBackup="false"`) and cleartext traffic (`android:usesCleartextTraffic="false"`).
`INTERNET` and `ACCESS_NETWORK_STATE` are the only declared permissions. The current page uses
neither; it does not request media access or inspect scoped storage.

No foreground service, scheduled background job, notification flow, boot receiver or media discovery
is implemented. Closing or restarting the app cannot resume a backup because no queue or backup
state exists. There is no local or remote deletion behavior.

## Configuration and Dependencies

There is no runtime configuration file, authentication flow, secret storage or destination setting.
Do not supply cloud credentials to this shell. Its packaged resources consist of UI styles, icons
and a splash screen.

The [central package versions](../../Directory.Packages.props) supply both direct dependencies:

| Package | Version | Use |
| --- | --- | --- |
| `Microsoft.Maui.Controls` | `10.0.110` | MAUI application, Shell and XAML controls |
| `Microsoft.Extensions.Logging.Debug` | `10.0.12` | Debug-build logging provider |

The project is not packable as a NuGet library. There are no test projects in this repository;
device behavior and accessibility require separate interactive validation.
