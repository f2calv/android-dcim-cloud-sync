---
description: '.NET 10 MAUI solution, project, central package management and SDK conventions.'
applyTo: '**/*.csproj,**/*.slnx,**/Directory.Build.props,**/Directory.Packages.props,**/global.json'
---

# .NET Solution and Build Structure

## Initial Scaffolding

- Let Visual Studio 2026 create the initial .NET MAUI solution and application project.
- Use the modern `.slnx` solution format and place all projects under `src/`.
- Target `net10.0-android` initially. Do not add other platform target frameworks unless requested.
- Keep the MAUI application non-packable. NuGet packaging metadata belongs only in a future reusable library that is intentionally published.

## Central Build Configuration

- Put shared properties in root `Directory.Build.props`: C# 14, nullable reference types, implicit usings, warnings, deterministic CI builds and non-packable defaults.
- Keep individual project files focused on project-specific target frameworks, MAUI settings, Android properties and references.
- Centralize warning suppressions with an explanatory comment. Do not suppress a warning solely to make a build pass.

## Central Package Management

- Centralize every NuGet version in root `Directory.Packages.props` with `ManagePackageVersionsCentrally` enabled.
- Project files use versionless `PackageReference` items.
- Keep Azure SDK dependencies inside the Azure provider project. Application and abstraction projects must not reference them directly.

## SDK and Workloads

- Add or update `global.json` only after Visual Studio has selected the installed .NET 10 feature band, then use an explicit roll-forward policy suitable for CI and contributors.
- Keep MAUI and Android workload requirements aligned between local development and CI.
- Build the entire solution after refactoring. Do not deploy to an emulator or physical device unless the user requests it.
