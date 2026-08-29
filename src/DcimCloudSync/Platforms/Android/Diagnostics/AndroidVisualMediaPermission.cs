using Microsoft.Maui.ApplicationModel;

namespace DcimCloudSync.Platforms.Android.Diagnostics;

/// <summary>
/// Contains the API-aware Android permission groups used by the temporary visual-media diagnostic.
/// </summary>
internal static class AndroidVisualMediaPermission
{
    /// <summary>Requests broad image access independently.</summary>
    internal sealed class Images : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        [
            (global::Android.Manifest.Permission.ReadMediaImages, true),
        ];
    }

    /// <summary>Requests broad video access independently.</summary>
    internal sealed class Videos : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        [
            (global::Android.Manifest.Permission.ReadMediaVideo, true),
        ];
    }

    /// <summary>Requests broad visual-media access and API 34+ selected-media access together.</summary>
    internal sealed class Combined : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(34)
                ?
                [
                    (global::Android.Manifest.Permission.ReadMediaImages, true),
                    (global::Android.Manifest.Permission.ReadMediaVideo, true),
                    (global::Android.Manifest.Permission.ReadMediaVisualUserSelected, true),
                ]
                :
                [
                    (global::Android.Manifest.Permission.ReadMediaImages, true),
                    (global::Android.Manifest.Permission.ReadMediaVideo, true),
                ];
    }

    /// <summary>Requests API 34+ selected-media access independently for rationale inspection.</summary>
    internal sealed class Selected : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            OperatingSystem.IsAndroidVersionAtLeast(34)
                ?
                [
                    (global::Android.Manifest.Permission.ReadMediaVisualUserSelected, true),
                ]
                : [];
    }
}
