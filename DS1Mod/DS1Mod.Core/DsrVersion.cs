using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// DSR shifts some player-character (ChrIns) field offsets between patches.
/// DSR-Gadget keys these "boosts" off the module's image size; we replicate
/// that exactly. Unknown (newer) sizes fall through to version 100, which gets
/// the latest (1.03+) boosts — the right default for the current Steam build.
/// </summary>
internal static class DsrVersion
{
    private static bool _initialized;

    public static int ChrClassWarpBoost { get; private set; }
    public static int ChrData1Boost1    { get; private set; }   // ChrMapData, flags
    public static int ChrData1Boost2    { get; private set; }   // HP, stamina
    public static int ModuleSize        { get; private set; }
    public static int Version           { get; private set; } = -1;

    private static readonly Dictionary<int, int> Versions = new()
    {
        [0x4869400] = 0,   // 1.01
        [0x496BE00] = 1,   // 1.01.1
        [0x37CB400] = 2,   // 1.01.2
        [0x3817800] = 3,   // 1.03
    };

    public static void Ensure()
    {
        if (_initialized) return;

        ModuleSize = GameMemory.ModuleSize;
        if (ModuleSize == 0) return;   // module not ready yet; try again next poll
        _initialized = true;

        Version = Versions.TryGetValue(ModuleSize, out int v) ? v : 100;

        if (Version > 1) ChrClassWarpBoost = 0x10;
        if (Version > 2) { ChrData1Boost1 = 0x20; ChrData1Boost2 = 0x10; }
    }

    public static string Describe()
    {
        Ensure();
        return $"moduleSize=0x{ModuleSize:X} version={Version} " +
               $"boost1=0x{ChrData1Boost1:X} boost2=0x{ChrData1Boost2:X}";
    }
}
