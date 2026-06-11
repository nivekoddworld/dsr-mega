using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkSoulsModLoader.Services;

/// <summary>
/// Manages application-level configuration (game directory, etc).
/// Stores settings in AppData/DarkSoulsModLoader/config.json
/// </summary>
public class ConfigService
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;

    public ConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configDirectory = Path.Combine(appDataPath, "DarkSoulsModLoader");
        _configFilePath = Path.Combine(_configDirectory, "config.json");
    }

    public async Task InitializeAsync()
    {
        await AutoDetectGameDirectoryAsync();
    }

    private async Task AutoDetectGameDirectoryAsync()
    {
        var config = await LoadConfigAsync();
        if (string.IsNullOrEmpty(config.GameDirectory))
        {
            var detectedPath = GameLaunchService.FindDarkSoulsRemastered();
            if (!string.IsNullOrEmpty(detectedPath))
            {
                config.GameDirectory = detectedPath;
                await SaveConfigAsync(config);
            }
        }
    }

    /// <summary>
    /// Load the app configuration from disk.
    /// </summary>
    public async Task<AppConfig> LoadConfigAsync()
    {
        if (!File.Exists(_configFilePath))
            return new AppConfig();

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load config: {ex.Message}");
            return new AppConfig();
        }
    }

    /// <summary>
    /// Save the app configuration to disk.
    /// </summary>
    public async Task SaveConfigAsync(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(_configDirectory);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save config: {ex.Message}");
        }
    }

    /// <summary>
    /// Set the game directory and save config.
    /// </summary>
    public async Task SetGameDirectoryAsync(string gameDirectory)
    {
        var config = await LoadConfigAsync();
        config.GameDirectory = gameDirectory;
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Get the configured game directory.
    /// </summary>
    public async Task<string?> GetGameDirectoryAsync()
    {
        var config = await LoadConfigAsync();
        return config.GameDirectory;
    }

    /// <summary>
    /// Set the manifest URL and save config.
    /// </summary>
    public async Task SetManifestUrlAsync(string manifestUrl)
    {
        var config = await LoadConfigAsync();
        config.ManifestUrl = manifestUrl;
        await SaveConfigAsync(config);
    }

    /// <summary>
    /// Get the configured manifest URL.
    /// </summary>
    public async Task<string?> GetManifestUrlAsync()
    {
        var config = await LoadConfigAsync();
        return config.ManifestUrl;
    }
}

public class AppConfig
{
    public string? GameDirectory { get; set; }
    public string? ManifestUrl { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;

    public bool IsValid => !string.IsNullOrEmpty(GameDirectory) && Directory.Exists(GameDirectory);
}
