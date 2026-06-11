using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DarkSoulsModLoader.Services;
using DarkSoulsModLoader.ViewModels;

namespace DarkSoulsModLoader.Views;

public partial class MainWindow : Window
{
    private ModsPage _modsPage;
    private LaunchPage _launchPage;
    private SettingsPage _settingsPage;

    public MainWindow()
    {
        var configService = new ConfigService();
        var modService = new ModService("");
        var gameLaunchService = new GameLaunchService("");

        InitializeServices(configService, modService, gameLaunchService);
    }

    public MainWindow(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        InitializeServices(configService, modService, gameLaunchService);
    }

    private void InitializeServices(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        InitializeComponent();

        this.Opened += async (s, e) =>
        {
            await LoadPagesAsync(configService, modService, gameLaunchService);
        };
    }

    private async Task LoadPagesAsync(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        try
        {
            // Auto-detect game directory on first load
            await configService.InitializeAsync();

            var modsVM = new ModManagerViewModel(modService);
            var launchVM = new LaunchPageViewModel(gameLaunchService, modService, configService);
            var settingsVM = new SettingsPageViewModel(configService, gameLaunchService);

            await modsVM.LoadModsAsync();

            _modsPage = new ModsPage { DataContext = modsVM };
            _launchPage = new LaunchPage { DataContext = launchVM };
            _settingsPage = new SettingsPage { DataContext = settingsVM };

            // Build the UI
            var navListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                Background = new SolidColorBrush(Color.Parse("#2a2a2a")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };
            var contentArea = new ContentControl();

            var modsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            modsItem.Content = new TextBlock { Text = "Mods", Margin = new Thickness(16, 12, 16, 12), Foreground = new SolidColorBrush(Color.Parse("#cccccc")) };

            var launchItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            launchItem.Content = new TextBlock { Text = "Launch", Margin = new Thickness(16, 12, 16, 12), Foreground = new SolidColorBrush(Color.Parse("#cccccc")) };

            var settingsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            settingsItem.Content = new TextBlock { Text = "Settings", Margin = new Thickness(16, 12, 16, 12), Foreground = new SolidColorBrush(Color.Parse("#cccccc")) };

            navListBox.Items.Add(modsItem);
            navListBox.Items.Add(launchItem);
            navListBox.Items.Add(settingsItem);

            navListBox.SelectionChanged += (s, e) =>
            {
                contentArea.Content = navListBox.SelectedIndex switch
                {
                    0 => _modsPage,
                    1 => _launchPage,
                    2 => _settingsPage,
                    _ => null
                };
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            Grid.SetColumn(navListBox, 0);
            Grid.SetColumn(contentArea, 1);
            grid.Children.Add(navListBox);
            grid.Children.Add(contentArea);

            this.Content = grid;
            navListBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            this.Content = new TextBlock { Text = $"Error: {ex.Message}", Foreground = new SolidColorBrush(Color.Parse("#cc4125")), Margin = new Thickness(16) };
        }
    }
}
