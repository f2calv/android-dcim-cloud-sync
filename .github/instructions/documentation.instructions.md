---
description: 'README synchronization, privacy documentation and Mermaid conventions.'
applyTo: '**/*.md'
---

# Documentation

## README Consistency

- Keep the root README synchronized with implemented features, supported Android versions, permissions, configuration, provider support and development prerequisites.
- Every project added under `src/` receives a README in the same change. Document its purpose, services, configuration, NuGet dependencies and project references.
- Update documentation and diagrams in the same change as a rename, project move, provider addition, permission change or architecture refactoring.
- Do not claim encryption, background reliability, resume behavior, deduplication or platform support until the implementation and tests provide it.
- Use synthetic examples. Never include real storage endpoints, tokens, device paths, filenames, screenshots of personal media or EXIF data.
- Markdown table separator rows use spaces around pipes, for example `| --- | --- |`.

## Security and Privacy Documentation

- Document every Android permission and why it is required.
- State where credentials and backup state are stored and what data leaves the device.
- Document destructive behavior, retention, conflict handling and deletion semantics before enabling those features.
- Keep the security policy aligned with supported releases and the repository's private reporting mechanism.

## Mermaid Diagrams

- Use `flowchart` for media discovery, queueing, upload and CI flows.
- Use `graph` for project and provider dependencies.
- Use `sequenceDiagram` for Android lifecycle, credential renewal and upload interactions.
- Group Android, application, persistence and cloud-provider components in labeled subgraphs where this improves clarity.
- Keep every diagram synchronized with the implementation.
