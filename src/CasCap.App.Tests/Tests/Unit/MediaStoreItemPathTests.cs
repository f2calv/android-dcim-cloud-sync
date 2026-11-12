namespace CasCap.Tests.Unit;

/// <summary>
/// Tests the MediaStore row mapping in <see cref="MediaStoreItemPath"/>.
/// </summary>
[Trait("Category", "Discovery")]
public sealed class MediaStoreItemPathTests
{
    [Theory]
    [InlineData("external_primary", "DCIM/Camera/", "PXL_0001.jpg", "Camera/PXL_0001.jpg")]
    [InlineData("external_primary", "DCIM/", "IMG_0001.jpg", "IMG_0001.jpg")]
    [InlineData("external_primary", "dcim/Camera/Nested/", "a.mp4", "Camera/Nested/a.mp4")]
    [InlineData("1234-abcd", "DCIM/Camera/", "a.jpg", "1234-abcd/Camera/a.jpg")]
    [InlineData("external_primary", "Pictures/Screenshots/", "a.png", null)]
    [InlineData("external_primary", "DCIMX/", "a.jpg", null)]
    [InlineData("external_primary", null, "a.jpg", null)]
    [InlineData("external_primary", "DCIM/Camera/", "", null)]
    [InlineData("external_primary", "DCIM/Camera/", "..", null)]
    [InlineData("external_primary", "DCIM/../Camera/", "a.jpg", null)]
    [InlineData(null, "DCIM/Camera/", "a.jpg", null)]
    public void GetRelativePath(string? volumeName, string? relativeDirectory, string? displayName, string? expected) =>
        Assert.Equal(expected, MediaStoreItemPath.GetRelativePath(volumeName, relativeDirectory, displayName));

    [Fact]
    public void SourceId_RoundTrip()
    {
        var sourceId = MediaStoreItemPath.CreateSourceId("external_primary", MediaStoreItemPath.VideoCollection, 42);

        Assert.True(MediaStoreItemPath.TryParseSourceId(sourceId, out var volumeName, out var collection, out var rowId));
        Assert.Equal(("external_primary", "video", 42L), (volumeName, collection, rowId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("external_primary:images")]
    [InlineData("external_primary:audio:1")]
    [InlineData("external_primary:images:-1")]
    [InlineData("external_primary:images:1:2")]
    [InlineData(":images:1")]
    [InlineData("Camera/IMG_0001.jpg")]
    public void TryParseSourceId_Invalid(string? sourceId) =>
        Assert.False(MediaStoreItemPath.TryParseSourceId(sourceId, out _, out _, out _));
}
