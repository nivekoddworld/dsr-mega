using System;
using Avalonia;
using Avalonia.Themes.Fluent;

namespace DarkSoulsModLoader;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("DarkSoulsModLoader starting...");
            Console.Out.Flush();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Console.WriteLine("App exited normally");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: {ex}");
            Console.WriteLine(ex.StackTrace);
            Console.Out.Flush();
            throw;
        }
    }

    static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
