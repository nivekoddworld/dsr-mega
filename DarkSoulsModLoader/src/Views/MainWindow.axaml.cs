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
    private ModBrowserPage _modBrowserPage;

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

            // Recreate services with detected game directory
            var gameDir = await configService.GetGameDirectoryAsync() ?? "";
            if (!string.IsNullOrEmpty(gameDir))
            {
                modService = new ModService(gameDir);
                gameLaunchService = new GameLaunchService(gameDir);
            }

            var modsVM = new ModManagerViewModel(modService, configService);
            var launchVM = new LaunchPageViewModel(gameLaunchService, modService, configService);
            var settingsVM = new SettingsPageViewModel(configService, gameLaunchService, modService);
            var modBrowserService = new ModBrowserService();
            var modBrowserVM = new ModBrowserViewModel(modBrowserService, configService);

            await modsVM.LoadModsAsync();

            // Initialize mod config files if they don't exist
            var modConfigLoader = new ModConfigLoaderService(modService);
            await modConfigLoader.InitializeModConfigFilesAsync();

            _modsPage = new ModsPage { DataContext = modsVM };
            _launchPage = new LaunchPage { DataContext = launchVM };
            _settingsPage = new SettingsPage { DataContext = settingsVM };
            _modBrowserPage = new ModBrowserPage { DataContext = modBrowserVM };

            // Build the UI
            var navListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                Background = new SolidColorBrush(Color.Parse("#1a1a1a")),
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = new SolidColorBrush(Color.Parse("#444444")),
                Padding = new Thickness(0)
            };
            var contentArea = new ContentControl();

            var modsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            modsItem.Content = new TextBlock { Text = "Mods", Margin = new Thickness(16, 16, 16, 16), Foreground = new SolidColorBrush(Color.Parse("#cccccc")), FontWeight = FontWeight.Bold };

            var launchItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            launchItem.Content = new TextBlock { Text = "Launch", Margin = new Thickness(16, 16, 16, 16), Foreground = new SolidColorBrush(Color.Parse("#cccccc")), FontWeight = FontWeight.Bold };

            var settingsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            settingsItem.Content = new TextBlock { Text = "Settings", Margin = new Thickness(16, 16, 16, 16), Foreground = new SolidColorBrush(Color.Parse("#cccccc")), FontWeight = FontWeight.Bold };

            var browserItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            browserItem.Content = new TextBlock { Text = "Browse", Margin = new Thickness(16, 16, 16, 16), Foreground = new SolidColorBrush(Color.Parse("#cccccc")), FontWeight = FontWeight.Bold };

            // Store nav items to update styling
            var navItems = new[] { modsItem, launchItem, browserItem, settingsItem };
            navListBox.Items.Add(modsItem);
            navListBox.Items.Add(launchItem);
            navListBox.Items.Add(browserItem);
            navListBox.Items.Add(settingsItem);

            navListBox.SelectionChanged += (s, e) =>
            {
                // Update nav item styling
                for (int i = 0; i < navItems.Length; i++)
                {
                    if (i == navListBox.SelectedIndex)
                    {
                        navItems[i].Background = new SolidColorBrush(Color.Parse("#3a3a3a"));
                        ((TextBlock)navItems[i].Content).Foreground = new SolidColorBrush(Color.Parse("#d4af37"));
                    }
                    else
                    {
                        navItems[i].Background = new SolidColorBrush(Colors.Transparent);
                        ((TextBlock)navItems[i].Content).Foreground = new SolidColorBrush(Color.Parse("#cccccc"));
                    }
                }

                contentArea.Content = navListBox.SelectedIndex switch
                {
                    0 => _modsPage,
                    1 => _launchPage,
                    2 => _modBrowserPage,
                    3 => _settingsPage,
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
