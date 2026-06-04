namespace DS1Mod.Core.Data;

/// <summary>
/// DS1 boss-fog "entered" triggers. These are the only fog gates the game flags
/// on entry — when the player passes into one of these boss fogs the game sets
/// the trigger flag, which is what fires <c>FogGateEntered</c>. Regular
/// (non-boss) fog gates don't set a traversal flag in DS1, so they can't be
/// detected this way and are intentionally omitted.
///
/// Flags + names from the bundled FogMod data (dist/fog.txt, "BossTrigger").
/// </summary>
internal static class FogGateData
{
    public static readonly IReadOnlyList<FogGate> All = new[]
    {
        new FogGate("To Bell Gargoyles",     "m10_01_00_00", 1012996),
        new FogGate("To Moonlight Butterfly", "m12_00_00_00", 1202896),
        new FogGate("To Sanctuary Guardian",  "m12_01_00_00", 1212886),
        new FogGate("To Knight Artorias",     "m12_01_00_00", 1212896),
        new FogGate("To Chaos Witch Quelaag", "m14_00_00_00", 1402996),
        new FogGate("To Demon Firesage",      "m14_01_00_00", 1412416),
        new FogGate("To Centipede Demon",     "m14_01_00_00", 1412896),
        new FogGate("To Iron Golem",          "m15_00_00_00", 1502996),
        new FogGate("To Ornstein & Smough",   "m15_01_00_00", 1512996),
    };
}
