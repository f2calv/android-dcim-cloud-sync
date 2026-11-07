namespace DcimCloudSync.Tests.Integration;

/// <summary>
/// Tests <see cref="AzureBlobBackupDestination"/> and <see cref="BackupService"/> against Azurite or a configured account.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AzureBlobBackupDestinationTests : TestBase
{
    [Fact]
    public async Task TryCreateBackupAsync()
    {
        var destination = CreateDestination();
        var photo = TestData.Item("Camera/IMG_0001.jpg", "image/jpeg");

        Assert.Null(await destination.FindBackupAsync(photo, TestContext.Current.CancellationToken));
        using (var first = new MemoryStream([1, 2, 3]))
            Assert.True(await destination.TryCreateBackupAsync(photo, first, "aaa", TestContext.Current.CancellationToken));
        using (var second = new MemoryStream([4, 5, 6]))
            Assert.False(await destination.TryCreateBackupAsync(photo, second, "bbb", TestContext.Current.CancellationToken));

        var record = await destination.FindBackupAsync(photo, TestContext.Current.CancellationToken);
        Assert.Equal(new BackupRecord("aaa", photo.SizeBytes, photo.ModifiedUtc), record);
        var blob = new BlobContainerClient(ConnectionString, ContainerName).GetBlobClient("photos/Camera/IMG_0001.jpg");
        var properties = await blob.GetPropertiesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("image/jpeg", properties.Value.ContentType);
        Assert.Equal(3, properties.Value.ContentLength);
    }

    [Fact]
    public async Task TryCreateBackupAsync_AccessTier()
    {
        var destination = CreateDestination(accessTier: "Cool");
        using var content = new MemoryStream([1]);

        Assert.True(await destination.TryCreateBackupAsync(
            TestData.Item("a.jpg", "image/jpeg"), content, "aaa", TestContext.Current.CancellationToken));

        var blob = new BlobContainerClient(ConnectionString, ContainerName).GetBlobClient("photos/a.jpg");
        var properties = await blob.GetPropertiesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Cool", properties.Value.AccessTier);
    }

    [Fact]
    public async Task TryCreateBackupAsync_MissingContainer()
    {
        var destination = CreateDestination(containerCreationEnabled: false);
        using var content = new MemoryStream([1]);

        var exception = await Assert.ThrowsAsync<BackupDestinationException>(() => destination.TryCreateBackupAsync(
            TestData.Item("a.jpg", "image/jpeg"), content, "aaa", TestContext.Current.CancellationToken));
        Assert.DoesNotContain(ContainerName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackUpAsync_ItemChangedAfterHashing()
    {
        var source = new ChangingMediaSource("Camera/IMG_0001.jpg", "photo"u8.ToArray(), "edited photo"u8.ToArray());
        var service = new BackupService(NullLogger<BackupService>.Instance, TimeProvider.System, source, CreateDestination());

        var summary = await service.BackUpAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((0, 1), (summary.UploadedCount, summary.ChangedCount));
        var blob = new BlobContainerClient(ConnectionString, ContainerName).GetBlobClient("photos/Camera/IMG_0001.jpg");
        Assert.False(await blob.ExistsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task BackUpAsync_Lifecycle()
    {
        AddFile("Camera/IMG_0001.jpg", "photo");
        AddFile("Camera/VID_0001.mp4", "video");
        var service = CreateBackupService(CreateDestination());

        var first = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        Assert.Equal((2, 2, 10L), (first.ItemCount, first.UploadedCount, first.UploadedBytes));

        var second = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        Assert.Equal((0, 2), (second.UploadedCount, second.AlreadyBackedUpCount));

        AddFile("Camera/IMG_0001.jpg", "edited photo");
        var third = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        Assert.Equal((0, 1, 1), (third.UploadedCount, third.AlreadyBackedUpCount, third.ConflictCount));
    }
}
