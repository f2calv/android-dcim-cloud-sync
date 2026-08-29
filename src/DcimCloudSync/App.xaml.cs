using DcimCloudSync.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DcimCloudSync;

/// <summary>
/// Defines the application and creates its root window.
/// </summary>
public sealed partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider used by the composition root.</param>
    public App(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        return new Window(new AppShell(mainPage));
    }
}