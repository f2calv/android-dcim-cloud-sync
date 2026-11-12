namespace DcimCloudSync.Abstractions;

/// <summary>
/// Persists the change token of the last backup run that read every changed item, so the next run can start from it.
/// </summary>
/// <remarks>Clear the checkpoint whenever the destination changes, so the next run backs everything up to the new one.</remarks>
public interface IBackupCheckpointStore
{
    /// <summary>
    /// Loads the saved change token.
    /// </summary>
    /// <returns>The token, or <see langword="null"/> when no complete run has been recorded.</returns>
    public string? Load();

    /// <summary>
    /// Saves the change token taken at the start of a complete run.
    /// </summary>
    /// <param name="changeToken">The token.</param>
    public void Save(string changeToken);

    /// <summary>
    /// Removes the saved token, so the next run reads every item.
    /// </summary>
    public void Clear();
}
