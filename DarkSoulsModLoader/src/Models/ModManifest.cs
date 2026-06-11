using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DarkSoulsModLoader.Models;

public class ModManifest
{
    [JsonPropertyName("mods")]
    public List<ModInfo> Mods { get; set; } = new();
}

public class ModInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";
}
