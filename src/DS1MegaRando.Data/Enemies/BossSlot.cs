namespace DS1MegaRando.Data.Enemies;

/// <summary>
/// Type-safe enumeration of all 32 boss slots that can be randomized.
/// Use these instead of indexing BossIds.All or searching by name.
/// </summary>
public enum BossSlot
{
    // ── Undead Asylum ─────────────────────────────────────────────────────
    AsylumDemon,
    StrayDemon,

    // ── Undead Parish ─────────────────────────────────────────────────────
    BellGargoyle1,
    BellGargoyle2,
    BellGargoyle3,

    // ── The Depths ────────────────────────────────────────────────────────
    GapingDragon,

    // ── Undead Burg / Lower Undead Burg ───────────────────────────────────
    TaurusDemon,
    CapraDemon,

    // ── Blighttown ────────────────────────────────────────────────────────
    ChaosWitchQuelaag,

    // ── Painted World ─────────────────────────────────────────────────────
    CrossbreedPriscilla,

    // ── Darkroot Garden ───────────────────────────────────────────────────
    SifTheGreatWolf,
    MoonlightButterfly,

    // ── Demon Ruins / Lost Izalith ────────────────────────────────────────
    CeaselessDischarge,
    DemonFiresage,
    CentipedeDemon,
    BedOfChaos,

    // ── Tomb of the Giants ────────────────────────────────────────────────
    Pinwheel,
    GravelordNito,

    // ── New Londo Ruins ───────────────────────────────────────────────────
    FourKings,

    // ── Sen's Fortress ────────────────────────────────────────────────────
    IronGolem,

    // ── Anor Londo ────────────────────────────────────────────────────────
    Ornstein,
    Smough,
    DarkSunGwyndolin,

    // ── Duke's Archives ───────────────────────────────────────────────────
    SeathTheScaleless,

    // ── Kiln of the First Flame ───────────────────────────────────────────
    GwynLordOfCinder,

    // ── DLC: Oolacile ─────────────────────────────────────────────────────
    SanctuaryGuardian,
    KnightArtorias,
    ManusFatherOfTheAbyss,
    BlackDragonKalameet,
}

/// <summary>
/// Helper methods for BossSlot enumeration.
/// </summary>
public static class BossSlotExtensions
{
    /// <summary>Get the BossDef for this slot.</summary>
    public static BossDef GetDef(this BossSlot slot) =>
        BossIds.All[(int)slot];

    /// <summary>Get the index of this slot in BossIds.All.</summary>
    public static int GetIndex(this BossSlot slot) =>
        (int)slot;

    /// <summary>Get the BossSlot for a given boss definition (or null if not found).</summary>
    public static BossSlot? FromDef(BossDef def)
    {
        int index = BossIds.All.IndexOf(def);
        return index >= 0 ? (BossSlot)index : null;
    }
}
