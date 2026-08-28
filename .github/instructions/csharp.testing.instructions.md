---
description: 'xUnit conventions for unit, integration and Android device tests.'
applyTo: '**/*Tests/**/*.cs'
---

# Testing

## Test Categories

- Put self-contained tests under `Tests/Unit/`. They must not require Android, Azure, network access, dependency injection or real media.
- Put provider and persistence tests under `Tests/Integration/` and mark them with `[Trait("Category", "Integration")]`.
- Keep emulator or physical-device tests in a clearly named Android test project and mark them with `[Trait("Category", "Device")]`.
- Use Azurite or isolated disposable Azure resources for integration tests. Never target a personal storage account or DCIM collection.

## Test Data and Isolation

- Use generated synthetic images, videos and metadata with no personal content or EXIF location data.
- Use temporary directories and unique container names. Clean up resources in `finally`, `DisposeAsync`, or fixture teardown.
- Mock at provider and Android media-source boundaries rather than mocking Azure SDK internals throughout the application.
- Cover cancellation, retry, duplicate detection, partial upload, lost connectivity, permission denial and process-restart scenarios.
- Do not share mutable static state between tests.

## Naming and Assertions

- Name tests after the method or behavior being tested. Avoid verbose BDD sentence names.
- Consolidate input-only variations into `[Theory]` tests with `[InlineData]` or dedicated test data.
- Prefer specific assertions such as `Assert.Equal`, `Assert.Contains` and `Assert.ThrowsAsync`.
- Every test must verify meaningful behavior. Do not add placeholder assertions or timing-only tests.
- Write diagnostics through `ITestOutputHelper` or a logger wired to test output. Never print secrets, URIs containing SAS tokens, or personal media identifiers.

## Execution

- Do not run tests without user approval. State whether the requested run needs only the host, Azurite, an Android emulator, or a physical device.
- Keep unit tests runnable without cloud credentials and without an Android workload where practical.
