using Android.Content;
using Android.Runtime;
using AndroidX.Work;

namespace CasCap.Services;

/// <summary>
/// Runs one background backup for WorkManager, using the services of the MAUI host that the process started.
/// </summary>
/// <remarks>
/// The Java class name is fixed because WorkManager persists it with each scheduled request; renaming it orphans existing
/// schedules until the app re-applies them on its next start.
/// </remarks>
// TODO: WorkManager stops ordinary work after about 10 minutes. A stopped run is retried and resumes, because items already
// backed up are skipped, but a single video whose upload takes longer never completes in the background; the user backs it
// up from the app instead. Running as a foreground dataSync service with progress was deferred to keep the first version
// simple; revisit when large videos show up as repeatedly unfinished.
[Register("io.github.f2calv.androiddcimcloudsync.BackupWorker")]
public sealed class BackupWorker : Worker
{
    /// <summary>The input value for a run started by new photos or videos.</summary>
    internal const string NewMediaTrigger = "new-media";

    /// <summary>The input value for a periodic catch-up run.</summary>
    internal const string PeriodicTrigger = "periodic";

    /// <summary>The input value for a one-time catch-up run after a new-media run failed transiently.</summary>
    internal const string RetryTrigger = "retry";

    private const string TriggerKey = "trigger";

    private readonly CancellationTokenSource _stopping = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupWorker"/> class; WorkManager calls it.
    /// </summary>
    /// <param name="context">The application context.</param>
    /// <param name="workerParams">The work parameters.</param>
    public BackupWorker(Context context, WorkerParameters workerParams)
        : base(context, workerParams)
    {
    }

    /// <summary>
    /// Creates the input data that names what started a run.
    /// </summary>
    /// <param name="trigger">One of <see cref="NewMediaTrigger"/>, <see cref="PeriodicTrigger"/> or <see cref="RetryTrigger"/>.</param>
    /// <returns>The input data.</returns>
    internal static Data CreateInputData(string trigger) => new Data.Builder().PutString(TriggerKey, trigger)!.Build();

    /// <inheritdoc/>
    public override Result DoWork()
    {
        var outcome = IPlatformApplication.Current?.Services.GetService<BackgroundBackupRunner>() is { } runner
            ? runner.RunAsync(RunAttemptCount, _stopping.Token).GetAwaiter().GetResult()
            : BackgroundBackupOutcome.Retry;

        if (InputData.GetString(TriggerKey) != NewMediaTrigger)
            return outcome == BackgroundBackupOutcome.Retry ? Result.InvokeRetry()! : Result.InvokeSuccess()!;

        if (outcome == BackgroundBackupOutcome.Retry)
            WorkManagerBackupScheduler.EnqueueRetry(ApplicationContext);

        // Replaces, and so cancels, this request; its result is no longer needed once the run is over.
        WorkManagerBackupScheduler.RearmNewMediaTrigger(ApplicationContext);
        return Result.InvokeSuccess()!;
    }

    /// <inheritdoc/>
    public override void OnStopped()
    {
        base.OnStopped();
        _stopping.Cancel();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _stopping.Dispose();
        base.Dispose(disposing);
    }
}
