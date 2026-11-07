# DcimCloudSync

The .NET MAUI application project for Android DCIM Cloud Sync. Android is the product platform; the
Windows target exists only for debugging services on a workstation, and is the only target that can
back up today.

## Usage

Follow the [root development instructions](../../README.md#local-development) from the repository
root. On launch, the **Status** page describes the current state. On Windows, it also shows a
**Media** section with **Scan media**, **Back up** (only when a backup destination is configured) and
**Cancel** buttons, an activity indicator and a result announced to screen readers. One operation runs
at a time, off the UI thread. On Android, a **Settings** tab holds the sign-in and storage settings, and
**Back up** appears once they are saved. Each operation first requests media access and appends a note
when access is partial or location is excluded.

## Application Composition

| Source contract | Responsibility |
| --- | --- |
| [`MauiProgram`](MauiProgram.cs) | Creates the MAUI host, registers the page and calls the platform's service registration; adds debug logging in Debug builds only |
| [`MauiProgram.Windows`](Platforms/Windows/MauiProgram.Windows.cs) | Windows-only registration of configuration, `TimeProvider`, the folder media source and the inventory service, plus the Azure backup destination and backup service when a connection string is configured |
| [`App`](App.xaml.cs) | Resolves the page from dependency injection for each window |
| [`AppShell`](AppShell.xaml.cs) | Tab bar hosting the injected status page at `main`, plus the settings page at `settings` where the platform registers one |
| [`MainPage`](Pages/MainPage.xaml.cs) | Status text and, when the services are registered, the scan and backup controls sharing one cancellable runner |
| [`WinUI.App`](Platforms/Windows/App.xaml.cs) | WinUI entry point that starts the MAUI app on Windows |
| [`MauiProgram.Android`](Platforms/Android/MauiProgram.Android.cs) | Android-only registration of preference-backed configuration, options, `TimeProvider`, media access, the MediaStore source, inventory and backup services, the MSAL token provider, the Azure destination and the settings page |
| [`MediaStoreMediaSource`](Platforms/Android/Services/MediaStoreMediaSource.cs) | Queries the image and video collections of every external volume for DCIM rows and opens original bytes with `SetRequireOriginal` and original-format acceptance |
| [`AndroidMediaAccess`](Platforms/Android/Services/AndroidMediaAccess.cs) | Requests the media permissions together and classifies the grant as full, partial or denied, with or without location |
| [`MsalAccessTokenProvider`](Platforms/Android/Services/MsalAccessTokenProvider.cs) | Acquires storage tokens silently, or interactively in the system browser without the broker; signs out by removing cached accounts |
| [`MsalActivity`](Platforms/Android/MsalActivity.cs) | Receives the `io.github.f2calv.androiddcimcloudsync://auth` sign-in redirect |
| [`SettingsPage`](Pages/SettingsPage.xaml.cs) | Edits and validates the backup settings and signs out |
| [`PreferencesBackupSettingsStore`](Services/PreferencesBackupSettingsStore.cs) | Saves the settings to MAUI `Preferences` and reloads configuration so options update without a restart |
| [`PreferencesConfigurationProvider`](Services/PreferencesConfigurationProvider.cs) | Exposes the saved settings as configuration keys |

The media and backup contracts and services come from [DcimCloudSync.Core](../DcimCloudSync.Core/README.md),
and both targets use the [Azure destination](../DcimCloudSync.Storage.Azure/README.md).

## Platforms and Permissions

The [project definition](DcimCloudSync.csproj) always targets `net10.0-android`, minimum Android API
33 (Android 13). It does not set a target SDK API explicitly; that is resolved by the installed
Android workload. When the build runs on Windows, it also targets `net10.0-windows10.0.19041.0`,
minimum Windows 10 version 1809 (`10.0.17763.0`), as an unpackaged app (`WindowsPackageType=None`)
started through the **Windows Machine** [launch profile](Properties/launchSettings.json). CI builds the
Android target on Linux and publishes the Windows target on a Windows runner as a self-contained test
artifact; neither is released to users. iOS and macOS are not targeted.

The [manifest](Platforms/Android/AndroidManifest.xml) disables application-data backup
(`android:allowBackup="false"`) and cleartext traffic (`android:usesCleartextTraffic="false"`).
It declares `READ_MEDIA_IMAGES`, `READ_MEDIA_VIDEO`, `READ_MEDIA_VISUAL_USER_SELECTED`,
`ACCESS_MEDIA_LOCATION`, `INTERNET` and `ACCESS_NETWORK_STATE`; the
[root README](../../README.md#privacy-and-platform-limits) explains each. Media access is requested only
when an operation starts, through MAUI's permissions API on the UI thread.

Discovery reads only rows whose MediaStore relative path starts with `DCIM/`; MediaStore already
excludes pending and trashed rows. Each item's relative path is relative to DCIM, prefixed with the volume
name on removable storage, so blob names match the Windows folder source. Without
`ACCESS_MEDIA_LOCATION` the source falls back to MediaStore's location-redacted bytes.

No foreground service, scheduled background job, notification flow or boot receiver is implemented. Closing or restarting the app cannot resume a backup because no
queue or backup state exists. There is no local or remote deletion behavior.

## Configuration and Dependencies

On Android, the **Settings** tab saves the directory (tenant) ID, application (client) ID, blob service URL,
container, prefixes and access tier to MAUI `Preferences`, bound as the `Entra` and `AzureBlobBackup`
configuration sections. None is a secret; never package them. MSAL keeps sign-in tokens in its own
app-private cache. Do not supply connection strings or keys to the Android app.

On Windows, environment variables prefixed `DcimCloudSync__` are added to configuration.
`DcimCloudSync__FileSystemMediaSource__RootPath` overrides the scanned folder, which defaults to
`DcimCloudSync` inside the user's Pictures folder. Setting
`DcimCloudSync__AzureBlobBackup__ConnectionString` enables **Back up**; the
[Azure destination README](../DcimCloudSync.Storage.Azure/README.md#configuration) lists the other
keys. Data-annotation validation runs when an operation first reads a value, because MAUI has no host
start to trigger `ValidateOnStart`.

The [central package versions](../../Directory.Packages.props) supply these direct dependencies:

| Package | Version | Targets | Use |
| --- | --- | --- | --- |
| `Microsoft.Maui.Controls` | `10.0.120` | All | MAUI application, Shell and XAML controls |
| `Microsoft.Extensions.Logging.Debug` | `10.0.14` | All | Debug-build logging provider |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | `10.0.14` | Windows | Environment-variable configuration |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.14` | All | Binds the media source, sign-in and backup configuration |
| `Microsoft.Extensions.Options.DataAnnotations` | `10.0.14` | All | Validates the media source, sign-in and backup configuration |
| `Microsoft.Identity.Client` | `4.92.0` | Android | MSAL sign-in and token refresh |

| Project reference | Use |
| --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/DcimCloudSync.Core.csproj) | Media and backup contracts, folder media source, scan and backup services |
| [DcimCloudSync.Storage.Azure](../DcimCloudSync.Storage.Azure/DcimCloudSync.Storage.Azure.csproj) | Azure Blob Storage backup destination |

The project is not packable as a NuGet library. Device behavior and accessibility require separate
interactive validation.
