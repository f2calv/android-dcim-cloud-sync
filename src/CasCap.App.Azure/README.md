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
- **Metadata:** the content SHA-256 is stored as `sha256` metadata, and the source item's size and
  modification time, in Unix seconds, as `sourcesize` and `sourcemodified`, so the quick check can
  recognize an unchanged item without reading it. The MIME type is stored as the content type, and the
  configured access tier is applied. Backups created before these keys existed fall back to the hash
  comparison.
- **Streaming:** content is streamed to the SDK, never buffered whole in memory.
- **Authentication:** a connection string, or a blob service URL with the signed-in user's token from the
  core [`IAccessTokenProvider`](../DcimCloudSync.Core/Abstractions/IAccessTokenProvider.cs), adapted to the
  SDK by [`AccessTokenProviderCredential`](Services/AccessTokenProviderCredential.cs). Azure types stay
  inside this project.
- **Lazy setup:** construction reads no configuration and performs no network I/O. The client is
  created on first use, and the container is created then only when configured; a failed creation is
  retried on the next operation. Settings are read through `IOptionsMonitor` on every operation, so a
  changed endpoint or container takes effect without a restart. `IsConfigured` reports whether a run
  can start, without network I/O.
- **Errors:** Azure failures surface as `BackupDestinationException`, whose message holds the
  operation and HTTP status only, never blob names, endpoints or credentials.

## Configuration

[`AzureBlobBackupConfig`](Models/AzureBlobBackupConfig.cs) binds the `AzureBlobBackup` section. The
Windows target reads it from environment variables prefixed `DcimCloudSync__`, for example
`DcimCloudSync__AzureBlobBackup__ConnectionString`, and offers **Back up** only when the connection
string is set. The Android target binds it from its **Settings** page and uses `BlobServiceUri`.

| Key | Default | Validation |
| --- | --- | --- |
| `ConnectionString` | None | Set exactly one of this or `BlobServiceUri`. `UseDevelopmentStorage=true` targets Azurite |
| `BlobServiceUri` | None | An `https` endpoint without a path; authenticates with the signed-in user's token |
| `ContainerName` | `media` | Required; a valid container name |
| `PhotoPrefix` | `photos` | Slashes are trimmed; empty for none |
| `VideoPrefix` | `videos` | Slashes are trimmed; empty for none |
| `AccessTier` | Account default | `Hot`, `Cool`, `Cold` or `Archive` |
| `ContainerCreationEnabled` | `false` | Enable for Azurite |

The connection string is a development credential. Supply it only through an environment variable
or another secret store, never tracked or packaged configuration. Device builds use `BlobServiceUri`,
with short-lived tokens and a no-delete role as decided in
[ADR 0001](../../docs/adr-0001-device-credentials.md).

## Dependencies

| Reference | Configuration | Use |
| --- | --- | --- |
| [DcimCloudSync.Core](../DcimCloudSync.Core/README.md) | All | Backup contract, models and exception |
| `CasCap.Api.Azure.Storage` project in a sibling `CasCap.Api.Azure` checkout | Debug | Create-only uploads and metadata reads |
| `CasCap.Api.Azure.Storage` 2.0.0 package | Release | Published create-only uploads and metadata reads |
| `Microsoft.Extensions.Options` | All | `IOptionsMonitor` configuration access |

Release builds restore `CasCap.Api.Azure.Storage` 2.0.0, which contains `UploadBlobIfNotExistsAsync`
and `GetBlobMetadataAsync`; Debug retains the sibling-source substitution. The library also brings
the Azure Queue and Table SDKs. This project is not packable and publishes no separate NuGet package.
