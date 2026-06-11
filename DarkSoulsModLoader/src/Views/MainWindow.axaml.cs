using Avalonia.Controls;
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
        // Parameterless constructor for XAML instantiation
        var configService = new ConfigService();
        var gameDir = configService.GetGameDirectoryAsync().Result ?? "";
        var modService = new ModService(gameDir);
        var gameLaunchService = new GameLaunchService(gameDir);

        InitializeServices(configService, modService, gameLaunchService);
    }

    public MainWindow(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        InitializeServices(configService, modService, gameLaunchService);
    }

    private void InitializeServices(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        InitializeComponent();

        // Create ViewModels
        var modsVM = new ModManagerViewModel(modService);
        var launchVM = new LaunchPageViewModel(gameLaunchService, modService, configService);
        var settingsVM = new SettingsPageViewModel(configService, gameLaunchService);

        // Load mods
        _ = modsVM.LoadModsAsync();

        // Create Pages with ViewModels
        _modsPage = new ModsPage { DataContext = modsVM };
        _launchPage = new LaunchPage { DataContext = launchVM };
        _settingsPage = new SettingsPage { DataContext = settingsVM };

        var navListBox = this.FindControl<ListBox>("NavListBox");
        var contentArea = this.FindControl<ContentControl>("ContentArea");

        if (navListBox != null && contentArea != null)
        {
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

            // Show Mods page by default
            navListBox.SelectedIndex = 0;
        }
    }
}
