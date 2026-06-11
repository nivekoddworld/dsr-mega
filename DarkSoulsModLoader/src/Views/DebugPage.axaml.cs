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

    private void OnCopySelectedClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DebugPageViewModel vm && this.FindControl<ListBox>("LogListBox") is ListBox listBox)
        {
            vm.CopySelected(listBox.SelectedItems);
        }
    }

    private void OnCopyAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DebugPageViewModel vm)
        {
            vm.CopyAll();
        }
    }
}
