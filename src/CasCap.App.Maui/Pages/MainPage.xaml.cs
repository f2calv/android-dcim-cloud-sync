using CasCap.Exceptions;
using CasCap.Services;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;

namespace CasCap.Pages;

/// <summary>
/// Displays the application status and, where the platform registers them, media scan and backup actions.
/// </summary>
public sealed partial class MainPage : ContentPage
{
    private const double BytesPerMebibyte = 1024 * 1024;

    private readonly IMediaAccess? _mediaAccess;
    private readonly MediaInventoryService? _mediaInventorySvc;
    private readonly BackupService? _backupSvc;
    private CancellationTokenSource? _operationCancellation;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="mediaAccess">
    /// The media permission service, or <see langword="null"/> on platforms that do not register a media source yet.
    /// </param>
    /// <param name="mediaInventorySvc">
    /// The media inventory service, or <see langword="null"/> on platforms that do not register a media source yet.
    /// </param>
    /// <param name="backupSvc">
    /// The backup service, or <see langword="null"/> when no backup destination is configured.
    /// </param>
    public MainPage(IMediaAccess? mediaAccess = null, MediaInventoryService? mediaInventorySvc = null, BackupService? backupSvc = null)
    {
        InitializeComponent();
        _mediaAccess = mediaAccess;
        _mediaInventorySvc = mediaInventorySvc;
        _backupSvc = backupSvc;
        MediaSection.IsVisible = mediaInventorySvc is not null;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        BackUpButton.IsVisible = _backupSvc?.IsAvailable == true;
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        if (_mediaInventorySvc is not { } mediaInventorySvc)
            return;

        await RunAsync<MediaInventoryProgress, MediaInventorySummary>(
            "Scanning…",
            mediaInventorySvc.ScanAsync,
            report => string.Format(
                CultureInfo.CurrentCulture,
                "Scanning: {0:N0} items, {1:N1} MiB read.",
                report.ItemCount,
                report.TotalBytes / BytesPerMebibyte),
            summary => string.Format(
                CultureInfo.CurrentCulture,
                "Scanned {0:N0} items ({1:N1} MiB) in {2:N1} s. Duplicates: {3:N0}. Unreadable: {4:N0}.",
                summary.ItemCount,
                summary.TotalBytes / BytesPerMebibyte,
                summary.Elapsed.TotalSeconds,
                summary.DuplicateCount,
                summary.UnreadableCount));
    }

    private async void OnBackUpClicked(object? sender, EventArgs e)
    {
        if (_backupSvc is not { } backupSvc)
            return;

        // Automatic backups default to on, so ask here too for installs whose settings predate them.
        _ = await Permissions.RequestAsync<Permissions.PostNotifications>();
        await RunAsync<BackupProgress, BackupSummary>(
            "Backing up…",
            backupSvc.BackUpAsync,
            DescribeBackupProgress,
            summary => string.Format(
                CultureInfo.CurrentCulture,
                "Checked {0:N0} items in {1:N1} s. Uploaded: {2:N0} ({3:N1} MiB). Already backed up: {4:N0}. "
                    + "Conflicts: {5:N0}. Unverified: {6:N0}. Changed during backup: {7:N0}. Unreadable: {8:N0}.",
                summary.ItemCount,
                summary.Elapsed.TotalSeconds,
                summary.UploadedCount,
                summary.UploadedBytes / BytesPerMebibyte,
                summary.AlreadyBackedUpCount,
                summary.ConflictCount,
                summary.UnverifiedCount,
                summary.ChangedCount,
                summary.UnreadableCount));
    }

    private string DescribeBackupProgress(BackupProgress report)
    {
        var fraction = report.Phase is BackupItemPhase.Hashing or BackupItemPhase.Uploading && report.CurrentItemLength > 0
            ? Math.Clamp((double)report.CurrentItemBytes / report.CurrentItemLength, 0, 1)
            : (double?)null;
        ItemProgressBar.IsVisible = fraction is not null;
        if (fraction is { } value)
            ItemProgressBar.Progress = value;

        var totals = string.Format(
            CultureInfo.CurrentCulture,
            "{0:N0} uploaded so far ({1:N1} MiB).",
            report.UploadedCount,
            report.UploadedBytes / BytesPerMebibyte);
        return report.Phase switch
        {
            BackupItemPhase.Checking => string.Format(
                CultureInfo.CurrentCulture, "Item {0:N0}: checking {1}. {2}", report.ItemCount, report.CurrentItemName, totals),
            BackupItemPhase.Hashing or BackupItemPhase.Uploading => string.Format(
                CultureInfo.CurrentCulture,
                "Item {0:N0}: {1} {2}, {3:N1} of {4:N1} MiB. {5}",
                report.ItemCount,
                report.Phase == BackupItemPhase.Hashing ? "reading" : "uploading",
                report.CurrentItemName,
                report.CurrentItemBytes / BytesPerMebibyte,
                report.CurrentItemLength / BytesPerMebibyte,
                totals),
            _ => string.Format(CultureInfo.CurrentCulture, "Backing up: {0:N0} items checked. {1}", report.ItemCount, totals),
        };
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_operationCancellation is { } operationCancellation)
            await operationCancellation.CancelAsync();
    }

    private async Task RunAsync<TProgress, TResult>(
        string startText,
        Func<IProgress<TProgress>, CancellationToken, Task<TResult>> operation,
        Func<TProgress, string> progressText,
        Func<TResult, string> resultText)
    {
        if (_operationCancellation is not null)
            return;

        var access = _mediaAccess is null ? MediaAccessStatus.Unrestricted : await _mediaAccess.RequestAccessAsync();
        if (access.Level == MediaAccessLevel.Denied)
        {
            ShowResult("Photo and video access was not granted. Allow it in the app's settings, then try again.");
            AppInfo.Current.ShowSettingsUI();
            return;
        }

        var accessNote = (access.Level, access.LocationIncluded) switch
        {
            (MediaAccessLevel.Partial, false) => " Only the photos and videos you selected were read, without location.",
            (MediaAccessLevel.Partial, true) => " Only the photos and videos you selected were read.",
            (_, false) => " Photo location was not included.",
            _ => string.Empty,
        };

        using var operationCancellation = new CancellationTokenSource();
        _operationCancellation = operationCancellation;
        SetBusy(true);
        StatusLabel.Text = startText;
        var progress = new Progress<TProgress>(report => StatusLabel.Text = progressText(report));

        try
        {
            var result = await Task.Run(() => operation(progress, operationCancellation.Token), operationCancellation.Token);
            ShowResult(resultText(result) + accessNote);
        }
        catch (OperationCanceledException)
        {
            ShowResult("Canceled.");
        }
        catch (DirectoryNotFoundException)
        {
            ShowResult("The development media folder was not found. See the project README to create or configure it.");
        }
        catch (OptionsValidationException)
        {
            ShowResult("A media or backup setting is invalid. See the project README to configure it.");
        }
        catch (BackupDestinationException ex)
        {
            ShowResult($"The backup stopped: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowResult("The operation failed because the media could not be read.");
        }
        finally
        {
            _operationCancellation = null;
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        ScanButton.IsEnabled = !isBusy;
        BackUpButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = isBusy;
        ActivityIndicator.IsRunning = isBusy;
        ActivityIndicator.IsVisible = isBusy;
        if (!isBusy)
            ItemProgressBar.IsVisible = false;
    }

    private void ShowResult(string text)
    {
        StatusLabel.Text = text;
        SemanticScreenReader.Default.Announce(text);
    }
}
