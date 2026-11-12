namespace CasCap.WinUI;

/// <summary>
/// Hosts the MAUI application in a WinUI process for development on Windows.
/// </summary>
public sealed partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App() => InitializeComponent();

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
