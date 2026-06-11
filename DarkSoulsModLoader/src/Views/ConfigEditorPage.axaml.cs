using Avalonia.Controls;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class ConfigEditorPage : UserControl
{
    public ConfigEditorPage()
    {
        InitializeComponent();
    }

    private async void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ConfigEditorViewModel vm)
        {
            await vm.SaveConfigAsync();
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Find the parent ModManagerViewModel and close the config editor
        var parent = this.Parent;
        while (parent != null)
        {
            if (parent is UserControl uc && uc.DataContext is ModManagerViewModel mvm)
            {
                mvm.ShowConfigEditor = false;
                mvm.SelectedMod = null;
                break;
            }
            parent = (parent as Control)?.Parent;
        }
    }
}
