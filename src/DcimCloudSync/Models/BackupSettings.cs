namespace DcimCloudSync.Models;

/// <summary>
/// The backup settings a user enters on the device.
/// </summary>
/// <param name="TenantId">The directory (tenant) ID.</param>
/// <param name="ClientId">The application (client) ID.</param>
/// <param name="BlobServiceUri">The blob service endpoint, such as <c>https://account.blob.core.windows.net/</c>.</param>
/// <param name="ContainerName">The container that receives backups.</param>
/// <param name="PhotoPrefix">The blob name prefix for photos.</param>
/// <param name="VideoPrefix">The blob name prefix for videos.</param>
/// <param name="AccessTier">The access tier for new backups, or an empty string for the account default.</param>
/// <param name="AutomaticBackup">Whether new photos and videos are backed up in the background on unmetered networks.</param>
/// <param name="AllFoldersIncluded">Whether every folder under DCIM is backed up, rather than only the camera folder <c>DCIM/Camera</c>.</param>
public sealed record BackupSettings(
    string TenantId,
    string ClientId,
    string BlobServiceUri,
    string ContainerName,
    string PhotoPrefix,
    string VideoPrefix,
    string AccessTier,
    bool AutomaticBackup,
    bool AllFoldersIncluded);
