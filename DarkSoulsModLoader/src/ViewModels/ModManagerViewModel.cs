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
public class ModManagerViewModel
{
    private readonly ModService _modService;

    public ObservableCollection<ModItemViewModel> Mods { get; } = new();

    public ModManagerViewModel(ModService modService)
    {
        _modService = modService;
    }

    public async Task LoadModsAsync()
    {
        var discoveredMods = await _modService.DiscoverModsAsync();

        Mods.Clear();
        foreach (var mod in discoveredMods.OrderBy(m => m.Name))
        {
            Mods.Add(new ModItemViewModel(mod, _modService));
        }
    }

    public async Task RefreshAsync()
    {
        await LoadModsAsync();
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
