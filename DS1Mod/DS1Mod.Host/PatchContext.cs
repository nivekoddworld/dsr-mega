using DS1Mod.Core;

namespace DS1Mod.Host;

internal sealed class PatchContext : IPatchContext
{
    public string GameDir { get; }
    public string ModsDir { get; }

    public PatchContext(string gameDir, string modsDir)
    {
        GameDir = gameDir;
        ModsDir = modsDir;
    }

    public void BackupFile(string filePath)
    {
        string bak = filePath + ".bak";
        if (!File.Exists(bak) && File.Exists(filePath))
            File.Copy(filePath, bak);
    }

    public void Log(string message) =>
        Console.WriteLine($"[DS1Mod.Patch] {message}");
}
