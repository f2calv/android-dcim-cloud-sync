namespace DcimCloudSync.Diagnostics;

/// <summary>
/// Represents an immutable, Android-free snapshot of visual-media access for the temporary diagnostic.
/// </summary>
/// <remarks>
/// The snapshot is refreshed only during explicit foreground, synthetic-only diagnostic activity.
/// Broad image and video grants remain independent, and selected access never implies complete access.
/// Protected media-location access is intentionally fixed to <see cref="MediaLocationAccessStatus.NotRequested"/>.
/// </remarks>
public sealed record MediaAccessDiagnosticSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaAccessDiagnosticSnapshot"/> record.
    /// </summary>
    /// <param name="runtimeApiLevel">The active Android API level represented as a platform-neutral integer.</param>
    /// <param name="imageAccess">The independently inspected broad image grant.</param>
    /// <param name="videoAccess">The independently inspected broad video grant.</param>
    /// <param name="selectedAccess">The independently inspected selected-visual-media grant.</param>
    /// <param name="shouldShowImageRationale">Whether an image-access rationale is currently appropriate.</param>
    /// <param name="shouldShowVideoRationale">Whether a video-access rationale is currently appropriate.</param>
    /// <param name="shouldShowSelectedAccessRationale">Whether a selected-access rationale is currently appropriate.</param>
    /// <param name="isReselectionAvailable">Whether the current API supports selected-media reselection.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="runtimeApiLevel"/> is below the application's API 33 floor.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when selected-media or reselection state conflicts with the runtime API level.
    /// </exception>
    public MediaAccessDiagnosticSnapshot(
        int runtimeApiLevel,
        VisualMediaGrantStatus imageAccess,
        VisualMediaGrantStatus videoAccess,
        SelectedVisualMediaAccessStatus selectedAccess,
        bool shouldShowImageRationale,
        bool shouldShowVideoRationale,
        bool shouldShowSelectedAccessRationale,
        bool isReselectionAvailable)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(runtimeApiLevel, 33);

        if (runtimeApiLevel < 34
            && (selectedAccess != SelectedVisualMediaAccessStatus.NotSupported || isReselectionAvailable))
        {
            throw new ArgumentException("Selected-media access and reselection are unavailable below API 34.");
        }

        if (runtimeApiLevel >= 34 && selectedAccess == SelectedVisualMediaAccessStatus.NotSupported)
        {
            throw new ArgumentException("Selected-media access must be inspected on API 34 and later.");
        }

        RuntimeApiLevel = runtimeApiLevel;
        ImageAccess = imageAccess;
        VideoAccess = videoAccess;
        SelectedAccess = selectedAccess;
        ShouldShowImageRationale = shouldShowImageRationale;
        ShouldShowVideoRationale = shouldShowVideoRationale;
        ShouldShowSelectedAccessRationale = shouldShowSelectedAccessRationale;
        IsReselectionAvailable = isReselectionAvailable;
    }

    /// <summary>Gets the active Android API level as a platform-neutral integer.</summary>
    public int RuntimeApiLevel { get; }

    /// <summary>Gets the independently inspected broad image grant.</summary>
    public VisualMediaGrantStatus ImageAccess { get; }

    /// <summary>Gets the independently inspected broad video grant.</summary>
    public VisualMediaGrantStatus VideoAccess { get; }

    /// <summary>Gets the independently inspected selected-visual-media grant.</summary>
    public SelectedVisualMediaAccessStatus SelectedAccess { get; }

    /// <summary>Gets whether an image-access rationale is currently appropriate.</summary>
    public bool ShouldShowImageRationale { get; }

    /// <summary>Gets whether a video-access rationale is currently appropriate.</summary>
    public bool ShouldShowVideoRationale { get; }

    /// <summary>Gets whether a selected-visual-media rationale is currently appropriate.</summary>
    public bool ShouldShowSelectedAccessRationale { get; }

    /// <summary>Gets whether any visual-media permission rationale is currently appropriate.</summary>
    public bool ShouldShowAnyRationale => ShouldShowImageRationale
        || ShouldShowVideoRationale
        || ShouldShowSelectedAccessRationale;

    /// <summary>Gets whether selected-media reselection is available on the active API.</summary>
    public bool IsReselectionAvailable { get; }

    /// <summary>Gets the protected media-location status, which is not requested by this diagnostic.</summary>
    public MediaLocationAccessStatus MediaLocationAccess => MediaLocationAccessStatus.NotRequested;

    /// <summary>
    /// Gets the derived complete, partial, or denied visual-media access level.
    /// </summary>
    /// <remarks>
    /// Complete access requires both broad grants. One-kind broad access, selected-only access, and mixed
    /// broad-selected access are always partial.
    /// </remarks>
    public VisualMediaAccessCompleteness Completeness
    {
        get
        {
            if (ImageAccess == VisualMediaGrantStatus.Granted
                && VideoAccess == VisualMediaGrantStatus.Granted)
            {
                return VisualMediaAccessCompleteness.Complete;
            }

            if (ImageAccess == VisualMediaGrantStatus.Granted
                || VideoAccess == VisualMediaGrantStatus.Granted
                || SelectedAccess == SelectedVisualMediaAccessStatus.Granted)
            {
                return VisualMediaAccessCompleteness.Partial;
            }

            return VisualMediaAccessCompleteness.Denied;
        }
    }
}

/// <summary>Identifies the independently inspected state of a broad visual-media grant.</summary>
public enum VisualMediaGrantStatus
{
    /// <summary>The broad grant is not held.</summary>
    Denied,

    /// <summary>The broad grant is held.</summary>
    Granted,
}

/// <summary>Identifies selected-visual-media access without implying broad access.</summary>
public enum SelectedVisualMediaAccessStatus
{
    /// <summary>The runtime API does not support selected-visual-media access.</summary>
    NotSupported,

    /// <summary>The selected-visual-media grant is not held.</summary>
    Denied,

    /// <summary>The selected-visual-media grant is held.</summary>
    Granted,
}

/// <summary>Identifies protected media-location access for sanitized diagnostic reporting.</summary>
public enum MediaLocationAccessStatus
{
    /// <summary>The diagnostic did not request or inspect protected media-location access.</summary>
    NotRequested,
}

/// <summary>Identifies the derived overall visual-media access level.</summary>
public enum VisualMediaAccessCompleteness
{
    /// <summary>No broad or selected visual-media access is held.</summary>
    Denied,

    /// <summary>Only one broad kind, selected media, or a mixed subset is accessible.</summary>
    Partial,

    /// <summary>Both broad image and broad video grants are held.</summary>
    Complete,
}
