using Azure.Core;

namespace DcimCloudSync.Services;

/// <summary>
/// Adapts a provider-neutral <see cref="IAccessTokenProvider"/> to the Azure SDK's <see cref="TokenCredential"/>.
/// </summary>
/// <param name="tokenProvider">The signed-in user's token provider.</param>
internal sealed class AccessTokenProviderCredential(IAccessTokenProvider tokenProvider) : TokenCredential
{
    /// <inheritdoc/>
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Synchronous token acquisition is not supported; use the asynchronous Azure SDK APIs.");

    /// <inheritdoc/>
    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var result = await tokenProvider.GetTokenAsync(requestContext.Scopes, cancellationToken).ConfigureAwait(false);
        return new AccessToken(result.Token, result.ExpiresOn);
    }
}
