using Android.App;
using Android.Content;
using CasCap.Services;
using Microsoft.Identity.Client;

namespace CasCap;

/// <summary>
/// Receives the browser redirect that completes an MSAL interactive sign-in.
/// </summary>
[Activity(Exported = true, NoHistory = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryBrowsable, Intent.CategoryDefault],
    DataHost = "auth",
    DataScheme = MsalAccessTokenProvider.RedirectScheme)]
public sealed class MsalActivity : BrowserTabActivity;
