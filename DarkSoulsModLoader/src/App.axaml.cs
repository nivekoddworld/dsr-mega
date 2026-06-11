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
        try
        {
            Log("OnFrameworkInitializationCompleted started");

            Log("Creating ConfigService...");
            var configService = new ConfigService();

            Log("Getting game directory...");
            var gameDir = configService.GetGameDirectoryAsync().Result ?? "";
            Log($"Game dir: {gameDir}");

            Log("Creating ModService...");
            var modService = new ModService(gameDir);

            Log("Creating GameLaunchService...");
            var gameLaunchService = new GameLaunchService(gameDir);

            Log("Setting MainWindow...");
            if (ApplicationLifetime != null)
            {
                dynamic lifetime = ApplicationLifetime;
                Log("Creating MainWindow instance...");
                lifetime.MainWindow = new MainWindow(configService, modService, gameLaunchService);
                Log("MainWindow set!");
            }

            Log("OnFrameworkInitializationCompleted done, calling base");
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR: {ex}");
            Log(ex.StackTrace);
            throw;
        }

        base.OnFrameworkInitializationCompleted();
        Log("Framework initialization completed!");
    }

    private void Log(string msg)
    {
        var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsr_loader_log.txt");
        System.IO.File.AppendAllText(logPath, msg + "\n");
    }
}
