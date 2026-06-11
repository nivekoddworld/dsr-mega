using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkSoulsModLoader.Services;

/// <summary>
/// Manages mod discovery, loading, and configuration.
/// Scans mods/ folder for .dll (enabled) and .disabled (disabled) files.
/// </summary>
public class ModService
{
    private readonly string _modsDirectory;
    private readonly string _settingsDirectory;
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), "mod_loader.log");

    private readonly string[] _excludedMods = new string[]
    {
        "DS1Mod.Core",
        "DS1Mod.SDK",
        "DS1Mod.Modding",
        "SoulsFormats"
    };

    public ModService(string gameDirectory)
    {
        _modsDirectory = Path.Combine(gameDirectory, "mods");
        _settingsDirectory = Path.Combine(_modsDirectory, "settings");
    }

    public string[] ExcludedMods => _excludedMods;

    private void Log(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(_logPath, $"[{timestamp}] {message}\n");
        }
        catch { }
    }

    /// <summary>
    /// Discover all installed mods from the mods/ directory.
    /// Returns mod metadata + current configuration.
    /// Enabled mods have .dll extension, disabled have .disabled.
    /// </summary>
    public async Task<List<ModInfo>> DiscoverModsAsync()
    {
        var mods = new List<ModInfo>();

        if (!Directory.Exists(_modsDirectory))
        {
            Log($"Mods directory not found: {_modsDirectory}");
            return mods;
        }

        // Get all .dll and .disabled files
        var allModFiles = Directory.GetFiles(_modsDirectory, "*.dll")
            .Concat(Directory.GetFiles(_modsDirectory, "*.disabled"))
            .OrderBy(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        Log($"Found {allModFiles.Count} mod files in {_modsDirectory}");
        foreach (var file in allModFiles)
            Log($"  - {Path.GetFileName(file)}");

        foreach (var modFile in allModFiles)
        {
            var fileName = Path.GetFileName(modFile);
            var modName = Path.GetFileNameWithoutExtension(modFile);
            var isEnabled = modFile.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
            var configPath = Path.Combine(_settingsDirectory, $"{modName}.json");

            Log($"Discovered mod '{modName}' (enabled={isEnabled}) at {modFile}");

            var mod = new ModInfo
            {
                Name = modName,
                DllPath = modFile,
                ConfigPath = configPath,
                IsEnabled = isEnabled,
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
            Log($"Failed to load config {configPath}: {ex.Message}");
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
    /// Enable or disable a mod by renaming its file extension.
    /// Enabled mods have .dll extension, disabled have .disabled extension.
    /// </summary>
    public async Task SetModEnabledAsync(string modName, bool enabled)
    {
        var dllFile = Path.Combine(_modsDirectory, $"{modName}.dll");
        var disabledFile = Path.Combine(_modsDirectory, $"{modName}.disabled");

        var existingFile = File.Exists(dllFile) ? dllFile :
                          File.Exists(disabledFile) ? disabledFile : null;

        Log($"SetModEnabledAsync: {modName}, enabled={enabled}, existing={Path.GetFileName(existingFile ?? "none")}");

        if (existingFile == null)
        {
            Log($"SetModEnabledAsync: {modName} not found");
            return;
        }

        var targetFile = enabled ? dllFile : disabledFile;

        if (existingFile != targetFile)
        {
            try
            {
                if (File.Exists(targetFile))
                    File.Delete(targetFile);

                File.Move(existingFile, targetFile, overwrite: true);
                Log($"SetModEnabledAsync: Renamed {Path.GetFileName(existingFile)} -> {Path.GetFileName(targetFile)}");
            }
            catch (Exception ex)
            {
                Log($"SetModEnabledAsync: Failed to rename {modName}: {ex.Message}");
            }
        }

        await Task.CompletedTask;
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
