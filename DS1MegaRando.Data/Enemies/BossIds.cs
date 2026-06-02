namespace DS1MegaRando.Data.Enemies;

/// <summary>
/// Per-boss EMEVD patch: event ID + instruction (bank, id) pairs to remove.
/// Stripping these prevents intro animations/warps from referencing the
/// original model when a replacement boss is placed.
/// </summary>
public record EmevdPatch(long EventId, params (int Bank, int InstrId)[] Remove);

/// <summary>
/// A replaceable boss slot in the world.
/// </summary>
/// <param name="MapId">Map the boss lives in</param>
/// <param name="EntityId">MSB EntityID that identifies this boss part</param>
/// <param name="ModelId">Vanilla model ID (correct DSR model IDs from reference data)</param>
/// <param name="Name">Human-readable name</param>
/// <param name="CanReplace">False for multi-part/scripted bosses that must stay vanilla</param>
/// <param name="EmevdPatches">EMEVD events to patch when this slot gets a new model</param>
public record BossDef(
    string MapId,
    int EntityId,
    string ModelId,
    string Name,
    bool CanReplace = true,
    IReadOnlyList<EmevdPatch>? EmevdPatches = null);

public static class BossIds
{
    // Common instruction pairs stripped from boss intros:
    //   2003[18] = ForceAnimationPlayback  — plays model-specific entrance anim
    //   2004[41] = WarpCharacter           — teleports boss to scripted position
    //   2004[56] = MoveEntityToEntity      — used by some entrance sequences
    private static readonly (int, int) ForceAnim = (2003, 18);
    private static readonly (int, int) WarpChar  = (2004, 41);

    public static readonly IReadOnlyList<BossDef> All = new BossDef[]
    {
        // ── Undead Asylum ─────────────────────────────────────────────────────
        // c2232 has broken AI (IsIgnored) and requires a specific map/intro sequence.
        // CanReplace:false keeps it vanilla; the slot is still tracked so that if
        // another boss ends up here (e.g. via FreeForAll on a different run), we
        // know to strip its intro EMEVD.
        new("m18_01_00_00", 1801800, "c2232", "Asylum Demon",
            CanReplace: false,
            EmevdPatches: new[] { new EmevdPatch(11810310, ForceAnim, WarpChar) }),

        // ── Undead Parish ─────────────────────────────────────────────────────
        new("m10_01_00_00", 1010800, "c5350", "Bell Gargoyles",
            EmevdPatches: new[]
            {
                new EmevdPatch(11015382, ForceAnim, WarpChar),
                new EmevdPatch(11015396, ForceAnim, WarpChar),
            }),

        // ── The Depths ────────────────────────────────────────────────────────
        new("m10_00_00_00", 1000800, "c5260", "Gaping Dragon",
            EmevdPatches: new[] { new EmevdPatch(11005382, ForceAnim, WarpChar) }),

        // ── Undead Burg / Lower Undead Burg ──────────────────────────────────
        new("m10_02_00_00", 1002990, "c2250", "Taurus Demon",
            EmevdPatches: new[] { new EmevdPatch(11025300, ForceAnim, WarpChar) }),
        new("m10_02_00_00", 1002800, "c2240", "Capra Demon"),

        // ── Blighttown ────────────────────────────────────────────────────────
        new("m14_00_00_00", 1400800, "c5280", "Chaos Witch Quelaag"),

        // ── Painted World ────────────────────────────────────────────────────
        new("m11_00_00_00", 1100800, "c2730", "Crossbreed Priscilla"),

        // ── Darkroot Garden ──────────────────────────────────────────────────
        new("m12_00_00_01", 1200800, "c5210", "Sif the Great Wolf"),
        // Moonlight Butterfly: IsIgnored model — always kept vanilla
        new("m12_00_00_01", 1200850, "c3230", "Moonlight Butterfly", CanReplace: false),

        // ── Demon Ruins / Lost Izalith ────────────────────────────────────────
        new("m14_01_00_00", 1410800, "c5250", "Ceaseless Discharge", CanReplace: false),
        new("m14_01_00_00", 1410850, "c2231", "Demon Firesage"),
        new("m14_01_00_00", 1410900, "c5200", "Centipede Demon"),
        // Bed of Chaos is multi-part/scripted; never replace
        new("m14_01_00_00", 1410980, "c3230", "Bed of Chaos", CanReplace: false),

        // ── Tomb of the Giants ────────────────────────────────────────────────
        new("m13_00_00_00", 1300800, "c3320", "Pinwheel"),
        new("m13_01_00_00", 1310800, "c5220", "Gravelord Nito",
            EmevdPatches: new[] { new EmevdPatch(11315382, ForceAnim, WarpChar) }),

        // ── New Londo Ruins ───────────────────────────────────────────────────
        new("m16_00_00_00", 1600800, "c5390", "Four Kings"),

        // ── Sen's Fortress ────────────────────────────────────────────────────
        new("m15_00_00_00", 1500800, "c2320", "Iron Golem",
            EmevdPatches: new[] { new EmevdPatch(11505382, ForceAnim, WarpChar) }),

        // ── Anor Londo ────────────────────────────────────────────────────────
        new("m15_01_00_00", 1500900, "c5270", "Ornstein"),
        new("m15_01_00_00", 1500901, "c2360", "Smough"),
        // Gwyndolin: IsIgnored model — always kept vanilla
        new("m15_01_00_00", 1500980, "c5320", "Dark Sun Gwyndolin", CanReplace: false),

        // ── Duke's Archives ───────────────────────────────────────────────────
        // Seath: keep NpcParam from original so scripted crystal-prison death triggers correctly
        new("m17_00_00_00", 1700800, "c5290", "Seath the Scaleless",
            EmevdPatches: new[] { new EmevdPatch(11705396, ForceAnim, WarpChar) }),

        // ── Kiln of the First Flame ───────────────────────────────────────────
        new("m18_00_00_00", 1800800, "c5370", "Gwyn, Lord of Cinder"),

        // ── DLC: Oolacile ─────────────────────────────────────────────────────
        new("m12_01_00_00", 1210800, "c4100", "Knight Artorias"),
        new("m12_01_00_00", 1210850, "c4510", "Black Dragon Kalameet",
            EmevdPatches: new[] { new EmevdPatch(11215382, ForceAnim, WarpChar) }),
        new("m12_01_00_00", 1210900, "c4500", "Manus, Father of the Abyss"),
        new("m12_01_00_00", 1210950, "c3471", "Sanctuary Guardian"),
    };

    private static readonly IReadOnlyDictionary<int, BossDef> _byEntityId =
        All.ToDictionary(b => b.EntityId);

    public static BossDef? ByEntityId(int entityId) =>
        _byEntityId.TryGetValue(entityId, out var b) ? b : null;

    /// <summary>Bosses that can have their model replaced.</summary>
    public static IEnumerable<BossDef> Replaceable =>
        All.Where(b => b.CanReplace);
}
