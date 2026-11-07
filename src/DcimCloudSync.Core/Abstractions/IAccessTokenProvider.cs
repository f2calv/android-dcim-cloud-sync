namespace DcimCloudSync.Abstractions;

/// <summary>
/// Acquires access tokens for a signed-in user, prompting for sign-in only when no cached token can be renewed.
/// </summary>
/// <remarks>
/// Failures that need the user, such as an expired sign-in while no screen is in the foreground, surface as
/// <see cref="Exceptions.BackupDestinationException"/> so a backup run stops instead of retrying.
/// </remarks>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets an access token for the given scopes.
    /// </summary>
    /// <param name="scopes">The requested scopes, such as <c>https://storage.azure.com/user_impersonation</c>.</param>
    /// <param name="cancellationToken">Cancels the acquisition.</param>
    /// <returns>The token and its expiry.</returns>
    public Task<AccessTokenResult> GetTokenAsync(IReadOnlyList<string> scopes, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value indicating whether the provider has the settings it needs, such as tenant and client identifiers.
    /// </summary>
    /// <remarks>Reading it performs no network I/O and never prompts.</remarks>
    public bool IsConfigured { get; }

    /// <summary>
    /// Removes every cached account and token, so the next acquisition prompts for sign-in.
    /// </summary>
    /// <returns>A task that completes when the cache is cleared.</returns>
    public Task SignOutAsync();
}
