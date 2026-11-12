namespace CasCap.Models;

/// <summary>
/// An access token and the time it stops being valid.
/// </summary>
/// <param name="Token">The bearer token; never log or display it.</param>
/// <param name="ExpiresOn">The token's expiry.</param>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresOn)
{
    /// <inheritdoc/>
    public override string ToString() => $"{nameof(AccessTokenResult)} {{ ExpiresOn = {ExpiresOn:O} }}";
}
