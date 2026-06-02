using Newtonsoft.Json;

namespace DS1MegaRando.Core.Settings;

public static class SettingsSerializer
{
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
    };

    public static void Save(MegaSettings settings, string path)
    {
        string json = JsonConvert.SerializeObject(settings, _jsonSettings);
        File.WriteAllText(path, json);
    }

    public static MegaSettings Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<MegaSettings>(json, _jsonSettings) ?? new MegaSettings();
    }

    public static string ToJson(MegaSettings settings) =>
        JsonConvert.SerializeObject(settings, _jsonSettings);

    public static MegaSettings FromJson(string json) =>
        JsonConvert.DeserializeObject<MegaSettings>(json, _jsonSettings) ?? new MegaSettings();
}
