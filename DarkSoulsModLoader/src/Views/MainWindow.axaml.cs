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
    private DebugPage _debugPage;
    private LogService _logService = new();

    public MainWindow()
    {
        var configService = new ConfigService();
        var modService = new ModService("", _logService);
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
            _logService.Log("Initializing app...");

            // Auto-detect game directory on first load
            await configService.InitializeAsync();

            // Recreate services with detected game directory
            var gameDir = await configService.GetGameDirectoryAsync() ?? "";
            if (!string.IsNullOrEmpty(gameDir))
            {
                _logService.Log($"Game directory: {gameDir}");
                modService = new ModService(gameDir, _logService);
                gameLaunchService = new GameLaunchService(gameDir);
            }

            var modsVM = new ModManagerViewModel(modService, configService);
            var launchVM = new LaunchPageViewModel(gameLaunchService, modService, configService);
            var settingsVM = new SettingsPageViewModel(configService, gameLaunchService, modService);
            var modBrowserService = new ModBrowserService();
            var modBrowserVM = new ModBrowserViewModel(modBrowserService, configService);
            var debugVM = new DebugPageViewModel(_logService);

            await modsVM.LoadModsAsync();

            // Initialize mod config files if they don't exist
            var modConfigLoader = new ModConfigLoaderService(modService);
            await modConfigLoader.InitializeModConfigFilesAsync();

            _modsPage = new ModsPage { DataContext = modsVM };
            _launchPage = new LaunchPage { DataContext = launchVM };
            _settingsPage = new SettingsPage { DataContext = settingsVM };
            _modBrowserPage = new ModBrowserPage { DataContext = modBrowserVM };
            _debugPage = new DebugPage { DataContext = debugVM };

            // Build sleek, sophisticated UI
            var navListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8)
            };

            var navBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                BorderBrush = new SolidColorBrush(Color.Parse("#d4af37")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Child = navListBox
            };

            var contentArea = new ContentControl();

            var modsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            modsItem.Content = new TextBlock { Text = "Mods", Margin = new Thickness(12, 12, 12, 12), Foreground = new SolidColorBrush(Color.Parse("#d4af37")), FontWeight = FontWeight.Bold, FontSize = 14 };

            var launchItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            launchItem.Content = new TextBlock { Text = "Launch", Margin = new Thickness(12, 12, 12, 12), Foreground = new SolidColorBrush(Color.Parse("#d4af37")), FontWeight = FontWeight.Bold, FontSize = 14 };

            var browserItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            browserItem.Content = new TextBlock { Text = "Browse", Margin = new Thickness(12, 12, 12, 12), Foreground = new SolidColorBrush(Color.Parse("#d4af37")), FontWeight = FontWeight.Bold, FontSize = 14 };

            var settingsItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            settingsItem.Content = new TextBlock { Text = "Settings", Margin = new Thickness(12, 12, 12, 12), Foreground = new SolidColorBrush(Color.Parse("#d4af37")), FontWeight = FontWeight.Bold, FontSize = 14 };

            var debugItem = new ListBoxItem { Padding = new Thickness(0), Background = new SolidColorBrush(Colors.Transparent) };
            debugItem.Content = new TextBlock { Text = "Debug", Margin = new Thickness(12, 12, 12, 12), Foreground = new SolidColorBrush(Color.Parse("#d4af37")), FontWeight = FontWeight.Bold, FontSize = 14 };

            // Store nav items to update styling
            var navItems = new[] { modsItem, launchItem, browserItem, settingsItem, debugItem };
            navListBox.Items.Add(modsItem);
            navListBox.Items.Add(launchItem);
            navListBox.Items.Add(browserItem);
            navListBox.Items.Add(settingsItem);
            navListBox.Items.Add(debugItem);

            navListBox.SelectionChanged += (s, e) =>
            {
                // Update nav item styling
                for (int i = 0; i < navItems.Length; i++)
                {
                    if (i == navListBox.SelectedIndex)
                    {
                        navItems[i].Background = new SolidColorBrush(Color.Parse("#5a4a3a"));
                        ((TextBlock)navItems[i].Content).Foreground = new SolidColorBrush(Color.Parse("#ffd700"));
                    }
                    else
                    {
                        navItems[i].Background = new SolidColorBrush(Colors.Transparent);
                        ((TextBlock)navItems[i].Content).Foreground = new SolidColorBrush(Color.Parse("#d4af37"));
                    }
                }

                contentArea.Content = navListBox.SelectedIndex switch
                {
                    0 => _modsPage,
                    1 => _launchPage,
                    2 => _modBrowserPage,
                    3 => _settingsPage,
                    4 => _debugPage,
                    _ => null
                };
            };

            var contentBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#0f0f0f")),
                BorderBrush = new SolidColorBrush(Color.Parse("#d4af37")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Margin = new Thickness(0),
                Child = contentArea
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(220, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            grid.Background = new SolidColorBrush(Color.Parse("#000000"));
            grid.Margin = new Thickness(0);

            Grid.SetColumn(navBorder, 0);
            Grid.SetColumn(contentBorder, 1);
            grid.Children.Add(navBorder);
            grid.Children.Add(contentBorder);

            this.Content = grid;
            navListBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            this.Content = new TextBlock { Text = $"Error: {ex.Message}", Foreground = new SolidColorBrush(Color.Parse("#cc4125")), Margin = new Thickness(16) };
        }
    }
}
