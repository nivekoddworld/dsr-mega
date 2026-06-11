using Avalonia.Controls;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class ModsPage : UserControl
{
    public ModsPage()
    {
        InitializeComponent();
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ModManagerViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }
}
