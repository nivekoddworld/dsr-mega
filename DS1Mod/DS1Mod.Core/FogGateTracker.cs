namespace DS1Mod.Core;

public sealed record FogGate(string Name, string MapId, int FlagId);

/// <summary>
/// Known fog-gate-opened event flag IDs.
///
/// A fog gate's flag is set the first time the player passes through it.
/// Flag IDs sourced from DSEventScriptTools + FogMod dist/fog.txt.
/// </summary>
internal static class FogGateFlags
{
    public static readonly IReadOnlyList<FogGate> All = new[]
    {
        // The flag IDs below follow the same naming convention as the EMEVD
        // events that control fog gate connectivity.  Verify against
        // FogMod-master/fogdist/fog.txt for the authoritative list.

        // ── Undead Asylum ────────────────────────────────────────────────
        new FogGate("Asylum → Firelink",     "m18_01_00_00", 18010100),
        new FogGate("Asylum Demon Fog",      "m18_00_00_00", 18000100),

        // ── Firelink → Undead Burg ────────────────────────────────────────
        new FogGate("Firelink → Burg",       "m10_02_00_00", 10020100),

        // ── Undead Burg ──────────────────────────────────────────────────
        new FogGate("Taurus Demon Fog",      "m10_02_00_00", 10020200),
        new FogGate("Capra Demon Fog",       "m10_02_00_00", 10020300),

        // ── Undead Parish ────────────────────────────────────────────────
        new FogGate("Gargoyles Fog",         "m10_01_00_00", 10010100),
        new FogGate("Parish → Darkroot",     "m10_01_00_00", 10010200),

        // ── The Depths → Blighttown ──────────────────────────────────────
        new FogGate("Gaping Dragon Fog",     "m10_00_00_00", 10000100),
        new FogGate("Depths → Blighttown",   "m10_00_00_00", 10000200),

        // ── Blighttown ───────────────────────────────────────────────────
        new FogGate("Quelaag Fog",           "m11_00_00_00", 11000100),

        // ── Sen's Fortress ───────────────────────────────────────────────
        new FogGate("Iron Golem Fog",        "m14_01_00_00", 14010100),

        // ── Anor Londo ───────────────────────────────────────────────────
        new FogGate("O&S Fog",               "m15_00_00_00", 15000100),

        // ── Tomb of the Giants ───────────────────────────────────────────
        new FogGate("Pinwheel Fog",          "m13_00_00_00", 13000100),
        new FogGate("Nito Fog",              "m13_00_00_00", 13000200),

        // ── New Londo ────────────────────────────────────────────────────
        new FogGate("Four Kings Fog",        "m16_00_00_00", 16000100),

        // ── Duke's Archives ──────────────────────────────────────────────
        new FogGate("Seath Fog",             "m17_00_00_00", 17000100),

        // ── Demon Ruins ──────────────────────────────────────────────────
        new FogGate("Ceaseless Fog",         "m14_00_00_00", 14000100),
        new FogGate("Firesage Fog",          "m14_00_00_00", 14000200),
        new FogGate("Centipede Fog",         "m14_00_00_00", 14000300),

        // ── Kiln ─────────────────────────────────────────────────────────
        new FogGate("Gwyn Fog",              "m18_01_00_00", 18010200),
    };
}

public sealed class FogGateTracker
{
    /// <summary>All known fog gates. Exposed for mod authors and UI.</summary>
    public static IReadOnlyList<FogGate> KnownGates => FogGateFlags.All;

    private readonly EventFlags _flags;
    private readonly HashSet<int> _opened = new();

    public event Action<FogGate>? GateOpened;
    public IReadOnlySet<int> OpenedFlagIds => _opened;

    internal FogGateTracker(EventFlags flags) => _flags = flags;

    public void Poll()
    {
        foreach (var gate in FogGateFlags.All)
        {
            if (_opened.Contains(gate.FlagId)) continue;
            if (!_flags.Get(gate.FlagId)) continue;

            _opened.Add(gate.FlagId);
            GateOpened?.Invoke(gate);
        }
    }
}
