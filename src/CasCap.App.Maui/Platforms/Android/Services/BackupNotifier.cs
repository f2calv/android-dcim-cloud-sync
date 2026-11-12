using System.Globalization;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;

namespace CasCap.Services;

/// <summary>
/// Posts background backup notifications: a short summary when new items were backed up, and a prompt when the user must act.
/// </summary>
/// <remarks>
/// Notifications carry counts and sizes only, never item names, paths, endpoints or accounts. Each kind reuses one
/// notification ID, so a later run replaces the earlier notification instead of stacking another; every summary alerts,
/// but a repeated prompt to act does not. Nothing is posted when the user has not allowed notifications.
/// </remarks>
public sealed class BackupNotifier
{
    private const string ChannelId = "backup-results";
    private const int SummaryNotificationId = 1;
    private const int AttentionNotificationId = 2;
    private const double BytesPerKibibyte = 1024;
    private const double BytesPerMebibyte = 1024 * 1024;

    /// <summary>
    /// Posts a summary when the run backed up at least one item.
    /// </summary>
    /// <param name="summary">The run summary.</param>
    public void NotifySummary(BackupSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (summary.UploadedCount == 0)
            return;

        var title = summary.UploadedCount == 1
            ? "Backed up 1 new photo or video"
            : string.Format(CultureInfo.CurrentCulture, "Backed up {0:N0} new photos and videos", summary.UploadedCount);
        var text = summary.UploadedBytes < BytesPerMebibyte
            ? string.Format(CultureInfo.CurrentCulture, "{0:N0} KiB uploaded.", Math.Ceiling(summary.UploadedBytes / BytesPerKibibyte))
            : string.Format(CultureInfo.CurrentCulture, "{0:N1} MiB uploaded.", summary.UploadedBytes / BytesPerMebibyte);
        var problems = summary.ConflictCount + summary.UnverifiedCount + summary.ChangedCount + summary.UnreadableCount;
        if (problems > 0)
            text += string.Format(CultureInfo.CurrentCulture, " {0:N0} could not be backed up; open the app for details.", problems);

        Post(SummaryNotificationId, title, text, alertOnce: false);
    }

    /// <summary>
    /// Posts a notification asking the user to act before automatic backups can continue.
    /// </summary>
    /// <param name="text">What the user should do, free of item names, paths, endpoints and accounts.</param>
    public void NotifyAttention(string text) => Post(AttentionNotificationId, "Automatic backup paused", text, alertOnce: true);

    private static void Post(int id, string title, string text, bool alertOnce)
    {
        var context = Platform.AppContext;
        if (context.CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != Permission.Granted
            || context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;

        // High importance shows a brief heads-up popup; the user can lower it in the channel's system settings.
        manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Backup results", NotificationImportance.High)
        {
            Description = "A short summary when new photos and videos are backed up, or when backups need you.",
        });

        var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var contentIntent = launch is null
            ? null
            : PendingIntent.GetActivity(context, id, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var notification = new Notification.Builder(context, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysUploadDone)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetStyle(new Notification.BigTextStyle().BigText(text))
            .SetCategory(Notification.CategoryStatus)
            .SetContentIntent(contentIntent)
            .SetAutoCancel(true)
            .SetOnlyAlertOnce(alertOnce)
            .Build();
        manager.Notify(id, notification);
    }
}
