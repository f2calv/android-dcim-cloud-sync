using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace CasCap.Pages;

/// <summary>
/// Shows the most recent scan and backup events kept on the device.
/// </summary>
public sealed partial class LogPage : ContentPage
{
    private const double BytesPerKibibyte = 1024;

    private readonly IBackupLog _backupLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogPage"/> class.
    /// </summary>
    /// <param name="backupLog">The on-device backup log.</param>
    public LogPage(IBackupLog backupLog)
    {
        ArgumentNullException.ThrowIfNull(backupLog);

        InitializeComponent();
        _backupLog = backupLog;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _backupLog.Changed += OnLogChanged;
        Refresh();
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        _backupLog.Changed -= OnLogChanged;
        base.OnDisappearing();
    }

    private void OnLogChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    private void OnClearClicked(object? sender, EventArgs e) => _backupLog.Clear();

    private void Refresh() => EntriesView.ItemsSource = _backupLog.GetEntries().Select(Format).ToList();

    private static string Format(BackupLogEntry entry)
    {
        var time = entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var level = entry.Level == BackupLogLevel.Information ? string.Empty : $"{entry.Level}: ";
        var subject = entry.FileName is null
            ? string.Empty
            : string.Format(
                CultureInfo.CurrentCulture,
                "  {0} ({1:N0} KB)",
                entry.FileName,
                Math.Ceiling((entry.SizeBytes ?? 0) / BytesPerKibibyte));
        return $"{time}{subject}\n{level}{entry.Message}";
    }
}
