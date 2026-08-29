---
title: DcimCloudSync Android Application
description: Project reference for the static .NET MAUI Android host and local validation baseline
---

## Purpose

`DcimCloudSync` is the Android .NET MAUI host, UI composition root, and Android platform boundary for Android DCIM Cloud Sync. The project currently provides a minimal local development surface for learning MAUI and validating the Android host.

## Current Implementation

The application contains one static, accessible Shell page. It has no media discovery, upload, provider implementation, persistence, authentication, background work, resume, deduplication, or deletion behavior. The page does not bind to a view model or execute product operations.

## Target and Identity

* Target framework: `net10.0-android`
* Minimum Android API level: 33 (Android 13)
* Current workload-owned target API level: 36
* Application ID: `io.github.f2calv.androiddcimcloudsync`
* Initial physical-device target: Pixel 7

The application ID is permanent and must not be changed casually. The .NET Android workload owns the target API level, so release planning must recheck it against current platform and Google Play requirements.

## Android Backup and Permissions

Android application-data backup is disabled with `android:allowBackup="false"`. The current shell has no credentials or product state. Any future persistence requires a separate review of Android backup and device-transfer protections.

The manifest declares only two permissions:

* `android.permission.INTERNET` permits future network communication, although the static shell makes no network requests
* `android.permission.ACCESS_NETWORK_STATE` permits future connectivity checks, although the static shell does not inspect network state

No media, location, notification, foreground-service, boot, or broad storage permission is declared.

## Services

No product service is registered. `MauiProgram` configures only the MAUI host and debug logging for Debug builds. Media access, backup orchestration, cloud providers, persistence, identity, scheduling, and deletion remain future concerns.

## Configuration and Secrets

The project has no application configuration file and consumes no runtime secret. Every asset packaged into an APK or Android App Bundle must be treated as public.

The future Azure provider must remain compatible with an existing Blob container and support separate configurable destination prefixes for JPG and MP4 media. The container and prefix selections are non-secret configuration concerns, but no real container name, prefix, endpoint, account identifier, path, SAS value, or token belongs in tracked files or logs.

## NuGet Dependencies

Project package references are versionless. Root `Directory.Packages.props` centrally supplies these versions:

| Package                              | Version    |
| ------------------------------------ | ---------- |
| `Microsoft.Extensions.Logging.Debug` | `10.0.11`  |
| `Microsoft.Maui.Controls`            | `10.0.100` |

## Project References

The project has no project references. Provider-neutral core contracts and an Azure provider are planned architectural boundaries, not implemented projects.

## Retained Resources

The project retains the scaffold icon, foreground icon, splash screen, Android host colors, and a reduced theme-aware palette. These resources keep the shell buildable but are temporary. They do not represent final branding or a completed accessibility review.

## Local Validation

Run validation from the repository root with the stable .NET 10 SDK selected by `global.json`. The .NET MAUI Android workload must already be available. If workload restore requests installation or elevation, stop and review that change rather than installing silently.

```powershell
dotnet --version
dotnet sln .\DcimCloudSync.slnx list
dotnet workload restore .\DcimCloudSync.slnx
dotnet restore .\DcimCloudSync.slnx
dotnet build .\DcimCloudSync.slnx --configuration Debug --no-restore -p:AndroidKeyStore=false -p:AndroidBuildApplicationPackage=false
dotnet build .\DcimCloudSync.slnx --configuration Release --no-restore -p:AndroidKeyStore=false -p:AndroidBuildApplicationPackage=false
dotnet list .\src\DcimCloudSync\DcimCloudSync.csproj package --vulnerable --include-transitive --no-restore
```

These commands do not run tests, publish, install or launch the application, contact an emulator or physical device, invoke ADB, sign for release, or deploy. Visual Studio 2026 deployment to a Pixel 7 over USB or wireless debugging remains a separate manual developer action.
