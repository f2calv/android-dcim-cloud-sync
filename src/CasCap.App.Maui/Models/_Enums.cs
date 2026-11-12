namespace CasCap.Models;

/// <summary>
/// What WorkManager should do after a background backup run.
/// </summary>
public enum BackgroundBackupOutcome
{
    /// <summary>The run finished, or stopped for a reason a retry cannot fix; wait for the next trigger.</summary>
    Completed,

    /// <summary>The run was stopped or hit a transient failure; retry with backoff.</summary>
    Retry,
}
