using Android.Content;
using Android.Provider;
using AndroidX.Work;
using Microsoft.Maui.ApplicationModel;
using Java.Util.Concurrent;

namespace CasCap.Services;

/// <summary>
/// Schedules background backups with WorkManager: one run shortly after new photos or videos appear, and a periodic
/// catch-up run. Both wait for an unmetered network and a battery that is not low.
/// </summary>
/// <remarks>
/// A content-URI trigger fires once, so each new-media run re-arms the trigger when it finishes by replacing its own unique
/// work. Replacing, rather than appending a successor, deletes the finished request's records: an append chain keeps every
/// finished link, and their stale job bookkeeping makes WorkManager reschedule on each cold start, which stops the very job
/// that started the process. A new-media run that must be retried hands over to a one-time catch-up run instead, because a
/// retried content-trigger request would wait for another media change. WorkManager persists every schedule across
/// restarts and reboots.
/// </remarks>
public sealed class WorkManagerBackupScheduler : IBackgroundBackupScheduler
{
    /// <summary>The unique work name of the new-media trigger.</summary>
    internal const string NewMediaWorkName = "backup-new-media";

    /// <summary>The unique work name of the one-time catch-up run after a new-media run failed transiently.</summary>
    internal const string RetryWorkName = "backup-retry";

    /// <summary>The unique work name of the periodic catch-up run.</summary>
    internal const string PeriodicWorkName = "backup-periodic";

    private const long PeriodicIntervalHours = 6;
    private const long RetryDelayMinutes = 5;
    private const long TriggerUpdateDelaySeconds = 15;
    private const long TriggerMaxDelaySeconds = 60;

    /// <inheritdoc/>
    public void Apply(bool enabled)
    {
        var workManager = WorkManager.GetInstance(Platform.AppContext);
        if (!enabled)
        {
            _ = workManager.CancelUniqueWork(NewMediaWorkName);
            _ = workManager.CancelUniqueWork(RetryWorkName);
            _ = workManager.CancelUniqueWork(PeriodicWorkName);
            return;
        }

        _ = workManager.EnqueueUniqueWork(NewMediaWorkName, ExistingWorkPolicy.Keep!, CreateNewMediaRequest());
        var periodic = new PeriodicWorkRequest.Builder(Java.Lang.Class.FromType(typeof(BackupWorker)), PeriodicIntervalHours, TimeUnit.Hours!)
            .SetConstraints(CreateConstraints(watchMedia: false))
            .SetInputData(BackupWorker.CreateInputData(BackupWorker.PeriodicTrigger))
            .Build();
        _ = workManager.EnqueueUniquePeriodicWork(PeriodicWorkName, ExistingPeriodicWorkPolicy.Update!, periodic);
    }

    /// <summary>
    /// Re-arms the new-media trigger by replacing the finishing request, so media added from now on starts another run.
    /// </summary>
    /// <param name="context">The application context.</param>
    internal static void RearmNewMediaTrigger(Context context) =>
        _ = WorkManager.GetInstance(context).EnqueueUniqueWork(NewMediaWorkName, ExistingWorkPolicy.Replace!, CreateNewMediaRequest());

    /// <summary>
    /// Schedules a one-time catch-up run, unless one is already waiting, after a new-media run failed transiently.
    /// </summary>
    /// <param name="context">The application context.</param>
    internal static void EnqueueRetry(Context context)
    {
        var request = (OneTimeWorkRequest)new OneTimeWorkRequest.Builder(Java.Lang.Class.FromType(typeof(BackupWorker)))
            .SetConstraints(CreateConstraints(watchMedia: false))
            .SetInputData(BackupWorker.CreateInputData(BackupWorker.RetryTrigger))
            .SetInitialDelay(RetryDelayMinutes, TimeUnit.Minutes!)!
            .Build();
        _ = WorkManager.GetInstance(context).EnqueueUniqueWork(RetryWorkName, ExistingWorkPolicy.Keep!, request);
    }

    private static OneTimeWorkRequest CreateNewMediaRequest() =>
        (OneTimeWorkRequest)new OneTimeWorkRequest.Builder(Java.Lang.Class.FromType(typeof(BackupWorker)))
            .SetConstraints(CreateConstraints(watchMedia: true))
            .SetInputData(BackupWorker.CreateInputData(BackupWorker.NewMediaTrigger))
            .Build();

    private static Constraints CreateConstraints(bool watchMedia)
    {
        var builder = new Constraints.Builder()
            .SetRequiredNetworkType(NetworkType.Unmetered!)
            .SetRequiresBatteryNotLow(true);
        if (watchMedia)
        {
            builder = builder
                .AddContentUriTrigger(MediaStore.Images.Media.ExternalContentUri!, triggerForDescendants: true)
                .AddContentUriTrigger(MediaStore.Video.Media.ExternalContentUri!, triggerForDescendants: true)
                .SetTriggerContentUpdateDelay(TriggerUpdateDelaySeconds, TimeUnit.Seconds!)
                .SetTriggerContentMaxDelay(TriggerMaxDelaySeconds, TimeUnit.Seconds!);
        }

        return builder.Build();
    }
}
