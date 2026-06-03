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
            "Global"  => new GlobalPage  { DataContext = Settings },
            "FogGate" => new FogGatePage { DataContext = Settings },
            "Items"   => new ItemPage    { DataContext = Settings },
            "Enemies" => new EnemyPage   { DataContext = Settings },
            "Spoiler" => new SpoilerPage { Log = _lastSpoilerLog },
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

    private void LaunchGame_Click(object sender, RoutedEventArgs e)
    {
        var gameDir = Settings.MegaSettings.Global.GameDirectory;
        var exe = string.IsNullOrWhiteSpace(gameDir)
            ? null
            : Path.Combine(gameDir, "DarkSoulsRemastered.exe");

        if (exe == null || !File.Exists(exe))
        {
            MessageBox.Show("Game executable not found. Please set the game directory first.",
                "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = gameDir,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not launch game: {ex.Message}",
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
