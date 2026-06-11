using Avalonia.Controls;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class DebugPage : UserControl
{
    public DebugPage()
    {
        InitializeComponent();
    }

    private void OnClearClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DebugPageViewModel vm)
        {
            vm.ClearLogs();
        }
    }
}
