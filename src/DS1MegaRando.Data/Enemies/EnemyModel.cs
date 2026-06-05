namespace DS1MegaRando.Data.Enemies;

/// <summary>
/// Type-safe enumeration of all playable enemy models in DS1 Remastered.
/// Use these instead of magic string model IDs (c0001, c2500, etc).
/// Enumeration order matches EnemyIds.All for index-based lookup.
/// </summary>
public enum EnemyModel
{
    // ── Rats ─────────────────────────────────────────────────────────────
    Rat,
    SmallRat,
    LargeRat,
    SnowRat,

    // ── Hollows / Undead ─────────────────────────────────────────────────
    Hollow,
    UndeadAssassin,
    BlowdartSniper,
    ArmoredHollow,
    UndeadSoldier,
    BalderKnight,
    BerenekeKnight,
    Necromancer,
    Butcher,
    UndeadCrystalSoldier,
    EngorgedZombie,

    // ── Knights / Elites ─────────────────────────────────────────────────
    Darkwraith,
    PaintingGuardian,
    SilverKnight,
    SerpentSoldier,
    SerpentMage,
    BlackKnight,

    // ── Skeletons ─────────────────────────────────────────────────────────
    Skeleton,
    GiantSkeleton,
    BonewheelSkeleton,
    SkeletonBeast,
    BoneTower,

    // ── Beasts / Creatures ────────────────────────────────────────────────
    InfestedGhoul,
    BatwingDemon,
    CrowDemon,
    DemonicFoliage,
    Channeler,
    GiantStoneKnight,
    DemonicStatue,
    CrystalGolem,
    GoldenCrystalGolem,
    InfestedBarbarian,
    InfestedBarbarianBoulder,
    Phalanx,
    Giant,
    Sentinel,
    GiantMosquito,
    Slime,
    EggCarrier,
    VileMaggot,
    ChaosEater,
    ManEaterShell,
    Basilisk,
    UndeadAttackDog,
    FlamingAttackDog,
    PossessedTree,
    TreeLizard,
    GiantLeech,
    CragSpider,
    FrogRay,
    ArmoredTusk,
    ArmoredTuskDukes,
    EvilVagrant,
    MassOfSouls,
    Drake,
    StoneGuardian,
    Scarecrow,
    Bloathead,
    BloatheadSorcerer,
    HumanityPhantom,
    HumanityPhantomM,
    HumanityPhantomL,
    ChainedPrisoner,
    DlcDog,
    GreatFeline,
    LightningGargoyle,

    // ── Mushrooms ─────────────────────────────────────────────────────────
    MushroomParent,
    MushroomChild,

    // ── Boss-Capable enemies (Type=1 in reference data) ───────────────────
    StrayDemon,
    DemonFiresage,
    CapraDemon,
    TaurusDemon,
    IronGolem,
    Smough,
    CrossbreedPriscilla,
    Pinwheel,
    SanctuaryGuardian,
    KnightArtorias,
    Manus,
    BlackDragonKalameet,
    CentipedeDemon,
    Sif,
    GravelordNito,
    GapingDragon,
    Ornstein,
    SuperOrnstein,
    ChaosWitchQuelaag,
    SeathTheScaleless,
    BellGargoyle,
    Gwyn,
    FourKings,

    // ── IsIgnored — broken AI or scripted; never place as replacement ─────
    AsylumDemon,
    TitaniteDemon,
    Ghost,
    LightningGhost,
    Mimic,
    BlackKnightVar1,
    BlackKnightVar2,
    BlackKnightVar3,
    SkeletonBaby,
    MoonlightButterfly,
    CrystalLizard,
    Pisaca,
    BurrowingRockworm,
    UndeadDragon,
    BoundingDemon,
    HellkiteDrake,
    ChaosBug,
    GoodVagrant,
    Hydra,
    CentipedeArm,
    CentipedeTail,
    ParasiticWallHugger,
    CeaselessDischarge,
    Gwyndolin,
}

/// <summary>
/// Helper methods for EnemyModel enumeration.
/// </summary>
public static class EnemyModelExtensions
{
    /// <summary>Get the EnemyDef for this model.</summary>
    public static EnemyDef GetDef(this EnemyModel model) =>
        EnemyIds.All[(int)model];

    /// <summary>Get the index of this model in EnemyIds.All.</summary>
    public static int GetIndex(this EnemyModel model) =>
        (int)model;

    /// <summary>Get the model ID string (e.g. "c2500") for this model.</summary>
    public static string GetModelId(this EnemyModel model) =>
        model.GetDef().ModelId;

    /// <summary>Get the EnemyModel for a given definition (or null if not found).</summary>
    public static EnemyModel? FromDef(EnemyDef def)
    {
        int index = EnemyIds.All.IndexOf(def);
        return index >= 0 ? (EnemyModel)index : null;
    }

    /// <summary>Get the EnemyModel for a model ID string (or null if not found).</summary>
    public static EnemyModel? FromModelId(string modelId)
    {
        var def = EnemyIds.ByModelId(modelId);
        return def != null ? FromDef(def) : null;
    }
}
