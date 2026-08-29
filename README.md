---
title: Android DCIM Cloud Sync
description: Privacy-focused Android DCIM backup to Azure Blob Storage, built with .NET 10 and .NET MAUI
---

## Overview

Android DCIM Cloud Sync is a planned open-source Android application for copying user-approved photos and videos from an Android device's DCIM collection to cloud storage. Azure Blob Storage is the first planned provider, and a Pixel 7 is the initial physical-device target. Future provider contracts must not make application and domain code depend directly on Azure SDK types.

The repository now contains a Visual Studio 2026 .NET 10 MAUI application reduced to an Android-only static Shell page. The root solution and centralized SDK, build, and package files provide a stable local build entry point.

> [!IMPORTANT]
> Media discovery, upload, persistence, authentication, background work, resume, deduplication, and deletion are not implemented. The current application is a learning and local-development shell only.

## Current Implementation

The current baseline provides:

* One static, accessible MAUI Shell page with no bound data or product services
* An Android-only `net10.0-android` application with a minimum Android API level of 33
* The permanent application ID `io.github.f2calv.androiddcimcloudsync`
* Android application-data backup disabled with `android:allowBackup="false"`
* Root `DcimCloudSync.slnx`, `global.json`, `Directory.Build.props`, and `Directory.Packages.props` build entry points

The retained icon, splash screen, and color palette come from the scaffold and are temporary. They are not final product branding.

## Motivation

This project is intentionally small and focused so it can serve as a practical way to learn .NET MAUI while solving a real mobile backup problem.

Transferring a large Android DCIM collection to a computer over USB can be unreliable. Managed photo services provide convenient automatic synchronization, but some download and export workflows may not return files with every original EXIF field intact, including geolocation metadata. Original-quality photos and increasingly large videos can also make general-purpose photo-service storage comparatively expensive for long-term retention.

Object storage provides a different trade-off. Uploading the original file bytes to cloud storage can preserve the source media and its embedded metadata, while cool and archive storage tiers can reduce long-term storage cost. Those tiers may have retrieval delays and additional access charges, so the application is intended as an independent archival backup rather than a replacement for a photo-browsing service.

## Planned Goals

* Discover user-approved photos and videos through Android's supported media APIs
* Stream media from content URIs without requiring filesystem paths or loading entire files into memory
* Preserve available original bytes and the operational metadata required for safe reconciliation
* Make interrupted work resumable and uploads idempotent without relying on filenames alone
* Propagate cancellation through discovery, hashing, persistence, retry, and upload operations
* Respect network, battery, and Android background-execution constraints
* Keep cloud credentials and personal media out of source control and logs
* Isolate Azure-specific behavior behind provider-neutral contracts

## Planned Initial Scope

The planned first release is copy-only. It is intended to:

* Run on Android, with a Pixel 7 as the initial physical-device target
* Read user-approved photos and videos from the DCIM media collection
* Copy content to a private Azure Blob Storage container
* Track backup state locally so work can resume after interruption
* Report progress and actionable failures without exposing personal data

Automatic deletion, bidirectional synchronization, and remote-to-device restore are out of scope until their safety behavior is designed and tested explicitly.

## Planned Architecture

The following diagram describes planned behavior, not the static shell that exists today.

```mermaid
flowchart LR
    A([Android DCIM]) --> B[Media discovery]
    B --> C[Backup queue]
    C --> D[Cloud storage provider]
    D --> E[(Azure Blob Storage)]
    C --> F[(Local backup state)]
```

The future Azure provider must remain compatible with an existing Blob container. Its non-secret configuration will support separate destination prefixes for JPG and MP4 media without forcing a new container or naming layout. Actual infrastructure identifiers, prefix values, endpoints, and credentials do not belong in tracked documentation or configuration.

## Security and Privacy

The static shell stores no credentials or backup state and sends no media or product data off the device. Its manifest currently declares only these network permissions:

* `android.permission.INTERNET` permits future network communication but is not exercised by the static shell
* `android.permission.ACCESS_NETWORK_STATE` permits future connectivity checks but is not exercised by the static shell

Android application-data backup is disabled with `android:allowBackup="false"`. Future persistent state requires a separate review of Android backup and device-transfer protections.

The following rules govern planned product work:

* Never embed Azure Storage account keys, connection strings, client secrets, signing passwords, or long-lived SAS values in the application package.
* Prefer short-lived, least-privilege access. Persist sensitive user-provided values only through .NET MAUI secure storage or a stronger platform-backed mechanism.
* Treat packaged configuration as public because values can be extracted from an APK or Android App Bundle.
* Do not log media contents, access tokens, SAS query strings, content URIs, full local paths, or personally identifying filenames.
* Request the minimum Android permissions required. Do not introduce broad all-files access without explicit approval and documented justification.
* Keep signing keys, keystores, local configuration, personal media, and generated application packages outside source control.
* Keep v1 copy-only. Do not add local or remote deletion or bidirectional synchronization without explicit approval and dedicated safety tests.

See [SECURITY.md](SECURITY.md) for vulnerability reporting and credential-handling expectations.

## Development Prerequisites

* Visual Studio 2026 with .NET MAUI and Android development workloads
* Stable .NET SDK `10.0.400`, or a compatible later stable .NET 10 feature band selected by `global.json`
* Android SDK and a Pixel 7 configured for USB or wireless debugging
* PowerShell 7
* Plain Git command-line tooling
* Pre-commit for full repository lint validation

Azure resources are not required for the current shell because no provider exists.

## Local Development Loop

Use the repository root as the working directory. Open `DcimCloudSync.slnx` in Visual Studio 2026, select the Android application and the paired Pixel 7, then build and deploy interactively over USB or Android wireless debugging. Device pairing, deployment, and launch remain deliberate developer actions in Visual Studio.

The command-line build entry point is the same root solution. With the required SDK and Android workload already installed, these commands restore packages, compile without producing an application package, and inspect dependencies:

```powershell
dotnet workload restore .\DcimCloudSync.slnx
dotnet restore .\DcimCloudSync.slnx
dotnet build .\DcimCloudSync.slnx --configuration Debug --no-restore -p:AndroidKeyStore=false -p:AndroidBuildApplicationPackage=false
dotnet build .\DcimCloudSync.slnx --configuration Release --no-restore -p:AndroidKeyStore=false -p:AndroidBuildApplicationPackage=false
dotnet list .\src\DcimCloudSync\DcimCloudSync.csproj package --vulnerable --include-transitive --no-restore
```

Stop and review the environment if workload restore requests an installation or elevation decision. These commands do not run tests, launch an emulator, contact a device, publish, or deploy.

See [src/DcimCloudSync/README.md](src/DcimCloudSync/README.md) for project-specific details.

## Automation and Distribution Status

The existing GitHub Actions workflow performs repository linting only. Android compilation in CI, automated device deployment, production signing, release artifacts, and Google Play publishing are explicitly deferred. No Play Store readiness or signed-release capability is implied by the local shell.

## Repository Conventions

* Application and future test projects live under `src/`.
* The root solution uses the modern `.slnx` format.
* Shared MSBuild properties and NuGet versions are centralized in `Directory.Build.props` and `Directory.Packages.props`.
* `global.json` selects the stable .NET 10 SDK policy.
* GitVersion provides repository versioning.
* Linting runs with `pre-commit run --all-files`; the pre-push hook is opt-in through `pre-commit install`.
* Use plain Git CLI for every repository operation. Do not use GitKraken.
* Copilot conventions live in `.github/copilot-instructions.md` and `.github/instructions/`.

## License

This project is released into the public domain under [The Unlicense](LICENSE).
