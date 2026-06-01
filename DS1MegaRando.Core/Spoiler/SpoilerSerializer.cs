using Newtonsoft.Json;

namespace DS1MegaRando.Core.Spoiler;

public static class SpoilerSerializer
{
    public static void WriteText(SpoilerLog log, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, log.FullText);
    }

    public static void WriteJson(SpoilerLog log, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        string json = JsonConvert.SerializeObject(log, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public static string DefaultTextPath(string outputDir, uint seed) =>
        Path.Combine(outputDir, $"spoiler_{seed:X8}.txt");
}
