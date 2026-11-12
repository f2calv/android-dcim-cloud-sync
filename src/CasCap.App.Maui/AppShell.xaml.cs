namespace CasCap;

/// <summary>
/// Provides the application navigation shell: the status page, plus a settings tab where the platform registers one.
/// </summary>
public sealed partial class AppShell : Shell
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// </summary>
    /// <param name="page">The constructor-injected status page.</param>
    /// <param name="settingsPage">The settings page, or <see langword="null"/> on platforms configured another way.</param>
    /// <param name="logPage">The backup log page, or <see langword="null"/> where no backup log is registered.</param>
    public AppShell(MainPage page, SettingsPage? settingsPage = null, LogPage? logPage = null)
    {
        ArgumentNullException.ThrowIfNull(page);

        InitializeComponent();
        var tabs = new TabBar();
        tabs.Items.Add(new ShellContent { Content = page, Route = "main", Title = "Status" });
        if (settingsPage is not null)
            tabs.Items.Add(new ShellContent { Content = settingsPage, Route = "settings", Title = "Settings" });
        if (logPage is not null)
            tabs.Items.Add(new ShellContent { Content = logPage, Route = "log", Title = "Log" });
        Items.Add(tabs);
    }
}
