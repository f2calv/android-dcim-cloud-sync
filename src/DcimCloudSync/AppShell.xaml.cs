using DcimCloudSync.Pages;

namespace DcimCloudSync;

/// <summary>
/// Provides the single-page application navigation shell.
/// </summary>
public sealed partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    /// <param name="page">The constructor-injected diagnostic page.</param>
    public AppShell(MainPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        InitializeComponent();
        Items.Add(new ShellContent
        {
            Content = page,
            Route = "main",
            Title = "Diagnostic",
        });
    }
}
