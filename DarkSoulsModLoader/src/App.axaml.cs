using Avalonia;
using Avalonia.Markup.Xaml;
using DarkSoulsModLoader.Services;
using DarkSoulsModLoader.Views;

namespace DarkSoulsModLoader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize services
        var configService = new ConfigService();
        var gameDir = configService.GetGameDirectoryAsync().Result ?? "";
        var modService = new ModService(gameDir);
        var gameLaunchService = new GameLaunchService(gameDir);

        if (ApplicationLifetime != null)
        {
            dynamic lifetime = ApplicationLifetime;
            lifetime.MainWindow = new MainWindow(configService, modService, gameLaunchService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
