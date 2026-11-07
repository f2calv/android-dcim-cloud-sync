using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;

namespace DcimCloudSync.Services;

/// <summary>
/// Requests Android visual-media and media-location permissions and classifies what the user granted.
/// </summary>
/// <remarks>
/// On Android 14 and later the user may grant only selected photos and videos, reported as
/// <see cref="MediaAccessLevel.Partial"/>. <c>ACCESS_MEDIA_LOCATION</c> shows no dialog of its own; without it, MediaStore
/// returns photos with location removed.
/// </remarks>
public sealed class AndroidMediaAccess : IMediaAccess
{
    /// <inheritdoc/>
    public async Task<MediaAccessStatus> RequestAccessAsync()
    {
        _ = await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<MediaPermissions>);

        var context = Platform.AppContext;
        bool IsGranted(string permission) => context.CheckSelfPermission(permission) == Permission.Granted;

        var level = IsGranted(global::Android.Manifest.Permission.ReadMediaImages)
                && IsGranted(global::Android.Manifest.Permission.ReadMediaVideo)
            ? MediaAccessLevel.Full
            : OperatingSystem.IsAndroidVersionAtLeast(34) && IsGranted(global::Android.Manifest.Permission.ReadMediaVisualUserSelected)
                ? MediaAccessLevel.Partial
                : MediaAccessLevel.Denied;
        var locationIncluded = level != MediaAccessLevel.Denied && IsGranted(global::Android.Manifest.Permission.AccessMediaLocation);
        return new MediaAccessStatus(level, locationIncluded);
    }

    /// <summary>
    /// Groups the runtime permissions requested together.
    /// </summary>
    private sealed class MediaPermissions : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(34)
                ?
                [
                    (global::Android.Manifest.Permission.ReadMediaImages, true),
                    (global::Android.Manifest.Permission.ReadMediaVideo, true),
                    (global::Android.Manifest.Permission.ReadMediaVisualUserSelected, true),
                    (global::Android.Manifest.Permission.AccessMediaLocation, true),
                ]
                :
                [
                    (global::Android.Manifest.Permission.ReadMediaImages, true),
                    (global::Android.Manifest.Permission.ReadMediaVideo, true),
                    (global::Android.Manifest.Permission.AccessMediaLocation, true),
                ];
    }
}
