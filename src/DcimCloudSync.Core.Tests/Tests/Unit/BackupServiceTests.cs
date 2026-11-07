using DcimCloudSync.Exceptions;

namespace DcimCloudSync.Tests.Unit;

/// <summary>
/// Tests backup outcomes produced by <see cref="BackupService"/> against an in-memory destination.
/// </summary>
[Trait("Category", "Backup")]
public sealed class BackupServiceTests
{
    private const string PhotoSha256 = "55c64d0fcd6f9d5f7c828093857e3fdfda68478bb4e9bd24d481ef391c7804e8";

    [Fact]
    public async Task BackUpAsync()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("Camera/IMG_0001.jpg", "photo");
        folder.AddFile("Camera/VID_0001.mp4", "video");
        var destination = new InMemoryBackupDestination();
        var reports = new List<BackupProgress>();

        using var service = CreateService(folder, destination);
        var summary = await service.BackUpAsync(new SynchronousProgress<BackupProgress>(reports.Add), TestContext.Current.CancellationToken);

        Assert.Equal((2, 2, 10L), (summary.ItemCount, summary.UploadedCount, summary.UploadedBytes));
        Assert.Equal("photo"u8.ToArray(), destination.Backups["Camera/IMG_0001.jpg"].Content);
        Assert.Equal(new BackupProgress(2, 2, 10), reports[^1]);
    }

    [Fact]
    public async Task BackUpAsync_ExistingBackups()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("same.jpg", "photo");
        folder.AddFile("changed.jpg", "photo");
        folder.AddFile("unverified.jpg", "photo");
        var destination = new InMemoryBackupDestination();
        using var service = CreateService(folder, destination);
        _ = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        destination.Backups["changed.jpg"] = ("0000", []);
        destination.Backups["unverified.jpg"] = (null, []);

        var summary = await service.BackUpAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(
            (0, 1, 1, 1),
            (summary.UploadedCount, summary.AlreadyBackedUpCount, summary.ConflictCount, summary.UnverifiedCount));
        Assert.Equal(("0000", 0), (destination.Backups["changed.jpg"].Sha256, destination.Backups["changed.jpg"].Content.Length));
    }

    [Theory]
    [InlineData(true, 1, 0)]
    [InlineData(false, 0, 1)]
    public async Task BackUpAsync_ConcurrentWriter(bool sameContent, int alreadyBackedUp, int conflicts)
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "photo");
        var destination = new InMemoryBackupDestination
        {
            ConcurrentWrite = ("IMG_0001.jpg", sameContent ? PhotoSha256 : "0000"),
        };

        using var service = CreateService(folder, destination);
        var summary = await service.BackUpAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((0, alreadyBackedUp, conflicts), (summary.UploadedCount, summary.AlreadyBackedUpCount, summary.ConflictCount));
    }

    [Fact]
    public async Task BackUpAsync_UnreadableItem()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "photo");
        var lockedPath = folder.AddFile("IMG_0002.jpg", "locked");

        using var service = CreateService(folder, new InMemoryBackupDestination());
        BackupSummary summary;
        using (new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            summary = await service.BackUpAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((2, 1, 1), (summary.ItemCount, summary.UploadedCount, summary.UnreadableCount));
    }

    [Fact]
    public async Task BackUpAsync_ItemChangedAfterHashing()
    {
        var source = new ChangingMediaSource("IMG_0001.jpg", "photo"u8.ToArray(), "edited photo"u8.ToArray());
        var destination = new InMemoryBackupDestination();
        using var service = new BackupService(NullLogger<BackupService>.Instance, TimeProvider.System, source, destination);

        var summary = await service.BackUpAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((1, 0, 1, 0L), (summary.ItemCount, summary.UploadedCount, summary.ChangedCount, summary.UploadedBytes));
        Assert.Empty(destination.Backups);
    }

    [Fact]
    public async Task BackUpAsync_DestinationFailure()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "photo");
        var failure = new BackupDestinationException("Destination unavailable.", new InvalidOperationException());
        var destination = new InMemoryBackupDestination { Failure = failure };

        using var service = CreateService(folder, destination);
        var exception = await Assert.ThrowsAsync<BackupDestinationException>(
            () => service.BackUpAsync(null, TestContext.Current.CancellationToken));

        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task BackUpAsync_SerializesRuns()
    {
        var source = new GatedMediaSource();
        using var service = new BackupService(
            NullLogger<BackupService>.Instance, TimeProvider.System, source, new InMemoryBackupDestination());

        var first = service.BackUpAsync(null, TestContext.Current.CancellationToken);
        var second = service.BackUpAsync(null, TestContext.Current.CancellationToken);
        var startedWhileFirstRuns = source.DiscoverCount;
        source.Open();
        _ = await Task.WhenAll(first, second);

        Assert.Equal((1, 2), (startedWhileFirstRuns, source.DiscoverCount));
    }

    [Fact]
    public async Task BackUpChangesAsync_WithoutCheckpoint()
    {
        var source = new ChangeTrackingMediaSource();
        source.Add("IMG_0001.jpg", "photo");
        source.Add("IMG_0002.jpg", "second photo");
        var checkpoint = new InMemoryCheckpointStore();
        using var service = CreateService(source, checkpoint);

        var summary = await service.BackUpChangesAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((2, 2, "2"), (summary.ItemCount, summary.UploadedCount, checkpoint.ChangeToken));
    }

    [Fact]
    public async Task BackUpChangesAsync_FromCheckpoint()
    {
        var source = new ChangeTrackingMediaSource();
        source.Add("IMG_0001.jpg", "photo");
        var checkpoint = new InMemoryCheckpointStore();
        var destination = new InMemoryBackupDestination();
        using var service = CreateService(source, checkpoint, destination);
        _ = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        source.Add("IMG_0002.jpg", "second photo");

        var summary = await service.BackUpChangesAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((1, 1, "2"), (summary.ItemCount, summary.UploadedCount, checkpoint.ChangeToken));
        Assert.Equal(2, destination.Backups.Count);
    }

    [Fact]
    public async Task BackUpChangesAsync_UnreadableItemKeepsCheckpoint()
    {
        var source = new ChangeTrackingMediaSource();
        source.Add("IMG_0001.jpg", "photo");
        var checkpoint = new InMemoryCheckpointStore();
        using var service = CreateService(source, checkpoint);
        _ = await service.BackUpAsync(null, TestContext.Current.CancellationToken);
        source.Add("IMG_0002.jpg", "locked", unreadable: true);

        var summary = await service.BackUpChangesAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal((1, 1, "1"), (summary.ItemCount, summary.UnreadableCount, checkpoint.ChangeToken));
    }

    private static BackupService CreateService(
        ChangeTrackingMediaSource source,
        InMemoryCheckpointStore checkpoint,
        InMemoryBackupDestination? destination = null) =>
        new(NullLogger<BackupService>.Instance, TimeProvider.System, source, destination ?? new InMemoryBackupDestination(), checkpoint);

    private static BackupService CreateService(TemporaryMediaFolder folder, InMemoryBackupDestination destination) =>
        new(NullLogger<BackupService>.Instance, TimeProvider.System, folder.Source, destination);
}
