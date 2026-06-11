using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnBrowseGameDirectory(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (this.VisualRoot is Window window && DataContext is SettingsPageViewModel vm)
        {
            var folder = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(folder))
            {
                await vm.SetGameDirectoryAsync(folder);
            }
        }
    }

    private async void OnBackupClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
        {
            await vm.BackupGameFilesAsync();
        }
    }

    private async void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
        {
            await vm.RestoreFromBackupAsync();
        }
    }

    private async void OnClearCacheClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
        {
            await vm.ClearCacheAsync();
        }
    }
}
