# Copilot Instructions

## Repository Purpose

Android DCIM Cloud Sync is a privacy-focused Android application built with .NET 10 and .NET MAUI. It is intended to copy user-approved photos and videos from Android's DCIM media collection to Azure Blob Storage. Azure is the first planned provider, but application and domain code must not assume it is the only provider.

The repository contains a Visual Studio 2026 .NET 10 Android MAUI scaffold reduced to one static Shell page. Backup functionality has not begun. Evolve the tracked scaffold rather than hand-creating or replacing its application project unless the user asks explicitly.

## Current Application Baseline

- Build from the root `DcimCloudSync.slnx` solution. Root `global.json`, `Directory.Build.props`, and `Directory.Packages.props` own SDK, build, and package policy.
- Keep the application Android-only on `net10.0-android` with minimum API 33 unless a separately approved compatibility change requires otherwise.
- Preserve the permanent application ID `io.github.f2calv.androiddcimcloudsync`.
- Keep Android application-data backup disabled with `android:allowBackup="false"`.
- The current manifest declares only Internet and network-state permissions. Do not add media or background permissions before implementing and documenting the behavior that requires them.
- No product services, persistence, configuration, credentials, or secrets exist yet.
- The scaffold icon, splash screen, and palette are temporary and are not final product branding.
- CI Android compilation, automated deployment, signing, release packaging, and Google Play publishing are deferred.

## Instruction Files

Detailed conventions live under `.github/instructions/` and are applied by file type:

| File | Applies to | Covers |
| --- | --- | --- |
| `csharp.instructions.md` | `**/*.cs` | C# style, async code, logging, memory use and documentation |
| `csharp.testing.instructions.md` | `**/*Tests/**/*.cs` | Unit, integration and device-test conventions |
| `maui-android.instructions.md` | MAUI and Android files | Media access, permissions, background work and secure storage |
| `configuration.instructions.md` | Configuration files and models | Public defaults, secret storage and validation |
| `dotnet.instructions.md` | Project and solution files | .NET 10, `.slnx`, central build properties and packages |
| `github-actions.instructions.md` | GitHub Actions | Workflow style, permissions, Android signing and secret safety |
| `documentation.instructions.md` | `**/*.md` | README synchronization, privacy documentation and Mermaid diagrams |

## Copilot Workflow

- Never run tests automatically. Tests may require an emulator, physical device, Android permissions, Azurite, or an Azure resource. Ask before running any test command.
- After a refactoring, build the entire root `DcimCloudSync.slnx` solution. Ask before any build that implicitly runs tests, creates a distributable package, or deploys to a device.
- Use plain Git command-line operations only. Do not use GitKraken.
- Preserve Git history for renames and moves. Use plain `git mv` first, then edit the file at its new path.
- Keep source and test projects under `src/`. Keep tooling under dot-prefixed root folders.
- Use the modern `.slnx` solution format and central package management.
- Keep the README and architecture diagrams synchronized with implementation and permission changes.
- Do not commit generated APK or Android App Bundle files, signing material, local configuration, media samples copied from a real device, or scan reports containing sensitive values.
- Treat Visual Studio 2026 deployment to the Pixel 7 over USB or wireless debugging as a manual developer action. Do not run ADB or deploy automatically.

## Architecture Boundaries

- Keep UI, Android platform integration, backup orchestration, persistence, and cloud-provider code separate.
- Define provider-neutral contracts in an Abstractions namespace and folder. Azure SDK types must not cross those contracts.
- Keep Android-specific code under `Platforms/Android` or an explicitly named Android integration project.
- Stream media from Android content URIs to the provider. Do not require a filesystem path and do not buffer complete photos or videos in memory.
- Make backup operations idempotent and resumable. Use stable media identity plus verified remote state rather than filenames alone.
- Propagate `CancellationToken` through discovery, hashing, persistence, retry, and upload operations.
- Treat local and remote deletion as destructive operations. Do not add deletion or bidirectional synchronization without explicit user approval and dedicated safety tests.

## Privacy and Security

- Never embed an Azure Storage account key, connection string, client secret, signing password, or long-lived SAS in source, configuration, resources, logs, tests, screenshots, or an application package.
- Treat every file packaged into an APK or Android App Bundle as public.
- Prefer short-lived, least-privilege access. Store sensitive user-provided values only through .NET MAUI `SecureStorage` or a stronger platform-backed mechanism.
- Store non-sensitive preferences separately from secrets.
- Never log access tokens, SAS query strings, media contents, full local paths, or personally identifying filenames. Prefer counts, durations, provider-neutral result codes, and redacted identifiers.
- Request the minimum Android permissions required. Do not add `MANAGE_EXTERNAL_STORAGE` without explicit approval and documented justification.
- Use synthetic test media and placeholder cloud endpoints. Never copy personal DCIM content into the repository.

## Repository Structure

The current root layout is:

```text
/
├── .github/                 # Workflows and Copilot conventions
├── src/                     # Application, libraries and tests
├── docs/                    # Additional documentation
├── DcimCloudSync.slnx       # Root solution and local build entry point
├── Directory.Build.props   # Shared MSBuild properties
├── Directory.Packages.props # Central NuGet versions
├── GitVersion.yml
├── global.json             # Stable .NET 10 SDK policy
├── LICENSE
└── README.md
```

## Public Repository Hygiene

- Use placeholders such as `https://mystorageaccount.blob.core.windows.net` and the empty GUID in tracked examples.
- Revoke a credential immediately if it is exposed. Removing it from a later commit does not remove it from Git history.
- Report vulnerabilities through the process in `SECURITY.md`, not through public issue details.
