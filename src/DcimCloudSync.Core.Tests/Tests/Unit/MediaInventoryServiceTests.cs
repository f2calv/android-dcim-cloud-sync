namespace DcimCloudSync.Tests.Unit;

/// <summary>
/// Tests scan summaries produced by <see cref="MediaInventoryService"/>.
/// </summary>
[Trait("Category", "Hashing")]
public sealed class MediaInventoryServiceTests
{
    [Fact]
    public async Task ScanAsync()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("Camera/IMG_0001.jpg", "same");
        folder.AddFile("Camera/Copy/IMG_0001.jpg", "same");
        folder.AddFile("Camera/VID_0001.mp4", "different");
        var reports = new List<MediaInventoryProgress>();

        var summary = await CreateService(folder).ScanAsync(
            new SynchronousProgress(reports.Add),
            TestContext.Current.CancellationToken);

        Assert.Equal(3, summary.ItemCount);
        Assert.Equal(17, summary.TotalBytes);
        Assert.Equal(1, summary.DuplicateCount);
        Assert.Equal(0, summary.UnreadableCount);
        Assert.Equal(3, reports.Count);
        Assert.Equal(new MediaInventoryProgress(3, 17), reports[^1]);
    }

    [Fact]
    public async Task ScanAsync_UnreadableItem()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "readable");
        var lockedPath = folder.AddFile("IMG_0002.jpg", "locked");

        MediaInventorySummary summary;
        using (new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            summary = await CreateService(folder).ScanAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(2, summary.ItemCount);
        Assert.Equal(8, summary.TotalBytes);
        Assert.Equal(1, summary.UnreadableCount);
    }

    [Fact]
    public async Task ScanAsync_Canceled()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "photo");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateService(folder).ScanAsync(null, new CancellationToken(canceled: true)));
    }

    private static MediaInventoryService CreateService(TemporaryMediaFolder folder) =>
        new(NullLogger<MediaInventoryService>.Instance, TimeProvider.System, folder.Source);

    private sealed class SynchronousProgress(Action<MediaInventoryProgress> handler) : IProgress<MediaInventoryProgress>
    {
        public void Report(MediaInventoryProgress value) => handler(value);
    }
}
