using System.IO;
using System.Windows;
using System.Windows.Controls;
using DS1MegaRando.Core;
using DS1MegaRando.Settings;
using DS1MegaRando.UI.Pages;
using DS1MegaRando.UI.ViewModels;

namespace DS1MegaRando.UI;

public partial class MainWindow : Window
{
    public MainViewModel Settings { get; } = new();

    private string?       _lastSpoilerLog;
    private MegaResult?   _lastResult;
    private MegaSettings? _lastSettings;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Load saved settings if present
        string settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DS1MegaRando", "settings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                var loaded = MegaSettings.LoadFromJson(settingsPath);
                Settings.ApplyFrom(loaded);
            }
            catch { /* silently ignore corrupt settings */ }
        }

        // Navigate to global page by default
        NavigateTo("Global");
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string page)
            NavigateTo(page);
    }

    private void NavigateTo(string page)
    {
        PageContent.Content = page switch
        {
            "Global"  => new GlobalPage   { DataContext = Settings },
            "FogGate" => new FogGatePage  { DataContext = Settings },
            "Items"   => new ItemPage     { DataContext = Settings },
            "Enemies" => new EnemyPage    { DataContext = Settings },
            "Spoiler" => new SpoilerPage  { Log = _lastSpoilerLog },
            "Mods"    => new ModsPage     { DataContext = Settings },
            "Debug"   => BuildDebugPage(),
            "About"   => new AboutPage(),
            _         => PageContent.Content,
        };
    }

    private DebugPage BuildDebugPage()
    {
        var page = new DebugPage();
        if (_lastResult != null && _lastSettings != null)
            page.SetResult(_lastResult, _lastSettings);
        return page;
    }

    private void LaunchGame_Click(object sender, RoutedEventArgs e) =>
        LaunchViaSteam();

    private void LaunchDropdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is ContextMenu menu)
        {
            menu.PlacementTarget = btn;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void LaunchWithModFramework_Click(object sender, RoutedEventArgs e)
    {
        var gameDir = Settings.MegaSettings.Global.GameDirectory;
        if (string.IsNullOrWhiteSpace(gameDir))
        {
            MessageBox.Show("Please set the game directory first.",
                "No Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // The framework folder is placed next to the UI exe by build.bat.
        // It contains dinput8.dll + DS1Mod.Host.dll + DS1Mod.Core.dll + etc.
        string frameworkSrc = Path.Combine(AppContext.BaseDirectory, "framework");
        string dinputSrc    = Path.Combine(frameworkSrc, "dinput8.dll");

        if (!Directory.Exists(frameworkSrc))
        {
            MessageBox.Show(
                "The framework\\ folder was not found next to the executable.\n\n" +
                "Run build.bat to build and stage all framework files, then try again.",
                "Mod Framework Not Built", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!File.Exists(dinputSrc))
        {
            MessageBox.Show(
                "dinput8.dll is missing from the framework\\ folder.\n\n" +
                "This file is the C++ mod loader and must be compiled separately.\n\n" +
                "To build it:\n" +
                "  1. Install Visual Studio 2022\n" +
                "  2. Add the \"Desktop development with C++\" workload\n" +
                "  3. Run build.bat again — step 4 will build dinput8.dll\n\n" +
                "The C# framework files are present and ready; only dinput8.dll is missing.",
                "dinput8.dll Not Built", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            foreach (string src in Directory.EnumerateFiles(frameworkSrc))
                File.Copy(src, Path.Combine(gameDir, Path.GetFileName(src)), overwrite: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not deploy Mod Framework to game folder:\n{ex.Message}",
                "Deploy Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LaunchViaSteam();
    }

    private void RemoveModFramework_Click(object sender, RoutedEventArgs e)
    {
        var gameDir = Settings.MegaSettings.Global.GameDirectory;
        if (string.IsNullOrWhiteSpace(gameDir))
        {
            MessageBox.Show("Please set the game directory first.",
                "No Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dllPath = Path.Combine(gameDir, "dinput8.dll");
        if (!File.Exists(dllPath))
        {
            MessageBox.Show("Mod Framework is not currently installed in the game folder.",
                "Not Installed", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Remove Mod Framework from:\n{gameDir}?",
            "Remove Mod Framework", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            File.Delete(dllPath);
            MessageBox.Show("Mod Framework removed successfully.",
                "Removed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not remove dinput8.dll:\n{ex.Message}",
                "Remove Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchViaSteam()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "steam://run/570940",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not launch game via Steam: {ex.Message}",
                "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RollSeed_Click(object sender, RoutedEventArgs e)
    {
        Settings.MegaSettings.Global.Seed = GlobalSettings.GenerateRandomSeed();
        Settings.NotifySeedChanged();
    }

    private async void Randomize_Click(object sender, RoutedEventArgs e)
    {
        var ms = Settings.BuildMegaSettings();

        if (string.IsNullOrWhiteSpace(ms.Global.GameDirectory))
        {
            MessageBox.Show("Please set the game directory first.",
                "No Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ProgressOverlay.Visibility = Visibility.Visible;
        ProgressOverlay.Reset();
        StatusLabel.Text = "Randomizing...";

        var rando = new MegaRandomizer();
        rando.LogMessage      += (_, msg) => Dispatcher.Invoke(() => ProgressOverlay.AppendLog(msg));
        rando.ProgressChanged += (_, pct) => Dispatcher.Invoke(() => ProgressOverlay.SetProgress(pct));

        try
        {
            var result = await rando.RandomizeAsync(ms);
            _lastSpoilerLog = result.SpoilerLog?.FullText;
            _lastResult   = result;
            _lastSettings = ms;
            ProgressOverlay.SetComplete(success: true, "Randomization complete!");
            StatusLabel.Text = $"Done — Seed {ms.Global.Seed:X8}";
        }
        catch (Exception ex)
        {
            // Build a full message including inner exceptions so YAML parse errors
            // show their line/column context rather than just the outer wrapper.
            var sb = new System.Text.StringBuilder(ex.Message);
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append("\n  → ").Append(inner.Message);
                inner = inner.InnerException;
            }
            ProgressOverlay.SetComplete(success: false, sb.ToString());
            StatusLabel.Text = "Error!";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DS1MegaRando");
            Directory.CreateDirectory(dir);
            Settings.BuildMegaSettings().SaveToJson(Path.Combine(dir, "settings.json"));
        }
        catch { }
    }
}
