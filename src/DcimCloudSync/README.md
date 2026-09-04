---
title: DcimCloudSync Android Application
description: Project reference for the .NET MAUI Android host and temporary synthetic Pixel 7 MediaStore diagnostic
---

# DcimCloudSync Android Application

## Purpose

`DcimCloudSync` is the Android .NET MAUI host, UI composition root, and Android platform boundary for Android DCIM Cloud Sync. It currently hosts a temporary foreground diagnostic for gathering synthetic Pixel 7 MediaStore evidence before production backup contracts are designed.

## Current Implementation

The application contains one accessible Shell page with explicit image, video, combined-access, selection-management, access-refresh, diagnostic-run, and current-run cancellation controls. The temporary diagnostic:

* Inspects broad image, broad video, and API 34 or later selected-media access independently
* Queries image and video collections separately for each mounted external MediaStore volume
* Restricts both control and DCIM queries to seven manifest-known synthetic fixture identities
* Compares ordinary, strict typed, and original-format file-descriptor representations
* Reads sequentially with bounded memory and computes SHA-256 plus byte counts
* Displays only sanitized in-memory observations through Android-free records

This behavior is evidence-gathering infrastructure. It is not backup discovery, upload, provider integration, persistence, authentication, background work, resume, deduplication, or deletion.

## Target and Identity

* Target framework: `net10.0-android`
* Minimum Android API level: 33 (Android 13)
* Current workload-owned target API level: 36
* Application ID: `io.github.f2calv.androiddcimcloudsync`
* Initial physical-device target: Pixel 7

The application ID is permanent and must not be changed casually. The .NET Android workload owns the target API level, so release planning must recheck it against current platform and Google Play requirements.

## Android Backup and Permissions

Android application-data backup is disabled with `android:allowBackup="false"`. The application has no credentials, persisted diagnostic output, or product state. Any future persistence requires a separate review of Android backup and device-transfer protections.

The manifest declares five permissions:

| Permission                                           | Reason                                                                                                  |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `android.permission.READ_MEDIA_IMAGES`               | Discovers and opens the known synthetic image fixtures on API 33 and later                              |
| `android.permission.READ_MEDIA_VIDEO`                | Discovers and opens the known synthetic video and cancellation fixtures on API 33 and later             |
| `android.permission.READ_MEDIA_VISUAL_USER_SELECTED` | Represents selected-only visibility and reselection on API 34 and later                                 |
| `android.permission.INTERNET`                        | Retained for future provider work; no current service performs a network request                        |
| `android.permission.ACCESS_NETWORK_STATE`            | Retained for future connectivity checks; the current application does not inspect connectivity          |

`READ_MEDIA_IMAGES` and `READ_MEDIA_VIDEO` are independent because the diagnostic must represent images-only and videos-only states. `READ_MEDIA_VISUAL_USER_SELECTED` is requested only behind an API 34 guard and prevents selected access from being mislabeled as complete broad access.

No media-location, device-location, legacy storage, broad all-files, camera, notification, foreground-service, or boot permission is declared. In particular, `ACCESS_MEDIA_LOCATION` is absent. Protected photo-location behavior remains unverified, and the diagnostic cannot guarantee unredacted bytes for location-bearing photos.

## Services and Extensions

`MauiProgram` registers the following temporary services as transient dependencies:

| Service or component                         | Responsibility                                                                                   |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `IPixel7MediaStoreDiagnostic`                | Android-free foreground permission, refresh, and diagnostic-run boundary                         |
| `AndroidPixel7MediaStoreDiagnostic`          | Android permission checks, specific-volume fixture queries, bounded hashing, and neutral results |
| `MainPage`                                   | Explicit user invocation, accessible status, cancellation ownership, and sanitized rendering     |
| `DiagnosticFixtureManifest`                  | Internal strict loading and validation of packaged fixture metadata                              |

`App` resolves the transient page after resources initialize, and `AppShell` hosts that constructor-injected page. Debug builds retain the standard debug logging extension, but the diagnostic emits no application log events. No network, cloud-provider, persistence, identity, scheduler, background, foreground-service, or deletion service is registered.

## Configuration and Packaged Fixture Contract

The project has no application configuration file and consumes no runtime secret. Every asset packaged into an APK or Android App Bundle must be treated as public.

The MAUI raw asset `Resources/Raw/diagnostic-fixtures.json` contains only deterministic synthetic fixture IDs, query-only display names and relative placements, media classes, expected MIME types, trusted byte counts and SHA-256 values, selected-subset markers, and scenario visibility. Media payloads are not packaged. The strict loader rejects missing, extra, duplicate, placeholder, malformed, or inconsistent fixture definitions.

Display names and relative placements remain internal to the packaged manifest, strict loader, and Android implementation. They do not cross the diagnostic interface or appear in the UI, report records, or logs.

The future Azure provider must remain compatible with an existing Blob container and support separate configurable destination prefixes for JPG and MP4 media. The container and prefix selections are non-secret configuration concerns, but no real container name, prefix, endpoint, account identifier, path, SAS value, or token belongs in tracked files or logs.

## Synthetic Fixture Workflow

The separately prepared external `pixel7-mediastore-v1` bundle is a prerequisite for a device run. Its seven media files, generation intermediates, codec tools, and local source directory remain outside the repository. The fixture classes are:

* A JPEG without EXIF location metadata in nested DCIM storage
* An AVC MP4 in DCIM storage
* A Main 10 HEVC MP4 in DCIM storage
* A 547,603,109-byte AVC MP4 in DCIM storage for active-read cancellation
* A JPEG in non-DCIM Pictures storage as an image control
* An AVC MP4 in non-DCIM Movies storage as a video control
* A JPEG in DCIM-like prefix storage as a path-boundary control

For a selected-only run, select exactly `jpeg-dcim-nested-v1` and `hevc-main10-dcim-v1`. These neutral fixture IDs are the only fixture identities allowed in sanitized output. The manifest defines expected visibility for full, images-only, videos-only, selected-only, and denied states; mixed expectations combine the held broad kind with that selected subset.

Transfer only this synthetic bundle to the Pixel 7 using an owner-controlled external method, preserving its intended placement classes so Android indexes the controls correctly. The application does not transfer, generate, import, persist, or remove fixtures. Do not use personal media.

## Open-Path Comparison and Cancellation

The diagnostic compares each expected, visible fixture through these paths:

| Path                           | Implementation intent                                                                                                     |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| Ordinary control               | Uses the ordinary input stream without an original-content or no-transcode request                                        |
| Strict typed candidate         | Applies `MediaStore.SetRequireOriginal` and opens a typed descriptor with compatible transcoding disabled                 |
| Original-format PFD comparison | Applies `MediaStore.SetRequireOriginal`, opens a read-only PFD, then requests the MediaStore original-format descriptor   |

All three paths stream to EOF through a 64 KiB buffer, check cancellation between reads, and compare the observed count and SHA-256 with trusted metadata. The active resource is closed when the current run is canceled. A successful source implementation or future hash match is diagnostic evidence, not a production exactness guarantee.

## Sanitized Output

The in-memory report contains:

* Runtime and target API levels, platform version, security-patch month, optional provider-module version, and fixture-set version
* Independent image, video, selected-media, rationale, reselection, and derived completeness values
* Neutral fixture IDs, expected scenario sets, aggregate counts, and result codes
* Expected and observed byte counts and SHA-256 values
* Descriptor length and seek traits plus elapsed durations

The report, page, and application logs exclude display names, relative placements, volume names, row IDs, content URIs, local paths, EXIF or coordinate values, media bytes, and exception details. No report is persisted or sent over the network.

## NuGet Dependencies

Project package references are versionless. Root `Directory.Packages.props` centrally supplies these versions:

| Package                              | Version    |
| ------------------------------------ | ---------- |
| `Microsoft.Extensions.Logging.Debug` | `10.0.11`  |
| `Microsoft.Maui.Controls`            | `10.0.100` |

## Project References

| Project | Reference | Status                                                                                     |
| ------- | --------- | ------------------------------------------------------------------------------------------ |
| None    | None      | Provider-neutral Core and Azure provider projects remain planned, not implemented projects |

## Retained Resources

The project retains the scaffold icon, foreground icon, splash screen, Android host colors, and a reduced theme-aware palette. These resources keep the shell buildable but are temporary. They do not represent final branding or a completed accessibility review.

## Local Validation

Run validation from the repository root with the stable .NET 10 SDK selected by `global.json`. The .NET MAUI Android workload must already be available. If workload restore requests installation or elevation, stop and review that change rather than installing silently.

```powershell
dotnet --version
dotnet sln .\DcimCloudSync.slnx list
dotnet workload restore .\DcimCloudSync.slnx
dotnet restore .\DcimCloudSync.slnx
dotnet msbuild .\src\DcimCloudSync\DcimCloudSync.csproj -nologo -t:"Compile;_ReadAndroidManifest" -p:Configuration=Debug -p:DesignTimeBuild=false
dotnet msbuild .\src\DcimCloudSync\DcimCloudSync.csproj -nologo -t:"Compile;_ReadAndroidManifest" -p:Configuration=Release -p:DesignTimeBuild=false
dotnet list .\src\DcimCloudSync\DcimCloudSync.csproj package --vulnerable --include-transitive --no-restore
```

The package-free target sequence covers managed compilation, analyzers and source generators, MAUI XAML, Android resource compilation and designer output, generated Java wrappers, and pre-merge and merged manifests. It does not cover `javac`, final `aapt2` linking, D8 or R8, dex generation, APK or AAB packaging, signing, installation, deployment, or runtime behavior. It does not contact an emulator or physical device or invoke ADB.

`_ReadAndroidManifest` is an internal, version-sensitive .NET Android target, so revalidate its dependency chain after Android workload updates. Normal MAUI `Build` is not package-free in the pinned Microsoft.Android.Sdk 36.1.69: `AndroidBuildApplicationPackage=false` does not suppress APK or AAB creation, and `AndroidKeyStore=false` selects the generated debug keystore rather than disabling signing.

Phase 4 completed this package-free validation in Debug and Release with zero warnings and errors. Visual Studio 2026 deployment to a Pixel 7 over USB or wireless debugging remains a separate manual developer action.

The owner explicitly selected **Stop before device execution** on 2026-08-29. The spike is compile-complete but empirically incomplete. No fixture transfer, deployment, launch, permission exercise, or diagnostic run occurred, and no runtime open path was selected for production.

For a future explicitly approved device phase, the owner transfers the external fixture bundle, deploys through Visual Studio 2026, chooses each permission scenario in the visible system UI, invokes the diagnostic from the page, and records only sanitized evidence. No device, emulator, ADB, deployment, or runtime action has occurred for the current implementation.

## Limitations

The diagnostic does not implement or prove:

* Backup, Azure upload, provider integration, persistence, authentication, scheduling, resume, reconciliation, or deletion
* Access to protected photo-location metadata or exactness for location-bearing photos
* Complete Android API 33 through 36, vendor, MediaProvider, or multi-volume behavior
* HEIC, DNG, motion-photo, pending, trashed, mutation, removal, or stale-row behavior
* Production stream ownership, retry, remote verification, or exact original-byte guarantees

The temporary app-local contracts must be removed or redesigned before production Core contracts are introduced.
