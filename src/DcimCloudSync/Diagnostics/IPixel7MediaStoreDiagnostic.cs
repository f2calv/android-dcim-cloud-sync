namespace DcimCloudSync.Diagnostics;

/// <summary>
/// Defines the temporary Android-free Pixel 7 MediaStore diagnostic boundary.
/// </summary>
/// <remarks>
/// Operations are foreground-only, synthetic-only, and initiated explicitly by the visible diagnostic page.
/// This interface is evidence-gathering infrastructure and is not a production backup abstraction.
/// </remarks>
public interface IPixel7MediaStoreDiagnostic
{
    /// <summary>
    /// Refreshes the current visual-media access state without displaying a permission prompt.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the foreground refresh.</param>
    /// <returns>A task whose result is the current Android-free access snapshot.</returns>
    Task<MediaAccessDiagnosticSnapshot> RefreshAccessAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Requests or manages the specified visual-media access from an explicit foreground user action.
    /// </summary>
    /// <param name="operation">The neutral visual-media access operation to perform.</param>
    /// <param name="cancellationToken">The token that cancels the foreground request operation.</param>
    /// <returns>A task whose result is the refreshed Android-free access snapshot.</returns>
    /// <remarks>
    /// The operation is synthetic-diagnostic-only. Separate image and video operations intentionally make
    /// one-kind access states observable without device automation.
    /// </remarks>
    Task<MediaAccessDiagnosticSnapshot> RequestAccessAsync(
        VisualMediaAccessRequestOperation operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs one foreground diagnostic against the packaged synthetic fixture contract.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels discovery or active sequential reads.</param>
    /// <returns>A task whose result contains only sanitized synthetic observations.</returns>
    Task<Pixel7DiagnosticReport> RunDiagnosticAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Identifies a platform-neutral foreground visual-media access operation.
/// </summary>
/// <remarks>
/// These operations exist only for the temporary synthetic Pixel 7 diagnostic.
/// </remarks>
public enum VisualMediaAccessRequestOperation
{
    /// <summary>Requests broad image access without requesting broad video access.</summary>
    Images,

    /// <summary>Requests broad video access without requesting broad image access.</summary>
    Videos,

    /// <summary>Requests image, video, and supported selected-media access together.</summary>
    Combined,

    /// <summary>Opens the supported selected-media request or reselection operation.</summary>
    ManageSelection,
}
