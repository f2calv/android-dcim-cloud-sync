namespace DcimCloudSync.Models;

/// <summary>
/// Describes one photo or video discovered by an <see cref="IMediaSource"/>.
/// </summary>
/// <remarks>
/// <see cref="RelativePath"/> and <see cref="DisplayName"/> can identify a person; never log or display them in
/// diagnostics.
/// </remarks>
public sealed record MediaItem
{
    /// <summary>Gets the identity that is stable for the item within its source.</summary>
    public required string SourceId { get; init; }

    /// <summary>Gets the item's path relative to the source root, using <c>/</c> separators.</summary>
    public required string RelativePath { get; init; }

    /// <summary>Gets the item's file name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the item's MIME type.</summary>
    public required string MimeType { get; init; }

    /// <summary>Gets the size in bytes reported by the source at discovery time.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Gets the last modification time reported by the source.</summary>
    public required DateTimeOffset ModifiedUtc { get; init; }
}
