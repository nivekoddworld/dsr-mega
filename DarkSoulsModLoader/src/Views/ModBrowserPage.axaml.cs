using Avalonia.Controls;
using Avalonia.Interactivity;
using DarkSoulsModLoader.ViewModels;
using System.Threading.Tasks;

namespace DarkSoulsModLoader.Views;

public partial class ModBrowserPage : UserControl
{
    public ModBrowserPage()
    {
        InitializeComponent();
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ModBrowserViewModel vm)
        {
            await vm.RefreshModsAsync();
        }
    }

    private async void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ModBrowserItemViewModel item)
        {
            await item.InstallAsync();
        }
    }
}
