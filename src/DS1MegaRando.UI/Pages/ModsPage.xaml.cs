using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DS1MegaRando.UI.ViewModels;
using Microsoft.Win32;

namespace DS1MegaRando.UI.Pages;

public partial class ModsPage : UserControl
{
    private string _modsDir = "";

    public ModsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        string gameDir = GetGameDir();
        _modsDir = string.IsNullOrWhiteSpace(gameDir) ? "" : Path.Combine(gameDir, "mods");

        ModsDirText.Text = string.IsNullOrWhiteSpace(gameDir)
            ? "Game directory not set — configure it on the Global page."
            : _modsDir;

        RefreshInstalledList();
        RefreshBundledList();
    }

    private void RefreshInstalledList()
    {
        var items = new List<ModItem>();
        if (Directory.Exists(_modsDir))
        {
            foreach (string dll in Directory.EnumerateFiles(_modsDir, "*.dll"))
            {
                var info = new FileInfo(dll);
                items.Add(new ModItem(Path.GetFileName(dll), dll, FormatSize(info.Length)));
            }
        }

        ModList.ItemsSource = items;
        EmptyHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshBundledList()
    {
        string bundledDir = Path.Combine(AppContext.BaseDirectory, "bundled-mods");
        if (!Directory.Exists(bundledDir))
        {
            BundledPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var items = new List<BundledModItem>();
        foreach (string dll in Directory.EnumerateFiles(bundledDir, "*.dll"))
        {
            string fileName   = Path.GetFileName(dll);
            bool   installed  = !string.IsNullOrEmpty(_modsDir) &&
                                 File.Exists(Path.Combine(_modsDir, fileName));
            items.Add(new BundledModItem(fileName, dll, installed));
        }

        if (items.Count == 0)
        {
            BundledPanel.Visibility = Visibility.Collapsed;
            return;
        }

        BundledList.ItemsSource = items;
        BundledPanel.Visibility = Visibility.Visible;
    }

    // ── Installed mods handlers ───────────────────────────────────────────────

    private void InstallMod_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureModsDir()) return;

        var dlg = new OpenFileDialog
        {
            Title  = "Select mod DLL",
            Filter = "Mod DLL (*.dll)|*.dll",
        };
        if (dlg.ShowDialog() != true) return;

        CopyToMods(dlg.FileName);
    }

    private void RemoveMod_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string filePath }) return;

        var result = MessageBox.Show(
            $"Remove {Path.GetFileName(filePath)}?",
            "Remove Mod", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            File.Delete(filePath);
            RefreshInstalledList();
            RefreshBundledList(); // update "installed" badges
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not remove mod:\n{ex.Message}",
                "Remove Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Bundled mods handler ──────────────────────────────────────────────────

    private void InstallBundled_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string sourcePath }) return;
        if (!EnsureModsDir()) return;
        CopyToMods(sourcePath);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private void CopyToMods(string sourcePath)
    {
        string dest = Path.Combine(_modsDir, Path.GetFileName(sourcePath));
        if (File.Exists(dest))
        {
            var r = MessageBox.Show(
                $"{Path.GetFileName(dest)} is already installed. Replace it?",
                "Replace Mod?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
        }

        try
        {
            File.Copy(sourcePath, dest, overwrite: true);
            RefreshInstalledList();
            RefreshBundledList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not install mod:\n{ex.Message}",
                "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureModsDir()) return;
        Process.Start(new ProcessStartInfo("explorer.exe", _modsDir) { UseShellExecute = true });
    }

    private bool EnsureModsDir()
    {
        if (string.IsNullOrWhiteSpace(_modsDir))
        {
            MessageBox.Show("Please set the game directory on the Global page first.",
                "No Game Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        Directory.CreateDirectory(_modsDir);
        return true;
    }

    private string GetGameDir() =>
        DataContext is MainViewModel vm ? vm.MegaSettings.Global.GameDirectory ?? "" : "";

    private static string FormatSize(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:F1} MB"
        : bytes >= 1_024   ? $"{bytes / 1_024.0:F0} KB"
        : $"{bytes} B";
}

// ── View models ───────────────────────────────────────────────────────────────

internal sealed record ModItem(string FileName, string FilePath, string SizeText);

internal sealed class BundledModItem : INotifyPropertyChanged
{
    public string FileName   { get; }
    public string SourcePath { get; }

    private bool _installed;
    public bool Installed
    {
        get => _installed;
        set
        {
            _installed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Installed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanInstall)));
        }
    }

    public string StatusText => _installed ? "installed" : "";
    public string ActionLabel => _installed ? "REINSTALL" : "INSTALL";
    public bool   CanInstall  => true; // always allow reinstall

    public event PropertyChangedEventHandler? PropertyChanged;

    public BundledModItem(string fileName, string sourcePath, bool installed)
    {
        FileName   = fileName;
        SourcePath = sourcePath;
        _installed = installed;
    }
}
