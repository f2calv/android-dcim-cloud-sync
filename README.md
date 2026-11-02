# Android DCIM Cloud Sync

Android DCIM Cloud Sync is a .NET 10 MAUI Android learning scaffold for a photo and video backup
application. It currently displays a static status page; it does not back up or synchronize media.

## Current Application

The Android 13-or-later app opens one scrollable **Status** page describing the current implementation
and its XAML learning surface. There are no backup controls, account screens or configuration forms.
Media discovery, uploads, cloud providers, authentication, persistence, background scheduling,
resume, deduplication and deletion are not implemented. Azure Blob Storage is not a usable destination
in this checkout.

The [application project](src/DcimCloudSync/README.md) documents its host, dependencies, manifest
and platform constraints. It is the repository's only delivery unit; there are no provider or test
projects.

## Local Development

Use a stable .NET SDK compatible with `global.json` (`10.0.400`, with `latestFeature` roll-forward
and prereleases disabled), the .NET MAUI Android workload and its matching Android SDK/JDK.
Visual Studio 2026 with MAUI/Android tooling is the repository's interactive development path.
No Azure account or cloud resources are needed.

Open `DcimCloudSync.slnx` to inspect the application. With the Android workload already installed,
compile from the repository root without requesting an application package:

```powershell
dotnet restore .\DcimCloudSync.slnx
dotnet build .\DcimCloudSync.slnx --configuration Debug --no-restore -p:AndroidKeyStore=false -p:AndroidBuildApplicationPackage=false
```

To view the page, separately select a compatible Android emulator or paired device in Visual Studio
and deploy interactively. Deployment is not required to inspect the source. The repository contains
no automated tests or published application releases.

## Privacy and Platform Limits

The current shell reads no media, stores no credentials or backup state, and sends no product data
off the device. Its [Android manifest](src/DcimCloudSync/Platforms/Android/AndroidManifest.xml)
declares only:

| Permission | Scope in this checkout |
| --- | --- |
| `android.permission.INTERNET` | Allows network access; the static page makes no network requests |
| `android.permission.ACCESS_NETWORK_STATE` | Allows connectivity inspection; the static page does not inspect it |

No media, broad storage, notification or background-service permissions are declared. Android
application-data backup is disabled and cleartext traffic is disallowed. There is no media storage
or secure-credential implementation to configure, and no background worker survives closing the app.
Other MAUI platforms are not targeted.

See the [security policy](SECURITY.md) for vulnerability reporting.

## License

[MIT](LICENSE).
