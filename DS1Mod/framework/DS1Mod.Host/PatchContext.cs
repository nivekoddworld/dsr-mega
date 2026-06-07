using DS1Mod.Core;
using DS1Mod.Modding;
using SoulsFormats;

namespace DS1Mod.Host;

internal sealed class PatchContext : IPatchContext
{
    public string GameDir { get; }
    public string ModsDir { get; }

    // Shared across all patchers in one session: selector → first mod that wrote it.
    private static readonly Dictionary<string, string> _globalEdits = new(StringComparer.OrdinalIgnoreCase);

    private string _currentMod = "(unknown)";
    private readonly IdAllocator _allocator;

    // Shared bonfire ESD that all mods can edit during the patch phase
    internal EsdEditor? BonfireEsd { get; set; }
    internal int BonfireEsdSize { get; set; }          // Detected size of bonfire ESD entries on disk
    internal int NextBonfireSlot { get; set; } = 13;  // Vanilla items use slots 1-12

    public PatchContext(string gameDir, string modsDir)
    {
        GameDir = gameDir;
        ModsDir = modsDir;
        _allocator = new IdAllocator(gameDir);
    }

    /// <summary>Call before each patcher runs so conflict messages name the right mod.</summary>
    public void SetCurrentMod(string modName)
    {
        _currentMod = modName;
        _allocator.SetCurrentMod(modName);
    }

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

    public int AllocateIds(string space, int count) => _allocator.AllocateIds(space, count);

    public int AllocateBonfireSlot() => NextBonfireSlot++;

    public EsdEditor? GetBonfireEsd() => BonfireEsd;

    // Explicit implementation so IPatchContext callers (typed as the interface)
    // get the real EsdEditor object rather than the default null.
    object? IPatchContext.GetBonfireEsd() => BonfireEsd;
}
