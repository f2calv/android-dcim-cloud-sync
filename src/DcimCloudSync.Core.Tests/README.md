# DcimCloudSync.Core.Tests

xUnit v3 unit tests for [DcimCloudSync.Core](../DcimCloudSync.Core/README.md), running on
Microsoft.Testing.Platform. They need no device, Android SDK, network or cloud resources. Each test
creates its own temporary folder of small synthetic text files with media extensions and deletes it
afterwards.

```powershell
dotnet test --project .\src\DcimCloudSync.Core.Tests\DcimCloudSync.Core.Tests.csproj
```

## Tests

| Class | Category | Methods | Test cases |
| --- | --- | --- | --- |
| [`FileSystemMediaSourceTests`](Tests/Unit/FileSystemMediaSourceTests.cs) | `Discovery` | 4 | 5 |
| [`MediaInventoryServiceTests`](Tests/Unit/MediaInventoryServiceTests.cs) | `Hashing` | 3 | 3 |
| [`BackupServiceTests`](Tests/Unit/BackupServiceTests.cs) | `Backup` | 6 | 7 |
| **Total** | | **13** | **15** |

The trait categories are `Discovery`, for finding and opening folder media; `Hashing`, for scan
summaries, duplicates, unreadable files and cancellation; and `Backup`, for uploads, existing
matching, conflicting and unverified backups, a concurrent writer, an item changed after hashing,
unreadable files and destination failures. The `Backup` tests use the in-memory
`InMemoryBackupDestination`, and `ChangingMediaSource` serves different content on its second open. The `*_UnreadableItem`
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
    Tests --> Unit[Unit/]
    Unit --> Source[FileSystemMediaSourceTests.cs]
    Unit --> Inventory[MediaInventoryServiceTests.cs]
    Unit --> Backup[BackupServiceTests.cs]
```

## Dependencies

| Package | Version |
| --- | --- |
| `xunit.v3` | `4.0.2` |

| Project reference | Use |
| --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/DcimCloudSync.Core.csproj) | System under test |
