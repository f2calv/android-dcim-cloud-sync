# DcimCloudSync

The .NET MAUI application project for Android DCIM Cloud Sync. Android is the product platform; the
Windows target exists only for debugging services on a workstation.

## Usage

Follow the [root development instructions](../../README.md#local-development) from the repository
root. On launch, the **Status** page describes the current state. On Windows, it also shows a
**Media** section with **Scan media**, **Back up** (only when a backup destination is configured) and
**Cancel** buttons, an activity indicator and a result announced to screen readers. One operation runs
at a time, off the UI thread. On Android, a **Settings** tab holds the sign-in and storage settings, and
**Back up** appears once they are saved. Each operation first requests media access and appends a note
when access is partial or location is excluded. On Android, **Back up** also asks once for notification
permission, and the **Settings** switch for automatic backup schedules background runs.

## Application Composition

| Source contract | Responsibility |
| --- | --- |
| [`MauiProgram`](MauiProgram.cs) | Creates the MAUI host, registers the page and calls the platform's service registration; adds debug logging in Debug builds only |
| [`MauiProgram.Windows`](Platforms/Windows/MauiProgram.Windows.cs) | Windows-only registration of configuration, `TimeProvider`, the folder media source and the inventory service, plus the Azure backup destination and backup service when a connection string is configured |
| [`App`](App.xaml.cs) | Resolves the page from dependency injection for each window |
| [`AppShell`](AppShell.xaml.cs) | Tab bar hosting the injected status page at `main`, plus the settings page at `settings` where the platform registers one |
| [`MainPage`](Pages/MainPage.xaml.cs) | Status text and, when the services are registered, the scan and backup controls sharing one cancellable runner |
| [`WinUI.App`](Platforms/Windows/App.xaml.cs) | WinUI entry point that starts the MAUI app on Windows |
| [`MauiProgram.Android`](Platforms/Android/MauiProgram.Android.cs) | Android-only registration of preference-backed configuration, options, `TimeProvider`, media access, the MediaStore source, the checkpoint store, inventory and backup services, the MSAL token provider, the Azure destination, the background scheduler, runner and notifier, and the settings page |
| [`MainApplication`](Platforms/Android/MainApplication.cs) | Creates the MAUI host for every process start, including WorkManager's, and re-applies the background schedule to match the saved settings |
| [`MediaStoreMediaSource`](Platforms/Android/Services/MediaStoreMediaSource.cs) | Queries the image and video collections of every external volume for DCIM rows, optionally only rows changed since a MediaStore generation, and opens original bytes with `SetRequireOriginal` and original-format acceptance |
| [`AndroidMediaAccess`](Platforms/Android/Services/AndroidMediaAccess.cs) | Requests the media permissions together, or checks them without prompting, and classifies the grant as full, partial or denied, with or without location |
| [`MsalAccessTokenProvider`](Platforms/Android/Services/MsalAccessTokenProvider.cs) | Acquires storage tokens silently, or interactively in the system browser without the broker, never in the background; signs out by removing cached accounts |
| [`MsalActivity`](Platforms/Android/MsalActivity.cs) | Receives the `io.github.f2calv.androiddcimcloudsync://auth` sign-in redirect |
| [`WorkManagerBackupScheduler`](Platforms/Android/Services/WorkManagerBackupScheduler.cs) | Schedules the new-media content trigger, the six-hourly catch-up and one-time retries with WorkManager, on unmetered networks with the battery not low |
| [`BackupWorker`](Platforms/Android/Services/BackupWorker.cs) | WorkManager worker with a fixed Java name; runs one background backup and re-arms the new-media trigger |
| [`BackgroundBackupRunner`](Platforms/Android/Services/BackgroundBackupRunner.cs) | Runs an incremental backup with sign-in prompts suppressed and decides between completing, retrying and notifying |
| [`BackupNotifier`](Platforms/Android/Services/BackupNotifier.cs) | Posts the high-importance run summary and requests to act, with counts and sizes only |
| [`SettingsPage`](Pages/SettingsPage.xaml.cs) | Edits and validates the backup settings, including automatic backup, and signs out |
| [`PreferencesBackupSettingsStore`](Services/PreferencesBackupSettingsStore.cs) | Saves the settings to MAUI `Preferences`, reloads configuration so options update without a restart, clears the checkpoint when the destination changes and re-applies the background schedule |
| [`PreferencesBackupCheckpointStore`](Services/PreferencesBackupCheckpointStore.cs) | Keeps the incremental backup checkpoint in MAUI `Preferences` |
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
`ACCESS_MEDIA_LOCATION`, `INTERNET`, `ACCESS_NETWORK_STATE` and `POST_NOTIFICATIONS`; WorkManager's
library manifest adds `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED` and `FOREGROUND_SERVICE`. The
[root README](../../README.md#privacy-and-platform-limits) explains each. Media and notification
access is requested only when an operation starts or settings are saved, through MAUI's permissions API
on the UI thread; background runs only check it.

Discovery reads only rows whose MediaStore relative path starts with `DCIM/`; MediaStore already
excludes pending and trashed rows. Each item's relative path is relative to DCIM, prefixed with the volume
name on removable storage, so blob names match the Windows folder source. Without
`ACCESS_MEDIA_LOCATION` the source falls back to MediaStore's location-redacted bytes.

### Background Backup

WorkManager runs [`BackupWorker`](Platforms/Android/Services/BackupWorker.cs) in three cases:

- **New media:** a one-time request with content-URI triggers on the image and video collections. It
  runs after the media index has been quiet for 15 seconds, at most a minute after the first change.
- **Catch-up:** a periodic request every six hours.
- **Retry:** a one-time request after a new-media run failed transiently.

All three wait for an unmetered network with the battery not low.

A content trigger fires once, so each new-media run re-arms it by replacing its own unique work. An
append chain would keep every finished request, and their stale job records make WorkManager
reschedule on each cold start, which stops the job that started the process. WorkManager persists the
schedules across process death and reboots.

Each run calls `BackupService.BackUpChangesAsync`, which reads only items whose MediaStore
`generation_modified` is newer than the checkpoint. The checkpoint holds the token taken at the start of
the last run that read every changed item. A rebuilt media index or a new volume is read in full, and
changing the destination clears the checkpoint. Manual and background runs share one lock, so they
never overlap.

Background runs suppress interactive sign-in through `InteractiveSignIn`. An expired sign-in or
withdrawn media access ends the run with a notification asking the user to open the app. Destination
failures are retried silently three times before the user is notified.

WorkManager stops ordinary work after about ten minutes; the stopped run is retried and resumes, but a
single upload longer than that never completes in the background (see the `TODO` on `BackupWorker`).
The app has no queue or per-item state and no deletion behavior.

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
| `Xamarin.AndroidX.Work.Runtime` | `2.10.3` | Android | WorkManager background scheduling; pinned to the AndroidX Core and Lifecycle versions MAUI brings in |

| Project reference | Use |
| --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/DcimCloudSync.Core.csproj) | Media and backup contracts, folder media source, scan and backup services |
| [DcimCloudSync.Storage.Azure](../DcimCloudSync.Storage.Azure/DcimCloudSync.Storage.Azure.csproj) | Azure Blob Storage backup destination |

The project is not packable as a NuGet library. Device behavior and accessibility require separate
interactive validation.
