using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class LaunchPageViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private string _gameDirectory = "";
    private string _statusText = "Ready to launch";
    private string _statusColor = "#27AE60";
    private int _enabledModCount = 0;
    private bool _isGameRunning = false;
    private bool _canLaunch = false;

    public string GameDirectory
    {
        get => _gameDirectory;
        set => SetProperty(ref _gameDirectory, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public int EnabledModCount
    {
        get => _enabledModCount;
        set => SetProperty(ref _enabledModCount, value);
    }

    public bool IsGameRunning
    {
        get => _isGameRunning;
        set => SetProperty(ref _isGameRunning, value);
    }

    public bool CanLaunch
    {
        get => _canLaunch;
        set => SetProperty(ref _canLaunch, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LaunchPageViewModel(GameLaunchService gameLaunchService, ModService modService, ConfigService configService)
    {
        _configService = configService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        GameDirectory = await _configService.GetGameDirectoryAsync() ?? "Not set";
        await RefreshStatusAsync();

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(2000);
                var currentDir = await _configService.GetGameDirectoryAsync();
                if (currentDir != GameDirectory && !string.IsNullOrEmpty(currentDir))
                {
                    GameDirectory = currentDir;
                    await RefreshStatusAsync();
                }
            }
        });
    }

    private GameLaunchService GetGameLaunchService()
    {
        var dir = GameDirectory;
        if (string.IsNullOrEmpty(dir) || dir == "Not set")
            return new GameLaunchService("");
        return new GameLaunchService(dir);
    }

    private ModService GetModService()
    {
        var dir = GameDirectory;
        if (string.IsNullOrEmpty(dir) || dir == "Not set")
            return new ModService("");
        return new ModService(dir);
    }

    public async Task RefreshStatusAsync()
    {
        var launcher = GetGameLaunchService();
        var mods = GetModService();

        var validation = launcher.ValidateGameDirectory();
        var enabledMods = await mods.GetEnabledModsAsync();
        var excludedSet = new HashSet<string>(mods.ExcludedMods, StringComparer.OrdinalIgnoreCase);
        EnabledModCount = enabledMods.Count(m => !excludedSet.Contains(m.Name));

        CanLaunch = validation.IsValid;
        StatusColor = validation.IsValid ? "#27AE60" : "#cc4125";

        if (!validation.IsValid)
        {
            StatusText = validation.Errors.Count > 0 ? validation.Errors[0] : "Not ready";
        }
        else
        {
            StatusText = "Ready to launch";
        }

        IsGameRunning = launcher.IsGameRunning;
    }

    public async Task LaunchGameAsync()
    {
        if (!CanLaunch)
            return;

        var launcher = GetGameLaunchService();
        var result = await launcher.LaunchGameAsync();

        if (result.IsSuccess)
        {
            StatusText = $"Game exited (code: {result.ExitCode})";
            StatusColor = "#27AE60";
        }
        else
        {
            StatusText = $"Launch failed: {result.Message}";
            StatusColor = "#cc4125";
        }

        IsGameRunning = launcher.IsGameRunning;
    }

    public async Task BrowseGameDirectoryAsync(string selectedPath)
    {
        if (!string.IsNullOrEmpty(selectedPath))
        {
            await _configService.SetGameDirectoryAsync(selectedPath);
            GameDirectory = selectedPath;
            await RefreshStatusAsync();
        }
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
