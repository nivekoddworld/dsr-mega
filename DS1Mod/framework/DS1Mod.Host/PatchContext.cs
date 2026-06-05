using DS1Mod.Core;

namespace DS1Mod.Host;

internal sealed class PatchContext : IPatchContext
{
    public string GameDir { get; }
    public string ModsDir { get; }

    // Shared across all patchers in one session: selector → first mod that wrote it.
    private static readonly Dictionary<string, string> _globalEdits = new(StringComparer.OrdinalIgnoreCase);

    private string _currentMod = "(unknown)";

    public PatchContext(string gameDir, string modsDir)
    {
        GameDir = gameDir;
        ModsDir = modsDir;
    }

    /// <summary>Call before each patcher runs so conflict messages name the right mod.</summary>
    public void SetCurrentMod(string modName) => _currentMod = modName;

    public void BackupFile(string filePath)
    {
        string bak = filePath + ".bak";
        if (!File.Exists(bak) && File.Exists(filePath))
            File.Copy(filePath, bak);
    }

    public void Log(string message) =>
        Console.WriteLine($"[DS1Mod.Patch] {message}");

    public void RecordEdit(string filePath, string selector)
    {
        string key = $"{filePath}|{selector}";
        if (_globalEdits.TryGetValue(key, out string? prev))
        {
            // Same mod writing the same selector twice in its own Patch
            // is fine — DefineGoods is called once per item, EditBnd3Glob
            // iterates every locale's FMG bundle, etc. Only complain when
            // a DIFFERENT mod targets a selector that's already claimed.
            if (string.Equals(prev, _currentMod, StringComparison.Ordinal)) return;

            Console.WriteLine(
                $"[DS1Mod.Patch] CONFLICT: '{_currentMod}' and '{prev}' both write {selector} in {Path.GetFileName(filePath)}");
        }
        else
        {
            _globalEdits[key] = _currentMod;
        }
    }
}
