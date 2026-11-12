namespace CasCap.Abstractions;

/// <summary>
/// Checks and requests permission to read the device's photos and videos.
/// </summary>
/// <remarks>
/// Request access from the UI thread, before calling <see cref="IMediaSource.DiscoverAsync(CancellationToken)"/>; platforms
/// with no permission model report full access.
/// </remarks>
public interface IMediaAccess
{
    /// <summary>
    /// Requests media access, prompting the user only for permissions not yet decided.
    /// </summary>
    /// <returns>The access the user granted.</returns>
    public Task<MediaAccessStatus> RequestAccessAsync();

    /// <summary>
    /// Gets the access already granted, without prompting, for runs that have no screen to prompt on.
    /// </summary>
    /// <returns>The current access.</returns>
    public MediaAccessStatus GetStatus();
}
