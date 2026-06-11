using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class LaunchPage : UserControl
{
    public LaunchPage()
    {
        InitializeComponent();
    }

    private async void OnLaunchClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LaunchPageViewModel vm)
        {
            await vm.LaunchGameAsync();
        }
    }

    private async void OnBrowseGameDirectory(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (this.VisualRoot is Window window)
        {
            var folder = await dialog.ShowAsync(window);
            if (!string.IsNullOrEmpty(folder) && DataContext is LaunchPageViewModel vm)
            {
                await vm.BrowseGameDirectoryAsync(folder);
            }
        }
    }
}
