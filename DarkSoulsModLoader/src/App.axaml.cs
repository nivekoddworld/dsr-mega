using System;
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
        var configService = new ConfigService();
        var modService = new ModService("");
        var gameLaunchService = new GameLaunchService("");

        if (ApplicationLifetime != null)
        {
            dynamic lifetime = ApplicationLifetime;
            lifetime.MainWindow = new MainWindow(configService, modService, gameLaunchService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
