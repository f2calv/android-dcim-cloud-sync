using Microsoft.Maui.Storage;

namespace CasCap.Services;

/// <summary>
/// Keeps the incremental backup checkpoint in MAUI <see cref="IPreferences"/>.
/// </summary>
/// <remarks>The token describes the media index only, such as volume versions and generations, never items.</remarks>
/// <param name="preferences">The preference store.</param>
public sealed class PreferencesBackupCheckpointStore(IPreferences preferences) : IBackupCheckpointStore
{
    private const string ChangeTokenKey = "Backup:ChangeToken";

    /// <inheritdoc/>
    public string? Load() => preferences.Get<string?>(ChangeTokenKey, null);

    /// <inheritdoc/>
    public void Save(string changeToken) => preferences.Set(ChangeTokenKey, changeToken);

    /// <inheritdoc/>
    public void Clear() => preferences.Remove(ChangeTokenKey);
}
