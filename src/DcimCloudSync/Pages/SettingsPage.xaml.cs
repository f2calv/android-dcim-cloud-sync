using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;

namespace DcimCloudSync.Pages;

/// <summary>
/// Edits the backup settings entered on the device and signs the user out.
/// </summary>
public sealed partial class SettingsPage : ContentPage
{
    private readonly IBackupSettingsStore _settingsStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    /// <param name="settingsStore">The settings store.</param>
    public SettingsPage(IBackupSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);

        InitializeComponent();
        _settingsStore = settingsStore;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        var settings = _settingsStore.Load();
        TenantIdEntry.Text = settings.TenantId;
        ClientIdEntry.Text = settings.ClientId;
        BlobServiceUriEntry.Text = settings.BlobServiceUri;
        ContainerNameEntry.Text = settings.ContainerName;
        PhotoPrefixEntry.Text = settings.PhotoPrefix;
        VideoPrefixEntry.Text = settings.VideoPrefix;
        AccessTierEntry.Text = settings.AccessTier;
        AutomaticBackupSwitch.IsToggled = settings.AutomaticBackup;
        AllFoldersIncludedSwitch.IsToggled = settings.AllFoldersIncluded;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var automaticBackup = AutomaticBackupSwitch.IsToggled;
        var errors = _settingsStore.Save(new BackupSettings(
            TenantIdEntry.Text ?? string.Empty,
            ClientIdEntry.Text ?? string.Empty,
            BlobServiceUriEntry.Text ?? string.Empty,
            ContainerNameEntry.Text ?? string.Empty,
            PhotoPrefixEntry.Text ?? string.Empty,
            VideoPrefixEntry.Text ?? string.Empty,
            AccessTierEntry.Text ?? string.Empty,
            automaticBackup,
            AllFoldersIncludedSwitch.IsToggled));
        if (errors.Count > 0)
        {
            ShowStatus(string.Join(" ", errors));
            return;
        }

        var notificationsAllowed = !automaticBackup
            || await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;
        ShowStatus(notificationsAllowed ? "Saved." : "Saved. Notifications are off, so automatic backups run without a summary.");
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        try
        {
            await _settingsStore.SignOutAsync();
            ShowStatus("Signed out. The next backup asks you to sign in.");
        }
        catch (Exception ex) when (ex is Exceptions.BackupDestinationException or InvalidOperationException)
        {
            ShowStatus("Sign-out failed. Check the directory and application IDs.");
        }
    }

    private void ShowStatus(string text)
    {
        SettingsStatusLabel.Text = text;
        SemanticScreenReader.Default.Announce(text);
    }
}
