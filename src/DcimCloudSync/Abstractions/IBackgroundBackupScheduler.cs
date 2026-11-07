namespace DcimCloudSync.Abstractions;

/// <summary>
/// Schedules or cancels background backups that run without the app on screen.
/// </summary>
public interface IBackgroundBackupScheduler
{
    /// <summary>
    /// Schedules background backups when enabled, keeping any existing schedule, or cancels them when disabled.
    /// </summary>
    /// <param name="enabled">Whether background backups should run.</param>
    public void Apply(bool enabled);
}
