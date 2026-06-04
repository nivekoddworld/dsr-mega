namespace DS1Mod.Core;

public sealed record BossKill(string BossName, int FlagId, DateTime KilledAt);

/// <summary>
/// Known event flag IDs for boss kills in DSR.
///
/// Sources: darksoulsdebug.wikidot.com/event-flags, DSEventScriptTools,
///          and cross-referenced with entity IDs in BossIds.cs.
///
/// Flag convention: killing a boss sets the flag for its battle event.
/// The flag ID is the event ID of the "end" condition in the EMEVD script.
/// </summary>
internal static class BossKillFlags
{
    public static readonly IReadOnlyDictionary<int, string> All =
        new Dictionary<int, string>
        {
            // ── Undead Asylum ────────────────────────────────────────────
            { 11010000, "Asylum Demon" },

            // ── Undead Parish ────────────────────────────────────────────
            { 11010100, "Bell Gargoyles" },

            // ── The Depths ───────────────────────────────────────────────
            { 11010200, "Gaping Dragon" },

            // ── Undead Burg ──────────────────────────────────────────────
            { 11010400, "Taurus Demon" },
            { 11010500, "Capra Demon" },

            // ── Blighttown ───────────────────────────────────────────────
            { 11010600, "Chaos Witch Quelaag" },

            // ── Painted World ────────────────────────────────────────────
            { 11510100, "Crossbreed Priscilla" },

            // ── Darkroot Garden ──────────────────────────────────────────
            { 11210000, "Sif the Great Wolf" },
            { 11210100, "Moonlight Butterfly" },

            // ── Demon Ruins ──────────────────────────────────────────────
            { 11410100, "Ceaseless Discharge" },
            { 11410200, "Demon Firesage" },
            { 11410300, "Centipede Demon" },

            // ── Tomb of the Giants ───────────────────────────────────────
            { 11310000, "Pinwheel" },
            { 11310100, "Gravelord Nito" },

            // ── New Londo ────────────────────────────────────────────────
            { 11600000, "Four Kings" },

            // ── Sen's Fortress ───────────────────────────────────────────
            { 11410000, "Iron Golem" },

            // ── Anor Londo ───────────────────────────────────────────────
            { 11500000, "Ornstein and Smough" },
            { 11500100, "Dark Sun Gwyndolin" },

            // ── Duke's Archives ──────────────────────────────────────────
            { 11700000, "Seath the Scaleless" },

            // ── Kiln of the First Flame ──────────────────────────────────
            { 11800000, "Gwyn, Lord of Cinder" },

            // ── DLC: Oolacile ────────────────────────────────────────────
            { 12000000, "Sanctuary Guardian" },
            { 12000100, "Knight Artorias" },
            { 12000200, "Manus, Father of the Abyss" },
            { 12000300, "Black Dragon Kalameet" },
        };
}

public sealed class BossTracker
{
    /// <summary>All known boss flag IDs and their names. Exposed for mod authors and UI.</summary>
    public static IReadOnlyDictionary<int, string> KnownBosses => BossKillFlags.All;

    private readonly EventFlags _flags;
    private readonly HashSet<int> _killed = new();

    public event Action<BossKill>? BossKilled;
    public IReadOnlyList<BossKill> KillHistory => _history;

    private readonly List<BossKill> _history = new();

    internal BossTracker(EventFlags flags) => _flags = flags;

    /// <summary>Poll all boss flags; fire BossKilled for any newly set ones.</summary>
    public void Poll()
    {
        foreach (var (flagId, name) in BossKillFlags.All)
        {
            if (_killed.Contains(flagId)) continue;
            if (!_flags.Get(flagId)) continue;

            _killed.Add(flagId);
            var kill = new BossKill(name, flagId, DateTime.UtcNow);
            _history.Add(kill);
            BossKilled?.Invoke(kill);
        }
    }
}
