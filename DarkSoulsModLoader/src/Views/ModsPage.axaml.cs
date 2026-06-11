using Avalonia.Controls;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class ModsPage : UserControl
{
    public ModsPage()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ModManagerViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(ModManagerViewModel.ShowConfigEditor))
                {
                    var panel = this.FindControl<UserControl>("ConfigEditorPanel");
                    if (panel != null)
                    {
                        if (vm.ShowConfigEditor)
                        {
                            // Show: expand column 2 to 400px
                            SetGridColumns("*,Auto,400");
                            // Slide in from right + fade in
                            panel.Opacity = 1.0;
                            if (panel.RenderTransform is Avalonia.Media.TranslateTransform translate)
                            {
                                translate.X = 0;
                            }
                        }
                        else
                        {
                            // Hide: collapse column 2 to 0px
                            SetGridColumns("*,Auto,0");
                            // Slide out to right + fade out
                            panel.Opacity = 0.0;
                            if (panel.RenderTransform is Avalonia.Media.TranslateTransform translate)
                            {
                                translate.X = 400;
                            }
                        }
                        panel.IsHitTestVisible = vm.ShowConfigEditor;
                    }
                }
            };
        }
    }

    private void SetGridColumns(string columnDefinitions)
    {
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null)
        {
            mainGrid.ColumnDefinitions = Avalonia.Controls.ColumnDefinitions.Parse(columnDefinitions);
        }
    }

    private async void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ModManagerViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }

    private async void OnUninstallClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ModItemViewModel mod && DataContext is ModManagerViewModel vm)
        {
            await vm.UninstallModAsync(mod.Name);
        }
    }
}
