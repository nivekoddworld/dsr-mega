namespace DS1MegaRando.Data.Enemies;

public record EnemyDef(string ModelId, string Name, EnemyCategory Category, bool CanFly = false);

/// <summary>
/// DS1R NPC model IDs and their categories.
/// Model IDs correspond to c####.chrbnd.dcx files.
/// </summary>
public static class EnemyIds
{
    public static readonly IReadOnlyList<EnemyDef> All = new[]
    {
        // ── Undead ────────────────────────────────────────────────────────
        new EnemyDef("c2010", "Hollow Warrior",          EnemyCategory.Undead),
        new EnemyDef("c2020", "Hollow Soldier",          EnemyCategory.Undead),
        new EnemyDef("c2030", "Hollow Knight",           EnemyCategory.Undead),
        new EnemyDef("c2040", "Hollow Archer",           EnemyCategory.Undead),
        new EnemyDef("c2060", "Infested Barbarian",      EnemyCategory.Undead),
        new EnemyDef("c2090", "Channeler",               EnemyCategory.NPC),
        new EnemyDef("c2100", "Balder Knight",           EnemyCategory.Undead),
        new EnemyDef("c2120", "Balder Side Sword Knight",EnemyCategory.Undead),
        new EnemyDef("c2140", "Pardoner",                EnemyCategory.Undead),

        // ── Skeletons ─────────────────────────────────────────────────────
        new EnemyDef("c2200", "Skeleton",                EnemyCategory.Skeleton),
        new EnemyDef("c2210", "Skeleton Archer",         EnemyCategory.Skeleton),
        new EnemyDef("c2220", "Bonewheel Skeleton",      EnemyCategory.Skeleton),
        new EnemyDef("c2230", "Giant Skeleton",          EnemyCategory.Skeleton),

        // ── Beasts ────────────────────────────────────────────────────────
        new EnemyDef("c2300", "Crow Demon",              EnemyCategory.Beast,  CanFly: true),
        new EnemyDef("c2310", "Frog",                    EnemyCategory.Beast),
        new EnemyDef("c2320", "Basilisk",                EnemyCategory.Beast),
        new EnemyDef("c2330", "Mushroom Parent",         EnemyCategory.Mushroom),
        new EnemyDef("c2340", "Mushroom Child",          EnemyCategory.Mushroom),
        new EnemyDef("c2350", "Man-Eater Shell",         EnemyCategory.Beast),
        new EnemyDef("c2360", "Giant Clam",              EnemyCategory.Beast),
        new EnemyDef("c2370", "Leech",                   EnemyCategory.Beast),
        new EnemyDef("c2380", "Giant Leech",             EnemyCategory.Beast),
        new EnemyDef("c2390", "Black Hydra",             EnemyCategory.Beast),
        new EnemyDef("c2400", "Hydra",                   EnemyCategory.Beast),
        new EnemyDef("c2410", "Rat",                     EnemyCategory.Beast),
        new EnemyDef("c2420", "Giant Rat",               EnemyCategory.Beast),
        new EnemyDef("c2430", "Slime",                   EnemyCategory.Slime),
        new EnemyDef("c2440", "Infested Ghoul",          EnemyCategory.Beast),
        new EnemyDef("c2450", "Phalanx Hollow",          EnemyCategory.Hollow),
        new EnemyDef("c2460", "Phalanx Blob",            EnemyCategory.Beast),
        new EnemyDef("c2480", "Blowdart Sniper",         EnemyCategory.Undead),
        new EnemyDef("c2490", "Cragspider",              EnemyCategory.Beast),
        new EnemyDef("c2500", "Chaos Bug",               EnemyCategory.Beast),
        new EnemyDef("c2510", "Chaos Eater",             EnemyCategory.Beast),

        // ── Golems ────────────────────────────────────────────────────────
        new EnemyDef("c2540", "Stone Guardian",          EnemyCategory.Golem),
        new EnemyDef("c2550", "Stone Knight",            EnemyCategory.Golem),
        new EnemyDef("c2560", "Sentinel",                EnemyCategory.Golem),
        new EnemyDef("c2570", "Giant Stone Knight",      EnemyCategory.Golem),

        // ── Ghosts ────────────────────────────────────────────────────────
        new EnemyDef("c2600", "Ghost",                   EnemyCategory.Ghost),
        new EnemyDef("c2610", "Banshee",                 EnemyCategory.Ghost),

        // ── Black Knights ─────────────────────────────────────────────────
        new EnemyDef("c2700", "Black Knight (Sword)",    EnemyCategory.Miniboss),
        new EnemyDef("c2710", "Black Knight (Greatsword)",EnemyCategory.Miniboss),
        new EnemyDef("c2720", "Black Knight (Greataxe)", EnemyCategory.Miniboss),
        new EnemyDef("c2730", "Black Knight (Halberd)",  EnemyCategory.Miniboss),
        new EnemyDef("c2740", "Silver Knight (Sword)",   EnemyCategory.Miniboss),
        new EnemyDef("c2750", "Silver Knight (Spear)",   EnemyCategory.Miniboss),
        new EnemyDef("c2760", "Silver Knight (Bow)",     EnemyCategory.Miniboss),

        // ── Demons ────────────────────────────────────────────────────────
        new EnemyDef("c2800", "Asylum Demon",            EnemyCategory.Boss),
        new EnemyDef("c2900", "Taurus Demon",            EnemyCategory.Boss),
        new EnemyDef("c2910", "Capra Demon",             EnemyCategory.Boss),
        new EnemyDef("c2920", "Demon Firesage",          EnemyCategory.Boss),
        new EnemyDef("c2930", "Centipede Demon",         EnemyCategory.Boss),

        // ── Dragons ───────────────────────────────────────────────────────
        new EnemyDef("c3000", "Hellkite Drake",          EnemyCategory.Dragon, CanFly: true),
        new EnemyDef("c3010", "Undead Dragon",           EnemyCategory.Dragon),

        // ── Bosses ────────────────────────────────────────────────────────
        new EnemyDef("c3080", "Bell Gargoyle",           EnemyCategory.Boss,   CanFly: true),
        new EnemyDef("c3100", "Moonlight Butterfly",     EnemyCategory.Boss,   CanFly: true),
        new EnemyDef("c3120", "Sif the Great Wolf",      EnemyCategory.Boss),
        new EnemyDef("c3200", "Gaping Dragon",           EnemyCategory.Boss),
        new EnemyDef("c3210", "Chaos Witch Quelaag",     EnemyCategory.Boss),
        new EnemyDef("c3220", "Ceaseless Discharge",     EnemyCategory.Boss),
        new EnemyDef("c3230", "Bed of Chaos",            EnemyCategory.Boss),
        new EnemyDef("c3240", "Gravelord Nito",          EnemyCategory.Boss),
        new EnemyDef("c3250", "Four Kings",              EnemyCategory.Boss),
        new EnemyDef("c3260", "Seath the Scaleless",     EnemyCategory.Boss),
        new EnemyDef("c3280", "Iron Golem",              EnemyCategory.Boss),
        new EnemyDef("c3300", "Dragon Slayer Ornstein",  EnemyCategory.Boss),
        new EnemyDef("c3301", "Executioner Smough",      EnemyCategory.Boss),
        new EnemyDef("c3320", "Gwyndolin",               EnemyCategory.Boss),
        new EnemyDef("c3360", "Gwyn, Lord of Cinder",    EnemyCategory.Boss),
        new EnemyDef("c3380", "Knight Artorias",         EnemyCategory.Boss),
        new EnemyDef("c3390", "Black Dragon Kalameet",   EnemyCategory.Boss,   CanFly: true),
        new EnemyDef("c3400", "Manus, Father of the Abyss", EnemyCategory.Boss),
    };

    private static readonly IReadOnlyDictionary<string, EnemyDef> _byModelId =
        All.ToDictionary(e => e.ModelId);

    public static EnemyDef? ByModelId(string modelId) =>
        _byModelId.TryGetValue(modelId, out var e) ? e : null;

    public static IEnumerable<EnemyDef> Bosses =>
        All.Where(e => e.Category == EnemyCategory.Boss);

    public static IEnumerable<EnemyDef> Minibosses =>
        All.Where(e => e.Category == EnemyCategory.Miniboss);

    public static IEnumerable<EnemyDef> Regular =>
        All.Where(e => e.Category != EnemyCategory.Boss &&
                       e.Category != EnemyCategory.Miniboss &&
                       e.Category != EnemyCategory.NPC);

    // Important NPC model IDs that should never be replaced
    public static readonly IReadOnlySet<string> ProtectedNPCModels = new HashSet<string>
    {
        "c0010", // Blacksmith Andre
        "c0100", // Kingseeker Frampt
        "c0101", // Darkstalker Kaathe
        "c0110", // Firekeeper
    };
}
