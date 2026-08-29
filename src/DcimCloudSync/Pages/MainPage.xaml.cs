using DcimCloudSync.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Text;

namespace DcimCloudSync.Pages;

/// <summary>
/// Displays and coordinates the temporary foreground Pixel 7 MediaStore diagnostic.
/// </summary>
public sealed partial class MainPage : ContentPage
{
    private readonly IPixel7MediaStoreDiagnostic _diagnostic;
    private CancellationTokenSource? _currentRunCancellationTokenSource;
    private MediaAccessDiagnosticSnapshot? _accessSnapshot;
    private bool _combinedAccessWasRequested;
    private bool _isBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="diagnostic">The Android-free synthetic diagnostic boundary.</param>
    public MainPage(IPixel7MediaStoreDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        InitializeComponent();
        _diagnostic = diagnostic;
    }

    private async void OnRequestImagesClicked(object? sender, EventArgs e)
    {
        await RequestAccessAsync(
            VisualMediaAccessRequestOperation.Images,
            "Busy: requesting image access.");
    }

    private async void OnRequestVideosClicked(object? sender, EventArgs e)
    {
        await RequestAccessAsync(
            VisualMediaAccessRequestOperation.Videos,
            "Busy: requesting video access.");
    }

    private async void OnRequestAllOrManageSelectionClicked(object? sender, EventArgs e)
    {
        var operation = CanManageSelection()
            ? VisualMediaAccessRequestOperation.ManageSelection
            : VisualMediaAccessRequestOperation.Combined;
        var busyStatus = operation == VisualMediaAccessRequestOperation.ManageSelection
            ? "Busy: opening selected-media management."
            : "Busy: requesting image and video access.";

        await RequestAccessAsync(operation, busyStatus);
    }

    private async void OnRefreshAccessClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        SetBusyState(isBusy: true, isDiagnosticRunning: false);
        SetOperationStatus("Busy: refreshing visual-media access.");

        try
        {
            var snapshot = await _diagnostic.RefreshAccessAsync(CancellationToken.None);
            await MainThread.InvokeOnMainThreadAsync(() => ApplyAccessSnapshot(snapshot));
        }
        catch (NotSupportedException)
        {
            SetOperationStatus("Unsupported transition: this visual-media access operation is unavailable on the runtime API.");
        }
        catch (Exception)
        {
            SetOperationStatus("Failed: visual-media access could not be refreshed. No exception details are displayed.");
        }
        finally
        {
            SetBusyState(isBusy: false, isDiagnosticRunning: false);
        }
    }

    private async void OnRunDiagnosticClicked(object? sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        var currentRun = new CancellationTokenSource();
        _currentRunCancellationTokenSource = currentRun;
        SetBusyState(isBusy: true, isDiagnosticRunning: true);
        SetOperationStatus("Busy: running the foreground synthetic diagnostic.");

        try
        {
            var report = await _diagnostic.RunDiagnosticAsync(currentRun.Token);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                currentRun.Token.ThrowIfCancellationRequested();
                ApplyDiagnosticReport(report);
            });
        }
        catch (OperationCanceledException)
        {
            SetOperationStatus("Canceled: the current diagnostic run did not complete successfully.");
        }
        catch (NotSupportedException) when (!currentRun.IsCancellationRequested)
        {
            SetOperationStatus("Unsupported transition: the requested diagnostic operation is unavailable on this runtime.");
        }
        catch (Exception) when (currentRun.IsCancellationRequested)
        {
            SetOperationStatus("Canceled: the current diagnostic run did not complete successfully.");
        }
        catch (Exception)
        {
            SetOperationStatus("Failed: the diagnostic did not complete. No exception details are displayed.");
        }
        finally
        {
            if (ReferenceEquals(_currentRunCancellationTokenSource, currentRun))
            {
                _currentRunCancellationTokenSource = null;
            }

            currentRun.Dispose();
            SetBusyState(isBusy: false, isDiagnosticRunning: false);
        }
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        var currentRun = _currentRunCancellationTokenSource;
        if (currentRun is null || currentRun.IsCancellationRequested)
        {
            return;
        }

        CancelButton.IsEnabled = false;
        SetOperationStatus("Busy: canceling the current diagnostic run.");
        currentRun.Cancel();
    }

    private async Task RequestAccessAsync(
        VisualMediaAccessRequestOperation operation,
        string busyStatus)
    {
        if (_isBusy)
        {
            return;
        }

        SetBusyState(isBusy: true, isDiagnosticRunning: false);
        SetOperationStatus(busyStatus);

        try
        {
            var snapshot = await _diagnostic.RequestAccessAsync(operation, CancellationToken.None);
            if (operation == VisualMediaAccessRequestOperation.Combined)
            {
                _combinedAccessWasRequested = true;
            }

            await MainThread.InvokeOnMainThreadAsync(() => ApplyAccessSnapshot(snapshot));
        }
        catch (NotSupportedException)
        {
            SetOperationStatus("Unsupported transition: selected-media management is unavailable on this runtime API.");
        }
        catch (Exception)
        {
            SetOperationStatus("Failed: the access request did not complete. No exception details are displayed.");
        }
        finally
        {
            SetBusyState(isBusy: false, isDiagnosticRunning: false);
        }
    }

    private void ApplyAccessSnapshot(MediaAccessDiagnosticSnapshot snapshot)
    {
        _accessSnapshot = snapshot;
        AccessSummaryLabel.Text = FormatAccessSummary(snapshot);
        SemanticProperties.SetDescription(AccessSummaryLabel, AccessSummaryLabel.Text);
        RequestAllOrManageSelectionButton.Text = CanManageSelection()
            ? "Manage selection"
            : "Request all";
        SetOperationStatus(GetAccessStatus(snapshot));
    }

    private void ApplyDiagnosticReport(Pixel7DiagnosticReport report)
    {
        ApplyAccessSnapshot(report.Access);
        ResultEditor.Text = FormatDiagnosticReport(report);
        SetOperationStatus(GetDiagnosticStatus(report));
    }

    private bool CanManageSelection()
    {
        return _accessSnapshot is
        {
            IsReselectionAvailable: true,
        } snapshot
            && (_combinedAccessWasRequested
                || snapshot.SelectedAccess == SelectedVisualMediaAccessStatus.Granted
                || snapshot.Completeness == VisualMediaAccessCompleteness.Complete);
    }

    private void SetBusyState(bool isBusy, bool isDiagnosticRunning)
    {
        _isBusy = isBusy;
        RequestImagesButton.IsEnabled = !isBusy;
        RequestVideosButton.IsEnabled = !isBusy;
        RequestAllOrManageSelectionButton.IsEnabled = !isBusy;
        RefreshAccessButton.IsEnabled = !isBusy;
        RunDiagnosticButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = isDiagnosticRunning;
        BusyIndicator.IsRunning = isBusy;
        BusyIndicator.IsVisible = isBusy;
    }

    private void SetOperationStatus(string status)
    {
        OperationStatusLabel.Text = status;
        SemanticProperties.SetDescription(OperationStatusLabel, status);
    }

    private static string GetAccessStatus(MediaAccessDiagnosticSnapshot snapshot)
    {
        var hasImages = snapshot.ImageAccess == VisualMediaGrantStatus.Granted;
        var hasVideos = snapshot.VideoAccess == VisualMediaGrantStatus.Granted;
        var hasSelectedMedia = snapshot.SelectedAccess == SelectedVisualMediaAccessStatus.Granted;

        return (hasImages, hasVideos, hasSelectedMedia) switch
        {
            (true, true, _) => "Full access: broad image and broad video access are granted.",
            (false, false, true) => "Selected-only access: only user-selected visual media is available.",
            (true, false, false) => "Images-only access: broad image access is granted; broad video access is denied.",
            (false, true, false) => "Videos-only access: broad video access is granted; broad image access is denied.",
            (true, false, true) => "Partial mixed access: broad images and selected visual media are available; broad video access is denied.",
            (false, true, true) => "Partial mixed access: broad videos and selected visual media are available; broad image access is denied.",
            _ => "Denied: no broad or selected visual-media access is granted.",
        };
    }

    private static string GetDiagnosticStatus(Pixel7DiagnosticReport report)
    {
        return report.ResultCode switch
        {
            Pixel7DiagnosticReport.DiagnosticResultCode.Success => "Diagnostic completed successfully.",
            Pixel7DiagnosticReport.DiagnosticResultCode.AccessDenied => "Denied: the diagnostic could not access the synthetic fixture set.",
            Pixel7DiagnosticReport.DiagnosticResultCode.PartialAccess => $"Diagnostic completed with {GetAccessStatus(report.Access).ToLowerInvariant()}",
            Pixel7DiagnosticReport.DiagnosticResultCode.UnsupportedTransition => "Unsupported transition: the requested diagnostic transition is unavailable on this runtime.",
            Pixel7DiagnosticReport.DiagnosticResultCode.Canceled => "Canceled: the current diagnostic run did not complete successfully.",
            _ => $"Failed: the diagnostic returned the sanitized result code {report.ResultCode}.",
        };
    }

    private static string FormatAccessSummary(MediaAccessDiagnosticSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine(GetAccessStatus(snapshot));
        AppendInvariant(builder, $"Runtime API: {snapshot.RuntimeApiLevel}");
        builder.AppendLine($"Broad image grant: {snapshot.ImageAccess}");
        builder.AppendLine($"Broad video grant: {snapshot.VideoAccess}");
        builder.AppendLine($"Selected-media grant: {snapshot.SelectedAccess}");
        builder.AppendLine($"Selected-media reselection: {(snapshot.IsReselectionAvailable ? "available" : "not supported")}");
        builder.AppendLine($"Permission rationale: {(snapshot.ShouldShowAnyRationale ? "recommended before another request" : "not currently indicated")}");
        builder.Append($"Protected media location: {snapshot.MediaLocationAccess}");
        return builder.ToString();
    }

    private static string FormatDiagnosticReport(Pixel7DiagnosticReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Run summary");
        builder.AppendLine($"Result: {report.ResultCode}");
        builder.AppendLine($"Duration: {FormatDuration(report.Duration)}");
        AppendInvariant(builder, $"Runtime API: {report.Environment.RuntimeApiLevel}");
        AppendInvariant(builder, $"Target API: {report.Environment.TargetApiLevel}");
        builder.AppendLine($"Platform version: {report.Environment.PlatformVersion}");
        builder.AppendLine($"Security patch month: {report.Environment.SecurityPatchMonth}");
        builder.AppendLine($"Media provider module version: {report.Environment.MediaProviderModuleVersion ?? "not reported"}");
        builder.AppendLine($"Fixture set version: {report.Environment.FixtureSetVersion}");

        builder.AppendLine();
        builder.AppendLine("Access used by run");
        builder.AppendLine(FormatAccessSummary(report.Access));

        builder.AppendLine();
        builder.AppendLine("Permission scenario expectations");
        foreach (var expectation in report.ScenarioExpectations)
        {
            builder.AppendLine($"- {expectation.Scenario}: {FormatFixtureIds(expectation.ExpectedVisibleFixtureIds)}");
        }

        builder.AppendLine();
        builder.AppendLine("Aggregate discovery observations");
        foreach (var observation in report.DiscoveryObservations)
        {
            AppendInvariant(
                builder,
                $"- {observation.MediaKind}: result={observation.ResultCode}; visible expected={observation.ExpectedVisibleFixtureCount}, actual={observation.ActualVisibleFixtureCount}; DCIM expected={observation.ExpectedDcimFixtureCount}, actual={observation.ActualDcimFixtureCount}; unexpected controls={observation.UnexpectedControlFixtureCount}; duration={FormatDuration(observation.Duration)}");
        }

        builder.AppendLine();
        builder.AppendLine("Fixture observations");
        foreach (var fixture in report.FixtureObservations)
        {
            builder.AppendLine($"- {fixture.FixtureId} ({fixture.MediaKind}): discovery={fixture.DiscoveryResultCode}");
            foreach (var observation in fixture.OpenPathObservations)
            {
                AppendInvariant(
                    builder,
                    $"  - {observation.OpenPath}: result={observation.ResultCode}; bytes expected={observation.ExpectedByteCount}, actual={FormatNullable(observation.ActualByteCount)}; SHA-256 expected={observation.ExpectedSha256}, actual={observation.ActualSha256 ?? "not observed"}; descriptor length={observation.DescriptorTraits.ReportedLength}, seek={observation.DescriptorTraits.CanSeek}; duration={FormatDuration(observation.Duration)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatFixtureIds(IReadOnlyList<string> fixtureIds)
    {
        return fixtureIds.Count == 0
            ? "none"
            : string.Join(", ", fixtureIds);
    }

    private static string FormatNullable(long? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "not observed";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{duration.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)} ms";
    }

    private static void AppendInvariant(StringBuilder builder, FormattableString value)
    {
        builder.AppendLine(value.ToString(CultureInfo.InvariantCulture));
    }
}