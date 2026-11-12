namespace DcimCloudSync.Models;

/// <summary>
/// Configures which DCIM folders the Android MediaStore media source reads.
/// </summary>
public sealed record MediaStoreMediaSourceConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "MediaStoreMediaSource";

    /// <summary>
    /// Gets or sets a value indicating whether every folder under DCIM is read, rather than only the camera folder
    /// <c>DCIM/Camera</c> and its subfolders.
    /// </summary>
    /// <remarks>Off by default: other DCIM folders, such as <c>DCIM/PhotoScan</c>, usually belong to other apps.</remarks>
    public bool AllFoldersIncluded { get; set; }
}
