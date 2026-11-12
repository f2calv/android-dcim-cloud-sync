# CasCap.App.Tests

xUnit v3 unit tests for [CasCap.App](../CasCap.App/README.md), running on
Microsoft.Testing.Platform. They need no device, Android SDK, network or cloud resources. Each test
creates its own temporary folder of small synthetic text files with media extensions and deletes it
afterwards.

```powershell
dotnet test --project .\src\CasCap.App.Tests\CasCap.App.Tests.csproj
```

## Tests

| Class | Category | Methods | Test cases |
| --- | --- | --- | --- |
| [`FileSystemMediaSourceTests`](Tests/Unit/FileSystemMediaSourceTests.cs) | `Discovery` | 4 | 5 |
| [`MediaInventoryServiceTests`](Tests/Unit/MediaInventoryServiceTests.cs) | `Hashing` | 3 | 3 |
| [`BackupServiceTests`](Tests/Unit/BackupServiceTests.cs) | `Backup` | 15 | 17 |
| [`MediaStoreItemPathTests`](Tests/Unit/MediaStoreItemPathTests.cs) | `Discovery` | 3 | 20 |
| **Total** | | **25** | **45** |

The trait categories are `Discovery`, for finding and opening folder media and mapping MediaStore rows; `Hashing`, for scan
summaries, duplicates, unreadable files and cancellation; and `Backup`, for uploads, existing
matching, conflicting and unverified backups, a concurrent writer, an item changed after hashing,
unreadable files, destination failures, serialized runs, incremental runs from a checkpoint, quick and full checks,
per-item progress phases and the backup log. The `Backup` tests use the in-memory
`InMemoryBackupDestination`, `InMemoryCheckpointStore`, `InMemoryBackupLog` and `StaticOptionsMonitor`. `ChangingMediaSource` serves different content on its second open,
`GatedMediaSource` holds discovery open to observe overlapping runs, and `ChangeTrackingMediaSource` versions its items and counts opens. The `*_UnreadableItem`
tests make a file unreadable by holding it open with `FileShare.None`.

## Skipped Tests

None.

## Layout

```mermaid
graph TD
    Tests[Tests/] --> Folder[TemporaryMediaFolder.cs]
    Tests --> Destination[InMemoryBackupDestination.cs]
    Tests --> Progress[SynchronousProgress.cs]
    Tests --> Changing[ChangingMediaSource.cs]
    Tests --> Gated[GatedMediaSource.cs]
    Tests --> Tracking[ChangeTrackingMediaSource.cs]
    Tests --> Checkpoint[InMemoryCheckpointStore.cs]
    Tests --> Log[InMemoryBackupLog.cs]
    Tests --> Options[StaticOptionsMonitor.cs]
    Tests --> Unit[Unit/]
    Unit --> Source[FileSystemMediaSourceTests.cs]
    Unit --> Inventory[MediaInventoryServiceTests.cs]
    Unit --> Backup[BackupServiceTests.cs]
    Unit --> MediaStore[MediaStoreItemPathTests.cs]
```

## Dependencies

| Package | Version |
| --- | --- |
| `xunit.v3` | `4.0.2` |

| Project reference | Use |
| --- | --- |
| [CasCap.App](../CasCap.App/CasCap.App.csproj) | System under test |
