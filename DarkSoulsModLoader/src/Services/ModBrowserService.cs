using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DarkSoulsModLoader.Models;
using System.IO.Compression;
using ModInfo = DarkSoulsModLoader.Models.ModInfo;

namespace DarkSoulsModLoader.Services;

public class ModBrowserService
{
    private readonly HttpClient _httpClient;
    private string _manifestUrl = "";

    public ModBrowserService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public void SetManifestUrl(string url)
    {
        _manifestUrl = url;
    }

    public async Task<ModManifest?> FetchManifestAsync()
    {
        if (string.IsNullOrEmpty(_manifestUrl))
            throw new InvalidOperationException("No manifest URL set");

        try
        {
            Console.WriteLine($"ModBrowserService: Fetching from {_manifestUrl}");
            var response = await _httpClient.GetStringAsync(_manifestUrl);
            Console.WriteLine($"ModBrowserService: Got response, deserializing...");
            var result = JsonSerializer.Deserialize<ModManifest>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"ModBrowserService: Deserialized {result?.Mods?.Count ?? 0} mods");
            return result;
        }
        catch (HttpRequestException hre)
        {
            throw new InvalidOperationException($"Network error: {hre.Message}", hre);
        }
        catch (TaskCanceledException tce)
        {
            throw new InvalidOperationException($"Request timeout after 5 seconds: {tce.Message}", tce);
        }
        catch (JsonException je)
        {
            throw new InvalidOperationException($"Invalid manifest format: {je.Message}", je);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch manifest: {ex.Message}", ex);
        }
    }

    public async Task<byte[]?> FetchImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            Console.WriteLine($"ModBrowserService: Fetching icon from {url}");
            var bytes = await _httpClient.GetByteArrayAsync(url);
            Console.WriteLine($"ModBrowserService: Icon fetched, {bytes.Length} bytes");
            return bytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ModBrowserService: Failed to fetch icon: {ex.Message}");
            return null;
        }
    }

    public async Task DownloadAndInstallModAsync(Models.ModInfo mod, string gameDirectory, Action<string>? onProgress = null)
    {
        if (string.IsNullOrEmpty(gameDirectory))
            throw new InvalidOperationException("Game directory not set");

        var modsDir = Path.Combine(gameDirectory, "mods");
        Directory.CreateDirectory(modsDir);

        try
        {
            onProgress?.Invoke($"Downloading {mod.Name}...");
            var modZipPath = Path.Combine(modsDir, $"{mod.Name}.zip.tmp");

            using (var response = await _httpClient.GetAsync(mod.DownloadUrl, HttpCompletionOption.ResponseContentRead))
            {
                response.EnsureSuccessStatusCode();
                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(modZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await contentStream.CopyToAsync(fileStream);
            }

            onProgress?.Invoke($"Extracting {mod.Name}...");
            // Extract directly into mods folder (zip should contain DLL at root level)
            ZipFile.ExtractToDirectory(modZipPath, modsDir, true);
            File.Delete(modZipPath);

            onProgress?.Invoke($"Installed {mod.Name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to install mod: {ex.Message}", ex);
        }
    }
}
