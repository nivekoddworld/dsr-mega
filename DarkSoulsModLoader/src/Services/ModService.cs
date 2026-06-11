using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkSoulsModLoader.Services;

/// <summary>
/// Manages mod discovery, loading, and configuration.
/// Scans mods/ folder, reads mod metadata and configs from settings/.
/// </summary>
public class ModService
{
    private readonly string _modsDirectory;
    private readonly string _settingsDirectory;
    private readonly string _enabledModsFile;

    public ModService(string gameDirectory)
    {
        _modsDirectory = Path.Combine(gameDirectory, "mods");
        _settingsDirectory = Path.Combine(_modsDirectory, "settings");
        _enabledModsFile = Path.Combine(_settingsDirectory, ".enabled_mods");
    }

    /// <summary>
    /// Discover all installed mods from the mods/ directory.
    /// Returns mod metadata + current configuration.
    /// </summary>
    public async Task<List<ModInfo>> DiscoverModsAsync()
    {
        var mods = new List<ModInfo>();

        if (!Directory.Exists(_modsDirectory))
            return mods;

        var enabledMods = await LoadEnabledModsAsync();

        foreach (var dllFile in Directory.GetFiles(_modsDirectory, "*.dll").OrderBy(f => Path.GetFileName(f)))
        {
            var modName = Path.GetFileNameWithoutExtension(dllFile);
            var configPath = Path.Combine(_settingsDirectory, $"{modName}.json");

            var mod = new ModInfo
            {
                Name = modName,
                DllPath = dllFile,
                ConfigPath = configPath,
                IsEnabled = enabledMods.Contains(modName),
                Config = await LoadConfigAsync(configPath)
            };

            mods.Add(mod);
        }

        return mods;
    }

    /// <summary>
    /// Get a specific mod by name.
    /// </summary>
    public async Task<ModInfo?> GetModAsync(string modName)
    {
        var allMods = await DiscoverModsAsync();
        return allMods.FirstOrDefault(m => m.Name == modName);
    }

    /// <summary>
    /// Get only enabled mods.
    /// </summary>
    public async Task<List<ModInfo>> GetEnabledModsAsync()
    {
        var allMods = await DiscoverModsAsync();
        return allMods.Where(m => m.IsEnabled).ToList();
    }

    /// <summary>
    /// Load configuration from mods/settings/{modName}.json
    /// </summary>
    private async Task<Dictionary<string, object>?> LoadConfigAsync(string configPath)
    {
        if (!File.Exists(configPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load config {configPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save configuration to mods/settings/{modName}.json
    /// </summary>
    public async Task SaveConfigAsync(string modName, Dictionary<string, object> config)
    {
        Directory.CreateDirectory(_settingsDirectory);

        var configPath = Path.Combine(_settingsDirectory, $"{modName}.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// Enable or disable a mod.
    /// </summary>
    public async Task SetModEnabledAsync(string modName, bool enabled)
    {
        var enabledMods = await LoadEnabledModsAsync();

        if (enabled && !enabledMods.Contains(modName))
            enabledMods.Add(modName);
        else if (!enabled && enabledMods.Contains(modName))
            enabledMods.Remove(modName);

        await SaveEnabledModsAsync(enabledMods);
    }

    /// <summary>
    /// Load the list of enabled mod names.
    /// </summary>
    private async Task<List<string>> LoadEnabledModsAsync()
    {
        if (!File.Exists(_enabledModsFile))
            return new List<string>();

        try
        {
            var json = await File.ReadAllTextAsync(_enabledModsFile);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Save the list of enabled mod names.
    /// </summary>
    private async Task SaveEnabledModsAsync(List<string> enabledMods)
    {
        Directory.CreateDirectory(_settingsDirectory);
        var json = JsonSerializer.Serialize(enabledMods, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_enabledModsFile, json);
    }

    /// <summary>
    /// Delete a mod (remove DLL and config).
    /// </summary>
    public async Task DeleteModAsync(string modName)
    {
        var mod = await GetModAsync(modName);
        if (mod == null)
            return;

        if (File.Exists(mod.DllPath))
            File.Delete(mod.DllPath);

        if (File.Exists(mod.ConfigPath))
            File.Delete(mod.ConfigPath);

        await SetModEnabledAsync(modName, false);
    }
}

public record ModInfo
{
    public required string Name { get; init; }
    public required string DllPath { get; init; }
    public required string ConfigPath { get; init; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object>? Config { get; set; }
}
