using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>
/// Configures the folder-backed media source used for development on Windows.
/// </summary>
public sealed record FileSystemMediaSourceConfig
{
    /// <summary>The configuration section bound to this type.</summary>
    public const string ConfigurationSectionName = "FileSystemMediaSource";

    /// <summary>Gets the folder that stands in for the device's DCIM collection.</summary>
    /// <remarks>Defaults to a <c>DcimCloudSync</c> folder inside the user's Pictures folder.</remarks>
    [Required, MinLength(1)]
    public string RootPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DcimCloudSync");
}
