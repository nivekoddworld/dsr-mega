using System;
using Avalonia;
using Avalonia.Themes.Fluent;

namespace DarkSoulsModLoader;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            // Write to temp file so we can verify execution
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsr_loader_log.txt"),
                "PROGRAM STARTED\n");

            System.Diagnostics.Debug.WriteLine("*** PROGRAM START ***");
            Console.WriteLine("DarkSoulsModLoader starting...");
            Console.Out.Flush();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            Console.WriteLine("App exited normally");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsr_loader_log.txt"),
                $"ERROR: {ex}\n{ex.StackTrace}\n");

            Console.WriteLine($"FATAL: {ex}");
            Console.WriteLine(ex.StackTrace);
            Console.Out.Flush();
            throw;
        }
    }

    static AppBuilder BuildAvaloniaApp()
    {
        System.IO.File.AppendAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsr_loader_log.txt"),
            "BuildAvaloniaApp() called\n");

        var builder = AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

        System.IO.File.AppendAllText(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dsr_loader_log.txt"),
            "AppBuilder created, about to call StartWithClassicDesktopLifetime\n");

        return builder;
    }
}
