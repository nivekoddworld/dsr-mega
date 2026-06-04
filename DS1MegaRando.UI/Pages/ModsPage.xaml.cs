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

    private void Refresh()
    {
        string gameDir = GetGameDir();
        _modsDir = Path.Combine(gameDir, "mods");
        ModsDirText.Text = string.IsNullOrWhiteSpace(gameDir)
            ? "Game directory not set — configure it on the Global page."
            : _modsDir;

        RefreshList();
    }

    private void RefreshList()
    {
        var items = new List<ModItem>();

        if (Directory.Exists(_modsDir))
        {
            foreach (string dll in Directory.EnumerateFiles(_modsDir, "*.dll"))
            {
                var info = new FileInfo(dll);
                items.Add(new ModItem(
                    Path.GetFileName(dll),
                    dll,
                    FormatSize(info.Length)));
            }
        }

        ModList.ItemsSource = items;
        EmptyHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void InstallMod_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureModsDir()) return;

        var dlg = new OpenFileDialog
        {
            Title  = "Select mod DLL",
            Filter = "Mod DLL (*.dll)|*.dll",
        };

        if (dlg.ShowDialog() != true) return;

        string dest = Path.Combine(_modsDir, Path.GetFileName(dlg.FileName));
        if (File.Exists(dest))
        {
            var result = MessageBox.Show(
                $"{Path.GetFileName(dest)} is already installed. Replace it?",
                "Replace Mod?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            File.Copy(dlg.FileName, dest, overwrite: true);
            RefreshList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not install mod:\n{ex.Message}",
                "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveMod_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string filePath) return;

        var result = MessageBox.Show(
            $"Remove {Path.GetFileName(filePath)}?",
            "Remove Mod", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            File.Delete(filePath);
            RefreshList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not remove mod:\n{ex.Message}",
                "Remove Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private string GetGameDir()
    {
        if (DataContext is MainViewModel vm)
            return vm.MegaSettings.Global.GameDirectory ?? "";
        return "";
    }

    private static string FormatSize(long bytes) =>
        bytes >= 1_048_576 ? $"{bytes / 1_048_576.0:F1} MB"
        : bytes >= 1_024   ? $"{bytes / 1_024.0:F0} KB"
        : $"{bytes} B";
}

internal sealed record ModItem(string FileName, string FilePath, string SizeText);
