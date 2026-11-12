namespace CasCap.Tests.Unit;

/// <summary>
/// Tests discovery and opening in <see cref="FileSystemMediaSource"/>.
/// </summary>
[Trait("Category", "Discovery")]
public sealed class FileSystemMediaSourceTests
{
    [Fact]
    public async Task DiscoverAsync()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("Camera/IMG_0001.JPG", "photo");
        folder.AddFile("Camera/Nested/VID_0001.mp4", "video");
        folder.AddFile("Camera/notes.txt", "not media");

        var items = await folder.Source.DiscoverAsync(TestContext.Current.CancellationToken)
            .OrderBy(item => item.RelativePath, StringComparer.Ordinal)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            items,
            photo =>
            {
                Assert.Equal("Camera/IMG_0001.JPG", photo.RelativePath);
                Assert.Equal(photo.RelativePath, photo.SourceId);
                Assert.Equal("IMG_0001.JPG", photo.DisplayName);
                Assert.Equal("image/jpeg", photo.MimeType);
                Assert.Equal(5, photo.SizeBytes);
            },
            video =>
            {
                Assert.Equal("Camera/Nested/VID_0001.mp4", video.RelativePath);
                Assert.Equal("video/mp4", video.MimeType);
            });
    }

    [Fact]
    public async Task DiscoverAsync_MissingRoot()
    {
        var source = TemporaryMediaFolder.CreateSource(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        var ex = await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => source.DiscoverAsync(TestContext.Current.CancellationToken)
                .ToListAsync(TestContext.Current.CancellationToken)
                .AsTask());

        Assert.DoesNotContain(Path.GetTempPath(), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenReadAsync()
    {
        using var folder = new TemporaryMediaFolder();
        folder.AddFile("IMG_0001.jpg", "photo");
        var item = await folder.Source.DiscoverAsync(TestContext.Current.CancellationToken)
            .SingleAsync(TestContext.Current.CancellationToken);

        await using var stream = await folder.Source.OpenReadAsync(item, TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);

        Assert.Equal("photo", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("Camera/../../outside.jpg")]
    public async Task OpenReadAsync_OutsideRoot(string relativePath)
    {
        using var folder = new TemporaryMediaFolder();
        var item = new MediaItem
        {
            SourceId = relativePath,
            RelativePath = relativePath,
            DisplayName = "outside.jpg",
            MimeType = "image/jpeg",
            SizeBytes = 0,
            ModifiedUtc = DateTimeOffset.UnixEpoch,
        };

        await Assert.ThrowsAsync<ArgumentException>(
            "item",
            () => folder.Source.OpenReadAsync(item, TestContext.Current.CancellationToken));
    }
}
