using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

/// <summary>
/// ViewModel for the mod browser/manager page.
/// Exposes discovered mods, handles enable/disable, configuration.
/// </summary>
public class ModManagerViewModel : INotifyPropertyChanged
{
    private readonly ModService _modService;
    private ModItemViewModel? _selectedMod;
    private ConfigEditorViewModel? _configEditor;
    private bool _showConfigEditor;

    public ObservableCollection<ModItemViewModel> Mods { get; } = new();

    public ModItemViewModel? SelectedMod
    {
        get => _selectedMod;
        set
        {
            if (SetProperty(ref _selectedMod, value))
            {
                if (value != null)
                {
                    ShowConfigEditor = true;
                    _ = _configEditor?.LoadConfigAsync(value.Name);
                }
            }
        }
    }

    public ConfigEditorViewModel? ConfigEditor
    {
        get => _configEditor;
        set => SetProperty(ref _configEditor, value);
    }

    public bool ShowConfigEditor
    {
        get => _showConfigEditor;
        set => SetProperty(ref _showConfigEditor, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModManagerViewModel(ModService modService, ConfigService configService = null!)
    {
        _modService = modService;
        _configEditor = new ConfigEditorViewModel(modService);
    }

    public async Task LoadModsAsync()
    {
        var discoveredMods = await _modService.DiscoverModsAsync();
        var excludedSet = new HashSet<string>(_modService.ExcludedMods, StringComparer.OrdinalIgnoreCase);

        Mods.Clear();
        foreach (var mod in discoveredMods.OrderBy(m => m.Name))
        {
            if (!excludedSet.Contains(mod.Name))
            {
                Mods.Add(new ModItemViewModel(mod, _modService));
            }
        }
    }

    public async Task RefreshAsync()
    {
        await LoadModsAsync();
    }

    public async Task UninstallModAsync(string modName)
    {
        try
        {
            await _modService.DeleteModAsync(modName);
            await LoadModsAsync();
            ShowConfigEditor = false;
            SelectedMod = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to uninstall mod: {ex.Message}");
        }
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        return false;
    }
}

/// <summary>
/// ViewModel for an individual mod item in the list.
/// </summary>
public class ModItemViewModel : INotifyPropertyChanged
{
    private readonly ModService _modService;
    private ModInfo _modInfo;
    private bool _isEnabled;

    public string Name => _modInfo.Name;
    public string DllPath => _modInfo.DllPath;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                _modInfo.IsEnabled = value;
                OnPropertyChanged();
                _ = _modService.SetModEnabledAsync(Name, value);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModItemViewModel(ModInfo modInfo, ModService modService)
    {
        _modInfo = modInfo;
        _isEnabled = modInfo.IsEnabled;
        _modService = modService;
    }

    public async Task SaveConfigAsync(Dictionary<string, object> config)
    {
        await _modService.SaveConfigAsync(Name, config);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
