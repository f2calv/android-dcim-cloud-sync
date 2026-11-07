using DcimCloudSync.Abstractions;

namespace DcimCloudSync.Tests;

/// <summary>
/// Keeps the backup checkpoint in memory.
/// </summary>
internal sealed class InMemoryCheckpointStore : IBackupCheckpointStore
{
    /// <summary>
    /// Gets or sets the saved change token.
    /// </summary>
    public string? ChangeToken { get; set; }

    /// <inheritdoc/>
    public string? Load() => ChangeToken;

    /// <inheritdoc/>
    public void Save(string changeToken) => ChangeToken = changeToken;

    /// <inheritdoc/>
    public void Clear() => ChangeToken = null;
}
