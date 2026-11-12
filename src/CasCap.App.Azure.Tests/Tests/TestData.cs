namespace DcimCloudSync.Tests;

/// <summary>
/// Builds synthetic media items for tests.
/// </summary>
internal static class TestData
{
    /// <summary>
    /// Creates a media item with the given path and MIME type.
    /// </summary>
    /// <param name="relativePath">The item's relative path with <c>/</c> separators.</param>
    /// <param name="mimeType">The item's MIME type.</param>
    /// <returns>The item.</returns>
    public static MediaItem Item(string relativePath, string mimeType) => new()
    {
        SourceId = relativePath,
        RelativePath = relativePath,
        DisplayName = Path.GetFileName(relativePath),
        MimeType = mimeType,
        SizeBytes = 0,
        ModifiedUtc = DateTimeOffset.UnixEpoch,
    };
}
