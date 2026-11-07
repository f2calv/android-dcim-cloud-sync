namespace DcimCloudSync.Services;

/// <summary>
/// Stores one block blob per media item in Azure Blob Storage, named by media-type prefix and relative path.
/// </summary>
/// <remarks>
/// Uploads are create-only and record the content SHA-256, the source size and the source modification time as blob
/// metadata, so a quick check can recognize an unchanged item without reading it. Constructing the destination reads no
/// configuration and performs no network I/O; the client is created on first use, and a missing container is created
/// then only when configured. A failed creation is retried on the next operation. Settings are read on every operation, so a
/// changed endpoint or container takes effect without a restart. A blob service URL authenticates with the signed-in user's
/// token from <see cref="IAccessTokenProvider"/>.
/// </remarks>
/// <param name="config">The destination configuration.</param>
/// <param name="tokenProvider">The signed-in user's token provider, or <see langword="null"/> when only connection strings are used.</param>
public sealed class AzureBlobBackupDestination(
    IOptionsMonitor<AzureBlobBackupConfig> config,
    IAccessTokenProvider? tokenProvider = null)
    : IBackupDestination
{
    /// <summary>The blob metadata key that records the content SHA-256.</summary>
    public const string Sha256MetadataKey = "sha256";

    /// <summary>The blob metadata key that records the source item's size in bytes.</summary>
    public const string SourceSizeMetadataKey = "sourcesize";

    /// <summary>The blob metadata key that records the source item's modification time, in Unix seconds.</summary>
    public const string SourceModifiedMetadataKey = "sourcemodified";

    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private (string Key, ContainerStorage Storage)? _storage;

    /// <inheritdoc/>
    public bool IsConfigured
    {
        get
        {
            try
            {
                var current = config.CurrentValue;
                return !string.IsNullOrWhiteSpace(current.ConnectionString)
                    || (current.BlobServiceUri is not null && tokenProvider is { IsConfigured: true });
            }
            catch (OptionsValidationException)
            {
                return false;
            }
        }
    }

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

            string? Read(string key) => metadata.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)).Value;
            return new BackupRecord(
                Read(Sha256MetadataKey),
                long.TryParse(Read(SourceSizeMetadataKey), NumberStyles.None, CultureInfo.InvariantCulture, out var size) ? size : null,
                long.TryParse(Read(SourceModifiedMetadataKey), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var modified)
                    ? DateTimeOffset.FromUnixTimeSeconds(modified)
                    : null);
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
                AccessTier = config.CurrentValue.AccessTier is { } tier ? (AccessTier?)new AccessTier(tier) : null,
                ContentType = item.MimeType,
                Metadata = new Dictionary<string, string>
                {
                    [Sha256MetadataKey] = sha256,
                    [SourceSizeMetadataKey] = item.SizeBytes.ToString(CultureInfo.InvariantCulture),
                    [SourceModifiedMetadataKey] = item.ModifiedUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                },
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
        var current = config.CurrentValue;
        var prefix = item.MimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            ? current.VideoPrefix
            : current.PhotoPrefix;
        prefix = prefix.Trim('/');
        return prefix.Length == 0 ? item.RelativePath : $"{prefix}/{item.RelativePath}";
    }

    private async Task<ContainerStorage> GetStorageAsync(CancellationToken cancellationToken)
    {
        var current = config.CurrentValue;
        var key = $"{current.ConnectionString}|{current.BlobServiceUri}|{current.ContainerName}";
        if (_storage is { } cached && cached.Key == key)
            return cached.Storage;

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storage is { } raced && raced.Key == key)
                return raced.Storage;

            var storage = new ContainerStorage(CreateContainerClient(current));
            if (current.ContainerCreationEnabled)
                await storage.CreateContainerIfNotExists(cancellationToken).ConfigureAwait(false);
            _storage = (key, storage);
            return storage;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private BlobContainerClient CreateContainerClient(AzureBlobBackupConfig current)
    {
        if (!string.IsNullOrWhiteSpace(current.ConnectionString))
            return new BlobContainerClient(current.ConnectionString, current.ContainerName);

        if (current.BlobServiceUri is not { } serviceUri || tokenProvider is null)
            throw new InvalidOperationException("The backup destination has no connection string, or no blob service URL and sign-in.");

        return new BlobContainerClient(new Uri(serviceUri, current.ContainerName), new AccessTokenProviderCredential(tokenProvider));
    }
    private sealed class ContainerStorage(BlobContainerClient containerClient) : AzBlobStorageBase(containerClient);
}
