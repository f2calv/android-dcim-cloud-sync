namespace DcimCloudSync.Tests;

/// <summary>
/// Reports progress synchronously, unlike <see cref="Progress{T}"/>, so tests can assert every report.
/// </summary>
/// <typeparam name="T">The progress value type.</typeparam>
/// <param name="handler">Receives each report.</param>
internal sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    /// <inheritdoc/>
    public void Report(T value) => handler(value);
}
