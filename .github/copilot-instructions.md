# Copilot Instructions

## Shared Instructions

Shared Copilot instructions, skills and prompts are maintained centrally in the
[account-level .github repository](https://github.com/f2calv/.github). They are deliberately not
copied here. Clone that repository and add it to the VS Code workspace, or link its instruction
folders into `~/.copilot/`. If the shared files are unavailable, stop rather than guessing the
conventions.

The scoped files under `.github/instructions/` add Android DCIM Cloud Sync requirements. Everything
below is specific to this repository.

## Current Baseline

This is a .NET 10 MAUI app with one Shell page. Android is the product platform; a Windows target
exists only for debugging services on a workstation. `DcimCloudSync.Core` holds the platform-neutral
media and backup contracts, a folder-backed media source, an inventory scan and a create-only backup
service. `DcimCloudSync.Storage.Azure` implements the backup destination with Azure Blob Storage and is
referenced only by the Windows target. Android media access, device credentials, background work and
persistence do not exist yet.

- Build from `DcimCloudSync.slnx`; central SDK, build and package policy lives in the root files.
- Keep `net10.0-android`, minimum API 33 and application ID
  `io.github.f2calv.androiddcimcloudsync`.
- Keep the Windows target development-only: add it only when building on Windows, keep it
  unpackaged, and never distribute it or let CI build it.
- Keep Azure SDK and `CasCap.Api.Azure.Storage` references inside `DcimCloudSync.Storage.Azure`.
  Debug references the sibling `CasCap.Api.Azure` checkout; Release uses the published package.
- Backups are create-only: never overwrite or delete a blob, and report hash conflicts instead.
- Integration tests use Azurite from the root `docker-compose.yml` and per-test containers.
- Keep `android:allowBackup="false"`.
- The manifest declares only Internet and network-state permissions. Add no media or background
  permission before the requiring behavior and privacy documentation exist.
- Keep Android CI compilation, device deployment, signing, packaging and Play publishing deferred.
- Preserve the tracked Visual Studio scaffold rather than replacing the application project.

## Architecture Boundaries

- Separate UI, Android integration, orchestration, persistence and provider code.
- Put provider-neutral contracts under `Abstractions`; Azure SDK types must not cross them.
- Keep Android-specific code under `Platforms/Android` or an explicitly named Android project.
- Stream content URIs without filesystem paths or whole-file buffering.
- Design future backup work to be idempotent, resumable and cancellation-aware.
- Do not add local or remote deletion or bidirectional synchronization without explicit approval and
  dedicated safety tests.

## Privacy and Workflow

- Treat every packaged file as public. Never package or log credentials, SAS query strings, media
  content, content URIs, full paths or identifying filenames.
- Store future secrets only in platform-backed secure storage and use synthetic test media.
- Never add `MANAGE_EXTERNAL_STORAGE`.
- Keep source and tests under `src/`, tooling under dot-prefixed root folders, and documentation
  synchronized with permissions and implementation.
- Do not run tests, ADB, device deployment or distributable packaging automatically.
