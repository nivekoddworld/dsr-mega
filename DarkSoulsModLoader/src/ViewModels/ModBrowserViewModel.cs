using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DarkSoulsModLoader.Models;
using DarkSoulsModLoader.Services;
using ModInfo = DarkSoulsModLoader.Models.ModInfo;

namespace DarkSoulsModLoader.ViewModels;

public class ModBrowserViewModel : INotifyPropertyChanged
{
    private readonly ModBrowserService _modBrowserService;
    private readonly ConfigService _configService;
    private ObservableCollection<ModBrowserItemViewModel> _availableMods = new();
    private string _statusText = "Enter manifest URL in settings";
    private bool _isLoading = false;

    public ObservableCollection<ModBrowserItemViewModel> AvailableMods
    {
        get => _availableMods;
        set => SetProperty(ref _availableMods, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModBrowserViewModel(ModBrowserService modBrowserService, ConfigService configService)
    {
        _modBrowserService = modBrowserService;
        _configService = configService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var manifestUrl = await _configService.GetManifestUrlAsync();
        if (!string.IsNullOrEmpty(manifestUrl))
        {
            _modBrowserService.SetManifestUrl(manifestUrl);
            await RefreshModsAsync();
        }
    }

    public async Task RefreshModsAsync()
    {
        IsLoading = true;
        StatusText = "Fetching mods...";
        AvailableMods.Clear();

        try
        {
            var manifest = await _modBrowserService.FetchManifestAsync();
            if (manifest?.Mods == null || manifest.Mods.Count == 0)
            {
                StatusText = "No mods found in manifest.";
                IsLoading = false;
                return;
            }

            foreach (var mod in manifest.Mods)
            {
                var item = new ModBrowserItemViewModel(mod, _modBrowserService, _configService);
                AvailableMods.Add(item);

                // Load icon async
                if (!string.IsNullOrEmpty(mod.IconUrl))
                {
                    _ = item.LoadIconAsync();
                }
            }

            StatusText = $"Found {AvailableMods.Count} mods";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            Console.WriteLine($"ModBrowserViewModel: {ex}");
        }

        IsLoading = false;
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

public class ModBrowserItemViewModel : INotifyPropertyChanged
{
    private readonly Models.ModInfo _modInfo;
    private readonly ModBrowserService _modBrowserService;
    private readonly ConfigService _configService;
    private Bitmap? _icon;
    private string _installStatus = "Download";
    private bool _isInstalling = false;

    public Models.ModInfo ModInfo => _modInfo;

    public Bitmap? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public string InstallStatus
    {
        get => _installStatus;
        set => SetProperty(ref _installStatus, value);
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        set => SetProperty(ref _isInstalling, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModBrowserItemViewModel(Models.ModInfo modInfo, ModBrowserService modBrowserService, ConfigService configService)
    {
        _modInfo = modInfo;
        _modBrowserService = modBrowserService;
        _configService = configService;
    }

    public async Task LoadIconAsync()
    {
        if (string.IsNullOrEmpty(_modInfo.IconUrl))
            return;

        try
        {
            var imageData = await _modBrowserService.FetchImageAsync(_modInfo.IconUrl);
            if (imageData != null)
            {
                using var memStream = new System.IO.MemoryStream(imageData);
                Icon = new Bitmap(memStream);
            }
        }
        catch
        {
            // Icon load failed, continue without it
        }
    }

    public async Task InstallAsync()
    {
        IsInstalling = true;
        InstallStatus = "Installing...";

        try
        {
            var gameDir = await _configService.GetGameDirectoryAsync();
            if (string.IsNullOrEmpty(gameDir))
            {
                InstallStatus = "Set game directory first";
                IsInstalling = false;
                return;
            }

            await _modBrowserService.DownloadAndInstallModAsync(_modInfo, gameDir,
                status => InstallStatus = status);

            InstallStatus = "Installed ✓";
        }
        catch (Exception ex)
        {
            InstallStatus = $"Failed: {ex.Message}";
        }

        IsInstalling = false;
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
