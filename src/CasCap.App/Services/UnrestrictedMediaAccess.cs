namespace DcimCloudSync.Services;

/// <summary>
/// Reports full media access for sources without a permission model, such as a local folder.
/// </summary>
public sealed class UnrestrictedMediaAccess : IMediaAccess
{
    /// <inheritdoc/>
    public Task<MediaAccessStatus> RequestAccessAsync() => Task.FromResult(MediaAccessStatus.Unrestricted);

    /// <inheritdoc/>
    public MediaAccessStatus GetStatus() => MediaAccessStatus.Unrestricted;
}
