---
description: 'GitHub Actions conventions for linting, .NET MAUI builds, Android artifacts and security.'
applyTo: '.github/workflows/**,.github/actions/**,**/action.yml,**/action.yaml'
---

# GitHub Actions

## General

- Set workflow-level `permissions: {}` and grant each job only the permissions it needs.
- Pin actions to major version tags. Use `fetch-depth: 0` when GitVersion needs full history.
- Use 2-space YAML indentation and leave one blank line between steps.
- Use kebab-case for inputs and outputs, uppercase underscore names for environment variables and secrets.
- Keep input descriptions short and move rationale into comments above the input.
- Prefer reusable workflows from `f2calv/gha-workflows` for shared logic. Prefix repository-local reusable workflow filenames with `_`.

## Android Builds

- Keep the bootstrap CI lint-only until Visual Studio creates the solution and the required .NET MAUI workload is known.
- After scaffolding, restore the declared workloads and build the whole solution before producing Android artifacts.
- Do not publish, sign, or upload an APK or Android App Bundle from pull requests from forks.
- Keep keystores and passwords in approved secret storage. Reconstruct temporary signing files only for trusted release jobs and remove them before the job ends.
- Upload only intended build artifacts. Never upload local configuration, logs containing media identifiers, or files copied from a device.

## Security

- Do not print secrets or pass them on command lines when a standard input or environment-based mechanism exists.
- Do not grant cloud identity permissions to lint, unit-test, or untrusted pull-request jobs.
- Use short-lived federated identity for Azure automation where possible instead of stored client secrets.
- Separate unsigned validation builds from trusted release-signing jobs.
