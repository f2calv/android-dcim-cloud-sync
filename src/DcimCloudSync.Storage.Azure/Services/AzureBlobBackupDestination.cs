namespace DcimCloudSync.Services;

/// <summary>
/// Stores one block blob per media item in Azure Blob Storage, named by media-type prefix and relative path.
/// </summary>
/// <remarks>
/// Uploads are create-only and record the content SHA-256 as blob metadata. Constructing the destination reads no
/// configuration and performs no network I/O; the client is created on first use, and a missing container is created
/// then only when configured. A failed creation is retried on the next operation.
/// </remarks>
/// <param name="config">The destination configuration.</param>
public sealed class AzureBlobBackupDestination(IOptions<AzureBlobBackupConfig> config) : IBackupDestination
{
    /// <summary>The blob metadata key that records the content SHA-256.</summary>
    public const string Sha256MetadataKey = "sha256";

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private ContainerStorage? _storage;

    /// <inheritdoc/>
    public async Task<BackupRecord?> FindBackupAsync(MediaItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        try
        {
            var storage = await GetStorageAsync(cancellationToken).ConfigureAwait(false);
            var metadata = await storage.GetBlobMetadataAsync(GetBlobName(item), cancellationToken).ConfigureAwait(false);
            if (metadata is null)
                return null;

            var sha256 = metadata.FirstOrDefault(entry =>
                string.Equals(entry.Key, Sha256MetadataKey, StringComparison.OrdinalIgnoreCase)).Value;
            return new BackupRecord(sha256);
        }
        catch (RequestFailedException ex)
        {
            throw new BackupDestinationException($"Azure Blob Storage rejected a backup lookup with status {ex.Status}.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateBackupAsync(MediaItem item, Stream content, string sha256, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        try
        {
            var storage = await GetStorageAsync(cancellationToken).ConfigureAwait(false);
            var args = new AzBlobUploadArgs
            {
                AccessTier = config.Value.AccessTier is { } tier ? (AccessTier?)new AccessTier(tier) : null,
                ContentType = item.MimeType,
                Metadata = new Dictionary<string, string> { [Sha256MetadataKey] = sha256 },
            };
            return await storage.UploadBlobIfNotExistsAsync(GetBlobName(item), content, args, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            throw new BackupDestinationException($"Azure Blob Storage rejected a backup upload with status {ex.Status}.", ex);
        }
    }

    /// <summary>
    /// Gets the blob name for an item: the photo or video prefix, then its relative path.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>The blob name.</returns>
    public string GetBlobName(MediaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var prefix = item.MimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? config.Value.VideoPrefix
            : config.Value.PhotoPrefix;
        prefix = prefix.Trim('/');
        return prefix.Length == 0 ? item.RelativePath : $"{prefix}/{item.RelativePath}";
    }

    private async Task<ContainerStorage> GetStorageAsync(CancellationToken cancellationToken)
    {
        if (_storage is { } ready)
            return ready;

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storage is null)
            {
                var storage = new ContainerStorage(new BlobContainerClient(config.Value.ConnectionString, config.Value.ContainerName));
                if (config.Value.ContainerCreationEnabled)
                    await storage.CreateContainerIfNotExists(cancellationToken).ConfigureAwait(false);
                _storage = storage;
            }

            return _storage;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private sealed class ContainerStorage(BlobContainerClient containerClient) : AzBlobStorageBase(containerClient);
}
