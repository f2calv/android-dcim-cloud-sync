using DcimCloudSync.Exceptions;
using Microsoft.Extensions.Options;

namespace DcimCloudSync.Services;

/// <summary>
/// Runs one unattended backup: checks it is enabled and permitted, never prompts for sign-in, and posts a notification
/// when something was backed up or the user needs to act.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="settingsStore">The device-entered settings.</param>
/// <param name="mediaAccess">The media permission service, checked without prompting.</param>
/// <param name="backupService">The backup service shared with manual runs.</param>
/// <param name="notifier">The notification publisher.</param>
public sealed partial class BackgroundBackupRunner(
    ILogger<BackgroundBackupRunner> logger,
    IBackupSettingsStore settingsStore,
    IMediaAccess mediaAccess,
    BackupService backupService,
    BackupNotifier notifier)
{
    /// <summary>Destination failures are retried silently this many times before the user is told.</summary>
    internal const int SilentRetryCount = 3;

    /// <summary>
    /// Runs one background backup.
    /// </summary>
    /// <param name="runAttemptCount">How many times WorkManager already attempted this run.</param>
    /// <param name="cancellationToken">Signals that WorkManager stopped the run.</param>
    /// <returns>Whether WorkManager should retry the run.</returns>
    public async Task<BackgroundBackupOutcome> RunAsync(int runAttemptCount, CancellationToken cancellationToken)
    {
        if (!settingsStore.Load().AutomaticBackup || !backupService.IsAvailable)
            return BackgroundBackupOutcome.Completed;

        if (mediaAccess.GetStatus().Level == MediaAccessLevel.Denied)
        {
            notifier.NotifyAttention("Allow photo and video access in the app to resume automatic backups.");
            return BackgroundBackupOutcome.Completed;
        }

        try
        {
            BackupSummary summary;
            using (InteractiveSignIn.Suppress())
                summary = await backupService.BackUpChangesAsync(null, cancellationToken).ConfigureAwait(false);

            notifier.NotifySummary(summary);
            return BackgroundBackupOutcome.Completed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogStopped(logger, nameof(BackgroundBackupRunner));
            return BackgroundBackupOutcome.Retry;
        }
        catch (SignInRequiredException)
        {
            notifier.NotifyAttention("Sign in again to resume automatic backups. Open the app and tap Back up.");
            return BackgroundBackupOutcome.Completed;
        }
        catch (BackupDestinationException ex) when (runAttemptCount < SilentRetryCount)
        {
            LogFailed(logger, nameof(BackgroundBackupRunner), runAttemptCount, ex.GetType().Name);
            return BackgroundBackupOutcome.Retry;
        }
        catch (BackupDestinationException ex)
        {
            LogFailed(logger, nameof(BackgroundBackupRunner), runAttemptCount, ex.GetType().Name);
            notifier.NotifyAttention($"Automatic backup stopped: {ex.Message}");
            return BackgroundBackupOutcome.Completed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OptionsValidationException)
        {
            LogFailed(logger, nameof(BackgroundBackupRunner), runAttemptCount, ex.GetType().Name);
            return BackgroundBackupOutcome.Completed;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{ClassName} background backup stopped by the system; it will be retried")]
    private static partial void LogStopped(ILogger logger, string className);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{ClassName} background backup failed RunAttemptCount={RunAttemptCount} ExceptionType={ExceptionType}")]
    private static partial void LogFailed(ILogger logger, string className, int runAttemptCount, string exceptionType);
}
