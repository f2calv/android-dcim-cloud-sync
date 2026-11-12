namespace DcimCloudSync.Tests;

// TODO: Duplicates DcimCloudSync.Storage.Azure.Tests/Tests/StaticOptionsMonitor.cs. Move both into a shared test-support
// project once a third test project needs it; one small helper does not justify the extra project yet.

/// <summary>
/// Serves a fixed options value through <see cref="IOptionsMonitor{TOptions}"/> without change notifications.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
/// <param name="value">The value returned for every name.</param>
internal sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    /// <inheritdoc/>
    public T CurrentValue => value;

    /// <inheritdoc/>
    public T Get(string? name) => value;

    /// <inheritdoc/>
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
