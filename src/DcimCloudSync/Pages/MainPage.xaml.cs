using DcimCloudSync.Models;
using DcimCloudSync.Services;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Accessibility;
using System.Globalization;

namespace DcimCloudSync.Pages;

/// <summary>
/// Displays the application status and, where the platform provides a media source, a media inventory scan.
/// </summary>
public sealed partial class MainPage : ContentPage
{
    private const double BytesPerMebibyte = 1024 * 1024;

    private readonly MediaInventoryService? _mediaInventorySvc;
    private CancellationTokenSource? _scanCancellation;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="mediaInventorySvc">
    /// The media inventory service, or <see langword="null"/> on platforms that do not register a media source yet.
    /// </param>
    public MainPage(MediaInventoryService? mediaInventorySvc = null)
    {
        InitializeComponent();
        _mediaInventorySvc = mediaInventorySvc;
        MediaInventorySection.IsVisible = mediaInventorySvc is not null;
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        if (_mediaInventorySvc is not { } mediaInventorySvc || _scanCancellation is not null)
            return;

        using var scanCancellation = new CancellationTokenSource();
        _scanCancellation = scanCancellation;
        SetScanning(true);
        ScanStatusLabel.Text = "Scanning…";
        var progress = new Progress<MediaInventoryProgress>(report => ScanStatusLabel.Text = string.Format(
            CultureInfo.CurrentCulture,
            "Scanning: {0:N0} items, {1:N1} MiB read.",
            report.ItemCount,
            report.TotalBytes / BytesPerMebibyte));

        try
        {
            var summary = await Task.Run(
                () => mediaInventorySvc.ScanAsync(progress, scanCancellation.Token),
                scanCancellation.Token);
            ShowResult(string.Format(
                CultureInfo.CurrentCulture,
                "Scanned {0:N0} items ({1:N1} MiB) in {2:N1} s. Duplicates: {3:N0}. Unreadable: {4:N0}.",
                summary.ItemCount,
                summary.TotalBytes / BytesPerMebibyte,
                summary.Elapsed.TotalSeconds,
                summary.DuplicateCount,
                summary.UnreadableCount));
        }
        catch (OperationCanceledException)
        {
            ShowResult("Scan canceled.");
        }
        catch (DirectoryNotFoundException)
        {
            ShowResult("The development media folder was not found. See the project README to create or configure it.");
        }
        catch (OptionsValidationException)
        {
            ShowResult("The development media folder setting is invalid. See the project README to configure it.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowResult("The scan failed because the media folder could not be read.");
        }
        finally
        {
            _scanCancellation = null;
            SetScanning(false);
        }
    }

    private async void OnCancelScanClicked(object? sender, EventArgs e)
    {
        if (_scanCancellation is { } scanCancellation)
            await scanCancellation.CancelAsync();
    }

    private void SetScanning(bool isScanning)
    {
        ScanButton.IsEnabled = !isScanning;
        CancelScanButton.IsEnabled = isScanning;
        ScanActivityIndicator.IsRunning = isScanning;
        ScanActivityIndicator.IsVisible = isScanning;
    }

    private void ShowResult(string text)
    {
        ScanStatusLabel.Text = text;
        SemanticScreenReader.Default.Announce(text);
    }
}
