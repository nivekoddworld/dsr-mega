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
    //   2004[12] = SetImmortality          — Seath's crystal-prison immortality flag
    //   2004[22] = CreateMultipartNpc      — Seath's tail part (invisible on other models)
    private static readonly (int, int) ForceAnim      = (2003, 18);
    private static readonly (int, int) WarpChar       = (2004, 41);
    private static readonly (int, int) SetImmortal    = (2004, 12);
    private static readonly (int, int) CreateMultipart= (2004, 22);
    private static readonly (int, int) ForceAnimDsrA  = (2003, 44);
    private static readonly (int, int) ForceAnimDsrB  = (2003, 46);
    private static readonly (int, int) WarpAndFloor   = (2004, 40);
    private static readonly (int, int) WarpCopyFloor  = (2004, 42);
    private static readonly (int, int) SetNpcPart     = (2004, 22);

    public static readonly IReadOnlyList<BossDef> All = new BossDef[]
    {
        // ── Undead Asylum ─────────────────────────────────────────────────────
        // c2232 has IsIgnored AI, so it behaves oddly if placed as a replacement
        // elsewhere — but its own slot is replaceable.  Use boss_overrides.json
        // "positions" to fine-tune the spawn point if a replacement clips geometry.
        new("m18_01_00_00", 1810800, "c2232", "Asylum Demon",
            CanReplace: true,
            EmevdPatches: new[] { new EmevdPatch(11810310, ForceAnim, WarpChar) }),
        new("m18_01_00_00", 1810810, "c2230", "Stray Demon"),

        // ── Undead Parish ─────────────────────────────────────────────────────
        new("m10_01_00_00", 1010800, "c5350", "Bell Gargoyle 1",
            EmevdPatches: new[]
            {
                new EmevdPatch(11015382, ForceAnim, WarpChar),
                new EmevdPatch(11015396, ForceAnim, WarpChar),
            }),
        // Gargoyle 2 (mid-fight drop-in) and Gargoyle 3 (a scripted reserve instance)
        // both use model-specific jump-down navigation; non-gargoyle models cannot perform
        // that entry, so both are kept vanilla.
        new("m10_01_00_00", 1010801, "c5350", "Bell Gargoyle 2", CanReplace: false),
        new("m10_01_00_00", 1010802, "c5350", "Bell Gargoyle 3", CanReplace: false),

        // ── The Depths ────────────────────────────────────────────────────────
        new("m10_00_00_00", 1000800, "c5260", "Gaping Dragon",
            EmevdPatches: new[] { new EmevdPatch(11005382, ForceAnim, WarpChar) }),

        // ── Undead Burg / Lower Undead Burg ──────────────────────────────────
        // Both entities live in m10_01_00_00 (entity IDs 1010xxx confirmed via fog.txt)
        new("m10_01_00_00", 1010700, "c2250", "Taurus Demon"),
        new("m10_01_00_00", 1010750, "c2240", "Capra Demon"),

        // ── Blighttown ────────────────────────────────────────────────────────
        new("m14_00_00_00", 1400800, "c5280", "Chaos Witch Quelaag",
            EmevdPatches: new[] { new EmevdPatch(11405396, ForceAnim, CreateMultipart), new EmevdPatch(11405397, ForceAnim, CreateMultipart) }),

        // ── Painted World ────────────────────────────────────────────────────
        new("m11_00_00_00", 1100160, "c2730", "Crossbreed Priscilla",
            EmevdPatches: new[] { new EmevdPatch(11105396, ForceAnim, CreateMultipart), new EmevdPatch(11105397, WarpChar) }),

        // ── Darkroot Garden ──────────────────────────────────────────────────
        new("m12_00_00_01", 1200800, "c5210", "Sif the Great Wolf",
            EmevdPatches: new[] { new EmevdPatch(11200000, WarpChar) }),
        // Moonlight Butterfly: IsIgnored in EnemyIds so it won't appear as a replacement
        // in other arenas, but its own slot is randomizable.
        new("m12_00_00_01", 1200801, "c3230", "Moonlight Butterfly"),

        // ── Demon Ruins / Lost Izalith ────────────────────────────────────────
        new("m14_01_00_00", 1410600, "c5250", "Ceaseless Discharge",
            EmevdPatches: new[] { new EmevdPatch(11415376, ForceAnim, WarpChar) }),
        new("m14_01_00_00", 1410400, "c2231", "Demon Firesage"),
        new("m14_01_00_00", 1410700, "c5200", "Centipede Demon",
            EmevdPatches: new[] { new EmevdPatch(11415350, CreateMultipart), new EmevdPatch(11415360, CreateMultipart) }),
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
        new("m15_01_00_00", 1510800, "c5270", "Ornstein",
            EmevdPatches: new[] { new EmevdPatch(11515397, SetImmortal) }),
        new("m15_01_00_00", 1510810, "c2360", "Smough",
            EmevdPatches: new[] { new EmevdPatch(11515397, SetImmortal), new EmevdPatch(11515398, CreateMultipart), new EmevdPatch(11515399, CreateMultipart) }),
        // Gwyndolin: IsIgnored in EnemyIds so it won't appear as a replacement elsewhere,
        // but its own slot is randomizable.
        new("m15_01_00_00", 1510650, "c5320", "Dark Sun Gwyndolin"),

        // ── Duke's Archives ───────────────────────────────────────────────────
        // 11705396: strip SetImmortality so the replacement isn't locked unkillable
        //   during the crystal-prison sequence (that flag is Seath-specific).
        // 11705397: strip CreateMultipartNpc to prevent an invisible indestructible
        //   tail part being spawned on the replacement model.
        new("m17_00_00_00", 1700800, "c5290", "Seath the Scaleless",
            EmevdPatches: new[]
            {
                new EmevdPatch(11705396, SetImmortal),
                new EmevdPatch(11705397, CreateMultipart),
            }),

        // ── Kiln of the First Flame ───────────────────────────────────────────
        new("m18_00_00_00", 1800800, "c5370", "Gwyn, Lord of Cinder"),

        // ── DLC: Oolacile ─────────────────────────────────────────────────────
        new("m12_01_00_00", 1210800, "c3471", "Sanctuary Guardian"),
        new("m12_01_00_00", 1210820, "c4100", "Knight Artorias"),
        new("m12_01_00_00", 1210840, "c4500", "Manus, Father of the Abyss"),
        new("m12_01_00_00", 1210400, "c4510", "Black Dragon Kalameet",
            EmevdPatches: new[] { new EmevdPatch(11215382, ForceAnim, WarpChar) }),
    };

    private static readonly IReadOnlyDictionary<int, BossDef> _byEntityId =
        All.ToDictionary(b => b.EntityId);

    public static BossDef? ByEntityId(int entityId) =>
        _byEntityId.TryGetValue(entityId, out var b) ? b : null;

    /// <summary>Bosses that can have their model replaced.</summary>
    public static IEnumerable<BossDef> Replaceable =>
        All.Where(b => b.CanReplace);
}
