# DcimCloudSync.Core

Platform-neutral contracts and services for Android DCIM Cloud Sync. The library has no MAUI,
Android or Azure dependency, so it can be developed and tested on any workstation.

## Contracts and Services

| Type | Responsibility |
| --- | --- |
| [`IMediaSource`](Abstractions/IMediaSource.cs) | Discovers photos and videos and opens each as a caller-owned read-only stream |
| [`MediaItem`](Models/MediaItem.cs) | Source identity, relative path, display name, MIME type, size and modification time of one item |
| [`FileSystemMediaSource`](Services/FileSystemMediaSource.cs) | `IMediaSource` over a local folder, standing in for Android MediaStore during development |
| [`MediaInventoryService`](Services/MediaInventoryService.cs) | Streams and SHA-256 hashes every discovered item, reporting progress and a summary |
| [`MediaInventoryProgress`](Models/MediaInventoryProgress.cs) | Items processed and bytes read so far |
| [`MediaInventorySummary`](Models/MediaInventorySummary.cs) | Item count, bytes read, duplicates, unreadable items and elapsed time |

```mermaid
flowchart LR
    A[Scan request] --> B[IMediaSource discovery]
    B --> C[Open stream]
    C --> D[SHA-256 with a pooled 80 KiB buffer]
    D --> E[Progress and summary]
```

`FileSystemMediaSource` recurses through subfolders and skips hidden and system files, inaccessible
folders and files without a known photo or video extension. It uses each item's relative path,
with `/` separators, as its source identity and rejects any item that resolves outside the
configured folder. A missing folder raises `DirectoryNotFoundException` without the path in its
message.

`MediaInventoryService` counts an item as unreadable when opening or reading it raises an
`IOException` or `UnauthorizedAccessException`, then continues with the next item. Duplicates are
counted within one scan by comparing hashes; nothing is persisted. Cancellation stops discovery
and the current read.

Item names and paths can identify a person, so logs contain only counts, item ordinal numbers,
byte lengths and exception type names.

## Configuration

[`FileSystemMediaSourceConfig`](Models/FileSystemMediaSourceConfig.cs) binds the
`FileSystemMediaSource` section:

| Key | Default | Validation |
| --- | --- | --- |
| `RootPath` | `DcimCloudSync` inside the user's Pictures folder | Required, non-empty |

The library defines no configuration provider; the host binds the section.

## Dependencies

| Package | Version | Use |
| --- | --- | --- |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.14` | `ILogger` and source-generated log messages |
| `Microsoft.Extensions.Options` | `10.0.14` | `IOptions` access to the media source configuration |

The project has no project references and is not packable.
