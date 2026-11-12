using System.Net.Sockets;

namespace DcimCloudSync.Tests.Integration;

/// <summary>
/// Provides a disposable Azurite container, or skips the test when storage is not reachable.
/// </summary>
/// <remarks>
/// The connection string comes from <c>DcimCloudSync__AzureBlobBackup__ConnectionString</c>, defaulting to Azurite. Each test
/// uses a new container, deleted afterwards, so runs never touch existing data.
/// </remarks>
public abstract class TestBase : IAsyncLifetime
{
    private const string ConnectionStringVariable = "DcimCloudSync__AzureBlobBackup__ConnectionString";
    private const string AzuriteConnectionString = "UseDevelopmentStorage=true";
    private const int AzuriteBlobPort = 10000;

    private bool _storageReachable;

    /// <summary>Initializes a new instance of the <see cref="TestBase"/> class.</summary>
    protected TestBase()
    {
        ConnectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable) ?? AzuriteConnectionString;
        ContainerName = $"test-{Guid.NewGuid():N}";
        MediaFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(MediaFolder);
    }

    /// <summary>Gets the storage connection string under test.</summary>
    protected string ConnectionString { get; }

    /// <summary>Gets the container created for this test.</summary>
    protected string ContainerName { get; }

    /// <summary>Gets a temporary folder for synthetic media files.</summary>
    protected string MediaFolder { get; }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        if (ConnectionString != AzuriteConnectionString)
        {
            _storageReachable = true;
            return;
        }

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync("127.0.0.1", AzuriteBlobPort, TestContext.Current.CancellationToken);
            _storageReachable = true;
        }
        catch (SocketException)
        {
            Assert.Skip("Azurite is not running; start it with: docker compose up -d --wait azurite");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Directory.Delete(MediaFolder, recursive: true);
        if (_storageReachable)
            await new BlobContainerClient(ConnectionString, ContainerName).DeleteIfExistsAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>Creates a destination for this test's container.</summary>
    /// <param name="containerCreationEnabled">Whether the destination may create the container.</param>
    /// <param name="accessTier">The access tier for new backups, or <see langword="null"/> for the account default.</param>
    /// <returns>The destination.</returns>
    protected AzureBlobBackupDestination CreateDestination(bool containerCreationEnabled = true, string? accessTier = null) =>
        new(new StaticOptionsMonitor<AzureBlobBackupConfig>(new AzureBlobBackupConfig
        {
            AccessTier = accessTier,
            ConnectionString = ConnectionString,
            ContainerName = ContainerName,
            ContainerCreationEnabled = containerCreationEnabled,
        }));

    /// <summary>Writes a synthetic file below <see cref="MediaFolder"/>.</summary>
    /// <param name="relativePath">The path relative to the media folder.</param>
    /// <param name="content">The file content.</param>
    protected void AddFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(MediaFolder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    /// <summary>Creates a backup service over <see cref="MediaFolder"/> and the given destination.</summary>
    /// <param name="destination">The backup destination.</param>
    /// <returns>The service.</returns>
    protected BackupService CreateBackupService(AzureBlobBackupDestination destination) =>
        new(NullLogger<BackupService>.Instance, TimeProvider.System,
            new FileSystemMediaSource(Options.Create(new FileSystemMediaSourceConfig { RootPath = MediaFolder })),
            destination);
}
