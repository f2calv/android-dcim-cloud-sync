namespace CasCap.Abstractions;

/// <summary>
/// Loads and saves the backup settings entered on the device, and signs the user out.
/// </summary>
public interface IBackupSettingsStore
{
    /// <summary>
    /// Loads the saved settings, with defaults for values never saved.
    /// </summary>
    /// <returns>The settings.</returns>
    public BackupSettings Load();

    /// <summary>
    /// Saves the settings and applies them to the running app.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>Validation messages; empty when the settings were saved.</returns>
    public IReadOnlyList<string> Save(BackupSettings settings);

    /// <summary>
    /// Removes the signed-in account and its cached tokens.
    /// </summary>
    /// <returns>A task that completes when the user is signed out.</returns>
    public Task SignOutAsync();
}
