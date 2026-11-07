using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace DcimCloudSync.Services;

/// <summary>
/// Exposes a fixed set of configuration keys stored in MAUI <see cref="IPreferences"/>, and reloads them on demand.
/// </summary>
/// <remarks>
/// Preferences hold only non-secret settings. Empty values are omitted, so bound options keep their defaults.
/// </remarks>
/// <param name="preferences">The preference store.</param>
/// <param name="keys">The configuration keys to expose, such as <c>Entra:TenantId</c>.</param>
public sealed class PreferencesConfigurationProvider(IPreferences preferences, IReadOnlyList<string> keys)
    : ConfigurationProvider, IConfigurationSource
{
    /// <inheritdoc/>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    /// <inheritdoc/>
    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            var value = preferences.Get<string?>(key, null);
            if (!string.IsNullOrWhiteSpace(value))
                data[key] = value;
        }

        Data = data;
    }

    /// <summary>
    /// Reloads the values and notifies options monitors of the change.
    /// </summary>
    public void Reload()
    {
        Load();
        OnReload();
    }
}
