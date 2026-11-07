namespace DcimCloudSync.Models;

/// <summary>
/// Describes the media access the user granted.
/// </summary>
/// <param name="Level">Whether all, some or none of the media is visible.</param>
/// <param name="LocationIncluded">Whether photo location metadata is kept in the bytes that are read.</param>
public sealed record MediaAccessStatus(MediaAccessLevel Level, bool LocationIncluded)
{
    /// <summary>Gets the status of a source with no permission model, such as a local folder.</summary>
    public static MediaAccessStatus Unrestricted { get; } = new(MediaAccessLevel.Full, LocationIncluded: true);
}
