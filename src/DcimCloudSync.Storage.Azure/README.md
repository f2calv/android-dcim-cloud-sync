# DcimCloudSync.Storage.Azure

Azure Blob Storage backup destination for Android DCIM Cloud Sync. It implements the core
[`IBackupDestination`](../DcimCloudSync.Core/Abstractions/IBackupDestination.cs) contract on top of the
example storage library, the public `CasCap.Api.Azure.Storage` package; other libraries could serve the
same contract.

## Destination Behavior

[`AzureBlobBackupDestination`](Services/AzureBlobBackupDestination.cs) stores one block blob per item:

- **Name:** the photo or video prefix, then the item's relative path, for example
  `photos/Camera/IMG_0001.jpg`. An empty prefix stores the relative path alone, so an existing
  container layout can be kept.
- **Create-only:** uploads are conditional on `If-None-Match: *`. An existing blob is never
  overwritten; the core backup service then compares hashes.
- **Metadata:** the content SHA-256 is stored as `sha256` metadata, the MIME type as the content type,
  and the configured access tier is applied.
- **Streaming:** content is streamed to the SDK, never buffered whole in memory.
- **Lazy setup:** construction reads no configuration and performs no network I/O. The client is
  created on first use, and the container is created then only when configured; a failed creation is
  retried on the next operation.
- **Errors:** Azure failures surface as `BackupDestinationException`, whose message holds the
  operation and HTTP status only, never blob names, endpoints or credentials.

## Configuration

[`AzureBlobBackupConfig`](Models/AzureBlobBackupConfig.cs) binds the `AzureBlobBackup` section. The
Windows target reads it from environment variables prefixed `DcimCloudSync__`, for example
`DcimCloudSync__AzureBlobBackup__ConnectionString`, and offers **Back up** only when the connection
string is set.

| Key | Default | Validation |
| --- | --- | --- |
| `ConnectionString` | None | Required. `UseDevelopmentStorage=true` targets Azurite |
| `ContainerName` | `media` | Required; a valid container name |
| `PhotoPrefix` | `photos` | Slashes are trimmed; empty for none |
| `VideoPrefix` | `videos` | Slashes are trimmed; empty for none |
| `AccessTier` | Account default | `Hot`, `Cool`, `Cold` or `Archive` |
| `ContainerCreationEnabled` | `false` | Enable for Azurite |

The connection string is a development credential. Supply it only through an environment variable
or another secret store, never tracked or packaged configuration. A device build needs short-lived,
least-privilege credentials, which are not designed yet.

## Dependencies

| Reference | Configuration | Use |
| --- | --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/README.md) | All | Backup contract, models and exception |
| `CasCap.Api.Azure.Storage` project in a sibling `CasCap.Api.Azure` checkout | Debug | Create-only uploads and metadata reads |
| `CasCap.Api.Azure.Storage` 2.0.0 package | Release | Published create-only uploads and metadata reads |
| `Microsoft.Extensions.Options` | All | `IOptions` configuration access |

Release builds restore `CasCap.Api.Azure.Storage` 2.0.0, which contains `UploadBlobIfNotExistsAsync`
and `GetBlobMetadataAsync`; Debug retains the sibling-source substitution. The library also brings
the Azure Queue and Table SDKs. This project is not packable and publishes no separate NuGet package.
