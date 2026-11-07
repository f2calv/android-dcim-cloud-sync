using DcimCloudSync.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Maui.ApplicationModel;

namespace DcimCloudSync.Services;

/// <summary>
/// Acquires Microsoft Entra tokens for the signed-in user with MSAL, silently when possible and interactively otherwise.
/// </summary>
/// <remarks>
/// The authentication broker is deliberately not enabled, so a device-management broker on a work profile cannot replace
/// the owner's account. Interactive sign-in uses the system browser and returns through <see cref="RedirectUri"/>. MSAL
/// persists its token cache in app-private storage; mobile platforms do not support custom cache serialization.
/// </remarks>
/// <param name="config">The tenant and client identifiers entered on the device.</param>
public sealed class MsalAccessTokenProvider(IOptionsMonitor<EntraConfig> config) : IAccessTokenProvider
{
    /// <summary>The redirect URI registered on the app registration and handled by <c>MsalActivity</c>.</summary>
    public const string RedirectUri = RedirectScheme + "://auth";

    /// <summary>The redirect URI scheme, which is the application ID and so needs no per-registration value.</summary>
    public const string RedirectScheme = "io.github.f2calv.androiddcimcloudsync";

    private readonly Lock _lock = new();
    private (string Key, IPublicClientApplication Application)? _application;

    /// <inheritdoc/>
    public bool IsConfigured
    {
        get
        {
            try
            {
                return config.CurrentValue.IsConfigured;
            }
            catch (OptionsValidationException)
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<AccessTokenResult> GetTokenAsync(IReadOnlyList<string> scopes, CancellationToken cancellationToken)
    {
        var application = GetApplication();
        try
        {
            var account = (await application.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();
            var silent = await application.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return new AccessTokenResult(silent.AccessToken, silent.ExpiresOn);
        }
        catch (MsalUiRequiredException)
        {
            // Fall through to interactive sign-in.
        }
        catch (MsalException ex)
        {
            throw new BackupDestinationException($"Sign-in failed ({ex.ErrorCode}).", ex);
        }

        if (Platform.CurrentActivity is not { } activity)
            throw new BackupDestinationException("Sign-in is required. Open the app and back up again.", new InvalidOperationException());

        try
        {
            var interactive = await MainThread.InvokeOnMainThreadAsync(() => application
                .AcquireTokenInteractive(scopes)
                .WithParentActivityOrWindow(activity)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync(cancellationToken)).ConfigureAwait(false);
            return new AccessTokenResult(interactive.AccessToken, interactive.ExpiresOn);
        }
        catch (MsalException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            throw new OperationCanceledException("Sign-in was canceled.", ex, cancellationToken);
        }
        catch (MsalException ex)
        {
            throw new BackupDestinationException($"Sign-in failed ({ex.ErrorCode}).", ex);
        }
    }

    /// <inheritdoc/>
    public async Task SignOutAsync()
    {
        if (!IsConfigured)
            return;

        var application = GetApplication();
        foreach (var account in await application.GetAccountsAsync().ConfigureAwait(false))
            await application.RemoveAsync(account).ConfigureAwait(false);
    }

    private IPublicClientApplication GetApplication()
    {
        var current = config.CurrentValue;
        if (!current.IsConfigured)
            throw new BackupDestinationException("Enter the directory and application IDs in Settings.", new InvalidOperationException());

        var key = $"{current.TenantId}|{current.ClientId}";
        lock (_lock)
        {
            if (_application is { } cached && cached.Key == key)
                return cached.Application;

            var application = PublicClientApplicationBuilder
                .Create(current.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, current.TenantId)
                .WithRedirectUri(RedirectUri)
                .Build();
            _application = (key, application);
            return application;
        }
    }
}
