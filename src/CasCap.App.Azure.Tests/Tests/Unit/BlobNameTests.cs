namespace CasCap.Tests.Unit;

/// <summary>
/// Tests blob naming in <see cref="AzureBlobBackupDestination"/>; no storage is contacted.
/// </summary>
[Trait("Category", "Naming")]
public sealed class BlobNameTests
{
    [Theory]
    [InlineData("image/jpeg", "photos", "videos", "photos/Camera/a.jpg")]
    [InlineData("video/mp4", "photos", "videos", "videos/Camera/a.jpg")]
    [InlineData("image/jpeg", "/archive/jpg/", "videos", "archive/jpg/Camera/a.jpg")]
    [InlineData("image/jpeg", "", "videos", "Camera/a.jpg")]
    public void GetBlobName(string mimeType, string photoPrefix, string videoPrefix, string expected)
    {
        var destination = new AzureBlobBackupDestination(new StaticOptionsMonitor<AzureBlobBackupConfig>(new AzureBlobBackupConfig
        {
            ConnectionString = "UseDevelopmentStorage=true",
            PhotoPrefix = photoPrefix,
            VideoPrefix = videoPrefix,
        }));

        Assert.Equal(expected, destination.GetBlobName(TestData.Item("Camera/a.jpg", mimeType)));
    }
}
