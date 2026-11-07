namespace DcimCloudSync.Services;

/// <summary>
/// Maps Android MediaStore rows to the source identities and DCIM-relative paths used by <see cref="MediaItem"/>.
/// </summary>
/// <remarks>
/// Kept free of Android types so the mapping is unit-testable. Relative paths are relative to the DCIM folder, so a photo
/// at <c>DCIM/Camera/IMG_0001.jpg</c> maps to <c>Camera/IMG_0001.jpg</c>, the same path a folder source rooted at a copy
/// of DCIM produces. Items on a removable volume are prefixed with the volume name to keep paths unique.
/// </remarks>
public static class MediaStoreItemPath
{
    /// <summary>The MediaStore name of the primary shared storage volume.</summary>
    public const string PrimaryVolume = "external_primary";

    /// <summary>The source collection for images.</summary>
    public const string ImagesCollection = "images";

    /// <summary>The source collection for videos.</summary>
    public const string VideoCollection = "video";

    /// <summary>The MediaStore relative-path prefix of the DCIM folder.</summary>
    public const string DcimPrefix = "DCIM/";

    private const char SourceIdSeparator = ':';

    /// <summary>
    /// Gets an item's path relative to DCIM.
    /// </summary>
    /// <param name="volumeName">The MediaStore volume name.</param>
    /// <param name="relativeDirectory">The MediaStore <c>RELATIVE_PATH</c>, such as <c>DCIM/Camera/</c>.</param>
    /// <param name="displayName">The MediaStore <c>DISPLAY_NAME</c>.</param>
    /// <returns>The DCIM-relative path with <c>/</c> separators, or <see langword="null"/> when the item is outside DCIM or
    /// its name is unusable.</returns>
    public static string? GetRelativePath(string? volumeName, string? relativeDirectory, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(volumeName) || string.IsNullOrWhiteSpace(displayName)
            || displayName.Contains('/') || displayName is "." or ".."
            || relativeDirectory is null || !relativeDirectory.StartsWith(DcimPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var directory = relativeDirectory[DcimPrefix.Length..].Trim('/');
        if (directory.Split('/').Any(segment => segment is "." or ".."))
            return null;

        var path = directory.Length == 0 ? displayName : $"{directory}/{displayName}";
        return volumeName == PrimaryVolume ? path : $"{volumeName}/{path}";
    }

    /// <summary>
    /// Creates the source identity of a MediaStore row.
    /// </summary>
    /// <param name="volumeName">The MediaStore volume name.</param>
    /// <param name="collection"><see cref="ImagesCollection"/> or <see cref="VideoCollection"/>.</param>
    /// <param name="rowId">The MediaStore row identifier.</param>
    /// <returns>The identity, such as <c>external_primary:images:42</c>.</returns>
    public static string CreateSourceId(string volumeName, string collection, long rowId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(volumeName);
        ArgumentOutOfRangeException.ThrowIfNegative(rowId);
        if (collection is not (ImagesCollection or VideoCollection))
            throw new ArgumentOutOfRangeException(nameof(collection), "The collection must be images or video.");
        if (volumeName.Contains(SourceIdSeparator))
            throw new ArgumentException("The volume name cannot contain a colon.", nameof(volumeName));

        return $"{volumeName}{SourceIdSeparator}{collection}{SourceIdSeparator}{rowId}";
    }

    /// <summary>
    /// Parses a source identity created by <see cref="CreateSourceId"/>.
    /// </summary>
    /// <param name="sourceId">The identity to parse.</param>
    /// <param name="volumeName">The MediaStore volume name.</param>
    /// <param name="collection"><see cref="ImagesCollection"/> or <see cref="VideoCollection"/>.</param>
    /// <param name="rowId">The MediaStore row identifier.</param>
    /// <returns><see langword="true"/> when the identity is well formed.</returns>
    public static bool TryParseSourceId(string? sourceId, out string volumeName, out string collection, out long rowId)
    {
        volumeName = collection = string.Empty;
        rowId = 0;
        var parts = sourceId?.Split(SourceIdSeparator);
        if (parts is not { Length: 3 } || parts[0].Length == 0 || parts[1] is not (ImagesCollection or VideoCollection)
            || !long.TryParse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out rowId))
            return false;

        volumeName = parts[0];
        collection = parts[1];
        return true;
    }
}
