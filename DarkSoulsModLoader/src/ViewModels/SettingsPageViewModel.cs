using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class SettingsPageViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private readonly GameLaunchService _gameLaunchService;
    private string _gameDirectory = "";
    private string _manifestUrl = "";

    public string GameDirectory
    {
        get => _gameDirectory;
        set => SetProperty(ref _gameDirectory, value);
    }

    public string ManifestUrl
    {
        get => _manifestUrl;
        set => SetProperty(ref _manifestUrl, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsPageViewModel(ConfigService configService, GameLaunchService gameLaunchService, ModService modService = null!)
    {
        _configService = configService;
        _gameLaunchService = gameLaunchService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        GameDirectory = await _configService.GetGameDirectoryAsync() ?? "Not set";
        ManifestUrl = await _configService.GetManifestUrlAsync() ?? "";
    }

    public async Task SetGameDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        await _configService.SetGameDirectoryAsync(path);
        GameDirectory = path;
    }

    public async Task SetManifestUrlAsync(string url)
    {
        await _configService.SetManifestUrlAsync(url);
        ManifestUrl = url;
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DarkSoulsModLoader");

            if (Directory.Exists(cacheDir))
            {
                var settingsDir = Path.Combine(cacheDir, "settings");
                if (Directory.Exists(settingsDir))
                    Directory.Delete(settingsDir, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Clear cache failed: {ex.Message}");
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
