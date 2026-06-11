using System;
using Avalonia;
using Avalonia.Themes.Fluent;

namespace DarkSoulsModLoader;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
