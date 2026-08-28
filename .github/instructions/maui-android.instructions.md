---
description: 'Android and .NET MAUI conventions for media access, background backup, UI and secure storage.'
applyTo: '**/*.cs,**/*.xaml,**/AndroidManifest.xml'
---

# .NET MAUI and Android

## Platform Scope

- Target .NET 10 Android first. Do not add iOS, macOS or Windows targets without explicit user approval.
- Keep Android APIs behind an interface and place implementations under `Platforms/Android` or an Android-specific project.
- Keep view code focused on rendering, navigation and user interaction. Backup orchestration and provider calls belong in services.
- Dispatch only UI updates to the main thread. Discovery, hashing, persistence and upload work must stay off it.

## Media Access and Permissions

- Discover media through Android `MediaStore` and access content through `ContentResolver` streams. Do not assume a content URI maps to a stable filesystem path.
- Use Android scoped storage and request the minimum runtime permissions for the device API level.
- Handle permission denial, partial photo access, permission revocation and empty results without crashing or repeatedly prompting.
- Do not request `MANAGE_EXTERNAL_STORAGE` or bypass scoped storage without explicit approval and documented justification.
- Preserve the source media's stable identity, relative path, display name, MIME type, size and modification metadata where available.

## Background Work

- Design uploads around Android lifecycle and background-execution limits. Do not rely on an in-process timer or a permanently alive MAUI page.
- Use an Android-supported scheduler for deferrable work and a foreground service with a visible notification only when long-running user-visible work requires it.
- Respect configured connectivity, roaming, metered-network, charging and battery constraints.
- Persist queue and checkpoint state before starting expensive work so backup can resume after process or device restart.
- Bound concurrency and retry with exponential backoff plus jitter. Treat authentication and permission failures as non-transient.

## Secure Local State

- Store secrets only through .NET MAUI `SecureStorage` or an explicitly reviewed stronger Android mechanism.
- Store non-sensitive preferences separately through `Preferences` or the application database.
- Never store raw account keys or long-lived credentials when short-lived delegated access can be used.
- Clear sensitive values on sign-out or credential replacement and provide an explicit way to reset local application state.

## XAML and Accessibility

- Use compiled bindings with `x:DataType` where supported.
- Keep styles and reusable resources centralized rather than duplicating values across pages.
- Provide semantic descriptions for actionable controls, progress indicators and status changes.
- Ensure backup state is understandable without relying on color alone and that controls remain usable with large text.
