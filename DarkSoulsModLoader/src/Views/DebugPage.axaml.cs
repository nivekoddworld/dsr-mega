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

    private async void OnCopySelectedClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DebugPageViewModel vm && this.FindControl<ListBox>("LogListBox") is ListBox listBox)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            await vm.CopySelected(listBox.SelectedItems, clipboard);
        }
    }

    private async void OnCopyAllClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is DebugPageViewModel vm)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            await vm.CopyAll(clipboard);
        }
    }
}
