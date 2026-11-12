using Android.App;
using Android.Content;
using DcimCloudSync.Services;
using Microsoft.Identity.Client;

namespace DcimCloudSync;

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
