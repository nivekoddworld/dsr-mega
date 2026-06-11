using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DarkSoulsModLoader.Services;

/// <summary>
/// Initializes mod configuration files by loading mods briefly to call InitializeConfig.
/// </summary>
public class ModConfigLoaderService
{
    private readonly ModService _modService;

    public ModConfigLoaderService(ModService modService)
    {
        _modService = modService;
    }

    /// <summary>
    /// Initialize config files for all mods.
    /// For new configs, creates them with defaults.
    /// For existing configs, merges in any new settings from InitializeConfig.
    /// </summary>
    public async Task InitializeModConfigFilesAsync()
    {
        var allMods = await _modService.DiscoverModsAsync();

        foreach (var mod in allMods)
        {
            try
            {
                // Use the same config path as ModService: mods/settings/{modName}.json
                var configPath = mod.ConfigPath;

                // Always initialize (creates new or updates existing with new settings)
                await InitializeModConfigAsync(mod.DllPath, mod.Name, configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize config for mod {mod.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load a mod DLL briefly to call its InitializeConfig method.
    /// </summary>
    private async Task InitializeModConfigAsync(string dllPath, string modName, string configPath)
    {
        if (!File.Exists(dllPath))
            return;

        try
        {
            // Load assembly from DLL
            var assembly = Assembly.LoadFrom(dllPath);
            var modBaseType = Type.GetType("DS1Mod.SDK.ModBase, DS1Mod.SDK");

            if (modBaseType == null)
                return;

            // Find class that inherits from ModBase
            var modType = assembly.GetTypes()
                .FirstOrDefault(t => modBaseType.IsAssignableFrom(t) && t != modBaseType && !t.IsAbstract);

            if (modType == null)
                return;

            // Instantiate and call InitializeConfig
            try
            {
                var instance = Activator.CreateInstance(modType);
                if (instance != null)
                {
                    // Load ModConfig type from SDK
                    var modConfigType = Type.GetType("DS1Mod.SDK.ModConfig, DS1Mod.SDK");
                    if (modConfigType == null)
                        return;

                    // Load existing config or create new one
                    var loadMethod = modConfigType.GetMethod("LoadAsync",
                        BindingFlags.Public | BindingFlags.Static);
                    if (loadMethod == null)
                        return;

                    Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? "");
                    var loadTask = (Task)loadMethod.Invoke(null, new object[] { modName, configPath });
                    await loadTask;

                    // Get the result from the Task<ModConfig>
                    var resultProperty = loadTask.GetType().GetProperty("Result");
                    if (resultProperty == null)
                        return;
                    var config = resultProperty.GetValue(loadTask);

                    // Call InitializeConfig method to add any missing settings
                    var method = modType.GetMethod("InitializeConfig",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { modConfigType },
                        null);

                    if (method != null && config != null)
                    {
                        method.Invoke(instance, new[] { config });
                    }

                    // Save the config (with merged old + new settings)
                    var saveMethod = modConfigType.GetMethod("SaveAsync",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (saveMethod != null && config != null)
                    {
                        // Call SaveAsync
                        var task = (Task)saveMethod.Invoke(config, null);
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to instantiate/initialize mod: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load mod from {dllPath}: {ex.Message}");
        }
    }
}
