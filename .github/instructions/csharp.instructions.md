---
description: 'C# conventions for the .NET MAUI application, provider libraries and services.'
applyTo: '**/*.cs'
---

# C# and .NET

## Style

- Follow `.editorconfig`: 4-space indentation, LF line endings, file-scoped namespaces, Allman braces, alphabetical using directives, nullable reference types and C# 14.
- Prefer `var` when the type is apparent from the right-hand side.
- Prefer primary constructors for dependency-injected types. Order injected parameters as logger, options, then application services.
- Mark concrete classes `sealed` unless inheritance is an intentional, documented extension point.
- Put each top-level type in a file named after the type. `_Enums.cs` may group project enums, and an underscore-prefixed configuration file may group one root configuration type with closely related nested configuration types.
- Name interfaces with an `I` prefix and place provider-neutral contracts in an Abstractions folder and namespace.
- Every project has a root-level `GlobalUsings.cs` file. Do not place global using directives in arbitrary source files.
- Use records for immutable values and configuration where value equality is useful.
- Avoid magic strings. Centralize repeated identifiers in constants or strongly typed values.

## Async and Cancellation

- Use asynchronous APIs for media, database, network and storage I/O.
- Propagate `CancellationToken` through every asynchronous layer and pass it to framework and SDK calls.
- Return a task directly from a thin pass-through method instead of adding an unnecessary async state machine.
- Use `Task` when an operation normally performs asynchronous I/O. Use `ValueTask` only when synchronous completion is common and measured.
- Do not block with `.Result`, `.Wait()`, or synchronous-over-asynchronous wrappers.
- Use `ConfigureAwait(false)` only in provider-neutral library code that does not update MAUI UI state. UI and lifecycle code must resume through the appropriate dispatcher.

## Resource and Memory Safety

- Stream photos and videos. Never call APIs such as `ReadAllBytes` for media uploads.
- Dispose streams, responses, database objects and cancellation registrations deterministically with `using` or `await using`.
- Do not retain Android `Activity`, `Context`, view or page instances in singleton services.
- Prefer bounded queues and explicit backpressure over unbounded in-memory work collections.

## Logging

- Use `ILogger<T>` and structured message templates. Do not use `Console.WriteLine` or `Debug.WriteLine` for application diagnostics.
- Include `{ClassName}` as the first structured field and pass `nameof(EnclosingType)` as its value.
- Use PascalCase template names and pass values separately rather than interpolating strings.
- Never log credentials, SAS query strings, media content, full paths, content URIs, or unredacted filenames.
- Use source-generated `[LoggerMessage]` methods on upload, discovery, retry and progress hot paths.

## Documentation and Validation

- Add concise XML documentation to public and internal types and members. Document contracts fully on interfaces and use `/// <inheritdoc/>` on implementations.
- Validate configuration and external input at boundaries. Fail with actionable, non-sensitive messages.
- Use provider-neutral exceptions or result types outside provider implementations. Do not leak Azure SDK types into application or domain contracts.
