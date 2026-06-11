using System;
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
        try
        {
            var configService = new ConfigService();
            var gameDir = configService.GetGameDirectoryAsync().Result ?? "";
            var modService = new ModService(gameDir);
            var gameLaunchService = new GameLaunchService(gameDir);

            InitializeServices(configService, modService, gameLaunchService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in MainWindow ctor: {ex}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public MainWindow(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        InitializeServices(configService, modService, gameLaunchService);
    }

    private void InitializeServices(ConfigService configService, ModService modService, GameLaunchService gameLaunchService)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent starting...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainWindow: InitializeComponent completed");

            // Create ViewModels
            System.Diagnostics.Debug.WriteLine("MainWindow: Creating ViewModels...");
            var modsVM = new ModManagerViewModel(modService);
            var launchVM = new LaunchPageViewModel(gameLaunchService, modService, configService);
            var settingsVM = new SettingsPageViewModel(configService, gameLaunchService);
            System.Diagnostics.Debug.WriteLine("MainWindow: ViewModels created");

            // Load mods
            _ = modsVM.LoadModsAsync();

            // Create Pages with ViewModels
            System.Diagnostics.Debug.WriteLine("MainWindow: Creating pages...");
            _modsPage = new ModsPage { DataContext = modsVM };
            _launchPage = new LaunchPage { DataContext = launchVM };
            _settingsPage = new SettingsPage { DataContext = settingsVM };
            System.Diagnostics.Debug.WriteLine("MainWindow: Pages created");

            System.Diagnostics.Debug.WriteLine("MainWindow: Finding controls...");
            var navListBox = this.FindControl<ListBox>("NavListBox");
            var contentArea = this.FindControl<ContentControl>("ContentArea");
            System.Diagnostics.Debug.WriteLine($"MainWindow: navListBox={navListBox != null}, contentArea={contentArea != null}");

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
                System.Diagnostics.Debug.WriteLine("MainWindow: Setting SelectedIndex to 0...");
                navListBox.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine("MainWindow: Window initialization complete");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow: ERROR - Could not find NavListBox or ContentArea!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in InitializeServices: {ex}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }
}
