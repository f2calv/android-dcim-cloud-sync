namespace DcimCloudSync.Models;

/// <summary>
/// Configures which DCIM folders the Android MediaStore media source reads.
/// </summary>
public sealed record MediaStoreMediaSourceConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "MediaStoreMediaSource";

    /// <summary>
    /// Gets or sets a value indicating whether photos and videos in DCIM subfolders are read as well as those directly in DCIM.
    /// </summary>
    /// <remarks>
    /// Off by default: subfolders such as <c>DCIM/PhotoScan</c> often belong to other apps. Some camera apps, including the
    /// Pixel camera on many devices, save to <c>DCIM/Camera</c>, which also needs this setting.
    /// </remarks>
    public bool SubfoldersIncluded { get; set; }
}
