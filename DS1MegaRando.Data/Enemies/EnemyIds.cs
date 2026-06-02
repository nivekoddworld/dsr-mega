namespace DS1MegaRando.Data.Enemies;

/// <summary>
/// Enemy model definition sourced from valid_new.txt (DS1 enemy randomizer reference data).
/// Model IDs correspond to c####.chrbnd.dcx files used by DSR.
/// </summary>
/// <param name="ModelId">Game model ID, e.g. "c2500"</param>
/// <param name="Name">Human-readable name</param>
/// <param name="Category">Broad category for placement logic</param>
/// <param name="BossCapable">True when this enemy can fill boss arenas (Type=1 in reference data)</param>
/// <param name="IsIgnored">True when AI is broken or otherwise excluded from the replacement pool</param>
/// <param name="Size">Relative size 0–5 (0=tiny, 5=colossal)</param>
/// <param name="Difficulty">Relative difficulty 0–7</param>
/// <param name="NpcParamId">Primary NpcParam row ID for stat scaling</param>
/// <param name="DefaultInitAnim">Idle anim ID to set as InitAnimID when placing this model (-1 = model default)</param>
/// <param name="CanFly">Enemy is primarily airborne</param>
public record EnemyDef(
    string ModelId,
    string Name,
    EnemyCategory Category,
    bool BossCapable    = false,
    bool IsIgnored      = false,
    int  Size           = 1,
    int  Difficulty     = 3,
    int  NpcParamId     = -1,
    int  DefaultInitAnim = -1,
    bool CanFly         = false);

public static class EnemyIds
{
    /// <summary>
    /// All known combat enemies.  Model IDs are taken from the reference enemy
    /// randomizer (valid_new.txt) and match DSR's chrbnd naming scheme.
    /// </summary>
    public static readonly IReadOnlyList<EnemyDef> All = new EnemyDef[]
    {
        // ── Rats ─────────────────────────────────────────────────────────────
        new("c1200", "Rat",               EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:120000),
        new("c1201", "Small Rat",         EnemyCategory.Beast,    Size:0, Difficulty:0, NpcParamId:120100),
        new("c1202", "Large Rat",         EnemyCategory.Beast,    Size:3, Difficulty:3, NpcParamId:120200),
        new("c1203", "Snow Rat",          EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:120300),

        // ── Hollows / Undead ─────────────────────────────────────────────────
        new("c2500", "Hollow",            EnemyCategory.Undead,   Size:1, Difficulty:0, NpcParamId:250000, DefaultInitAnim:9000),
        new("c2520", "Undead Assassin",   EnemyCategory.Undead,   Size:1, Difficulty:1, NpcParamId:252000),
        new("c2530", "Blowdart Sniper",   EnemyCategory.Undead,   Size:1, Difficulty:2, NpcParamId:253002),
        new("c2540", "Armored Hollow",    EnemyCategory.Undead,   Size:1, Difficulty:0, NpcParamId:254000, DefaultInitAnim:9000),
        new("c2550", "Undead Soldier",    EnemyCategory.Undead,   Size:1, Difficulty:1, NpcParamId:255000),
        new("c2560", "Balder Knight",     EnemyCategory.Undead,   Size:1, Difficulty:1, NpcParamId:256000),
        new("c2570", "Berenike Knight",   EnemyCategory.Undead,   Size:2, Difficulty:2, NpcParamId:257000),
        new("c2650", "Necromancer",       EnemyCategory.Undead,   Size:1, Difficulty:3, NpcParamId:265000),
        new("c2660", "Butcher",           EnemyCategory.Undead,   Size:1, Difficulty:3, NpcParamId:266000, DefaultInitAnim:9000),
        new("c2800", "Undead Crystal Soldier", EnemyCategory.Undead, Size:1, Difficulty:5, NpcParamId:280000),
        new("c2840", "Engorged Zombie",   EnemyCategory.Undead,   Size:1, Difficulty:4, NpcParamId:284000),

        // ── Knights / Elites ─────────────────────────────────────────────────
        new("c2390", "Darkwraith",        EnemyCategory.Undead,   Size:1, Difficulty:4, NpcParamId:239000),
        new("c2400", "Painting Guardian", EnemyCategory.Undead,   Size:1, Difficulty:4, NpcParamId:240000),
        new("c2410", "Silver Knight",     EnemyCategory.Miniboss, Size:1, Difficulty:4, NpcParamId:241000),
        new("c2690", "Serpent Soldier",   EnemyCategory.Undead,   Size:1, Difficulty:4, NpcParamId:269000, DefaultInitAnim:7000),
        new("c2700", "Serpent Mage",      EnemyCategory.Undead,   Size:1, Difficulty:4, NpcParamId:270000, DefaultInitAnim:9000),
        new("c2790", "Black Knight",      EnemyCategory.Miniboss, Size:1, Difficulty:5, NpcParamId:279000),

        // ── Skeletons ─────────────────────────────────────────────────────────
        new("c2900", "Skeleton",          EnemyCategory.Skeleton, Size:1, Difficulty:3, NpcParamId:290000),
        new("c2910", "Giant Skeleton",    EnemyCategory.Skeleton, Size:2, Difficulty:5, NpcParamId:291000),
        new("c2930", "Bonewheel Skeleton",EnemyCategory.Skeleton, Size:1, Difficulty:4, NpcParamId:293000),
        new("c2950", "Skeleton Beast",    EnemyCategory.Beast,    Size:2, Difficulty:6, NpcParamId:295000),
        new("c2960", "Bone Tower",        EnemyCategory.Skeleton, Size:1, Difficulty:2, NpcParamId:296000),

        // ── Beasts / Creatures ────────────────────────────────────────────────
        new("c2060", "Infested Ghoul",    EnemyCategory.Beast,    Size:1, Difficulty:2, NpcParamId:206000, DefaultInitAnim:9000),
        new("c2260", "Batwing Demon",     EnemyCategory.Demon,    Size:2, Difficulty:5, NpcParamId:226000, CanFly:true),
        new("c2310", "Crow Demon",        EnemyCategory.Beast,    Size:2, Difficulty:4, NpcParamId:231000, DefaultInitAnim:9000, CanFly:true),
        new("c2330", "Demonic Foliage",   EnemyCategory.Beast,    Size:1, Difficulty:3, NpcParamId:233000, DefaultInitAnim:9000),
        new("c2370", "Channeler",         EnemyCategory.Undead,   Size:1, Difficulty:3, NpcParamId:237000),
        new("c2380", "Giant Stone Knight",EnemyCategory.Golem,    Size:2, Difficulty:4, NpcParamId:238000, DefaultInitAnim:9000),
        new("c2430", "Demonic Statue",    EnemyCategory.Golem,    Size:1, Difficulty:3, NpcParamId:243000),
        new("c2710", "Crystal Golem",     EnemyCategory.Golem,    Size:2, Difficulty:3, NpcParamId:271000),
        new("c2711", "Golden Crystal Golem", EnemyCategory.Golem, Size:2, Difficulty:4, NpcParamId:271100),
        new("c2810", "Infested Barbarian",EnemyCategory.Beast,    Size:2, Difficulty:3, NpcParamId:281000),
        new("c2811", "Infested Barbarian (Boulder)", EnemyCategory.Beast, Size:2, Difficulty:3, NpcParamId:281100),
        new("c2830", "Phalanx",           EnemyCategory.Beast,    Size:1, Difficulty:2, NpcParamId:283001),
        new("c2860", "Giant",             EnemyCategory.Golem,    Size:4, Difficulty:5, NpcParamId:286000),
        new("c2870", "Sentinel",          EnemyCategory.Golem,    Size:3, Difficulty:5, NpcParamId:287000),
        new("c3090", "Giant Mosquito",    EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:309000, CanFly:true),
        new("c3200", "Slime",             EnemyCategory.Slime,    Size:0, Difficulty:1, NpcParamId:320000),
        new("c3210", "Egg Carrier",       EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:321000, DefaultInitAnim:9000),
        new("c3220", "Vile Maggot",       EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:322000),
        new("c3240", "Chaos Eater",       EnemyCategory.Beast,    Size:2, Difficulty:4, NpcParamId:324000),
        new("c3250", "Man-Eater Shell",   EnemyCategory.Beast,    Size:2, Difficulty:5, NpcParamId:325000, DefaultInitAnim:9000),
        new("c3270", "Basilisk",          EnemyCategory.Beast,    Size:1, Difficulty:3, NpcParamId:327000),
        new("c3340", "Undead Attack Dog", EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:334000, DefaultInitAnim:9000),
        new("c3341", "Flaming Attack Dog",EnemyCategory.Beast,    Size:0, Difficulty:2, NpcParamId:334100),
        new("c3350", "Possessed Tree",    EnemyCategory.Beast,    Size:3, Difficulty:1, NpcParamId:335000),
        new("c3370", "Tree Lizard",       EnemyCategory.Beast,    Size:0, Difficulty:2, NpcParamId:337000, DefaultInitAnim:9003),
        new("c3380", "Giant Leech",       EnemyCategory.Beast,    Size:0, Difficulty:1, NpcParamId:338000),
        new("c3400", "Crag-Spider",       EnemyCategory.Beast,    Size:0, Difficulty:2, NpcParamId:340000),
        new("c3410", "Frog-Ray",          EnemyCategory.Beast,    Size:0, Difficulty:2, NpcParamId:341000),
        new("c3460", "Armored Tusk",      EnemyCategory.Beast,    Size:3, Difficulty:3, NpcParamId:346000),
        new("c3461", "Armored Tusk (Dukes)", EnemyCategory.Beast, Size:3, Difficulty:5, NpcParamId:346100),
        new("c3491", "Evil Vagrant",      EnemyCategory.Misc,     Size:1, Difficulty:3, NpcParamId:349100),
        new("c3500", "Mass of Souls",     EnemyCategory.Ghost,    Size:3, Difficulty:5, NpcParamId:350000),
        new("c3520", "Drake",             EnemyCategory.Dragon,   Size:3, Difficulty:5, NpcParamId:352002, DefaultInitAnim:10000),
        new("c4120", "Stone Guardian",    EnemyCategory.Golem,    Size:2, Difficulty:6, NpcParamId:412000, DefaultInitAnim:9000),
        new("c4130", "Scarecrow",         EnemyCategory.Beast,    Size:1, Difficulty:4, NpcParamId:413000, DefaultInitAnim:9000),
        new("c4150", "Bloathead",         EnemyCategory.Undead,   Size:1, Difficulty:5, NpcParamId:415000, DefaultInitAnim:20000),
        new("c4160", "Bloathead Sorcerer",EnemyCategory.Undead,   Size:1, Difficulty:6, NpcParamId:416000),
        new("c4170", "Humanity Phantom",  EnemyCategory.Ghost,    Size:0, Difficulty:3, NpcParamId:417000),
        new("c4171", "Humanity Phantom (M)", EnemyCategory.Ghost, Size:1, Difficulty:4, NpcParamId:417100),
        new("c4172", "Humanity Phantom (L)", EnemyCategory.Ghost, Size:1, Difficulty:4, NpcParamId:417200),
        new("c4180", "Chained Prisoner",  EnemyCategory.Beast,    Size:2, Difficulty:6, NpcParamId:418000),
        new("c4190", "DLC Dog",           EnemyCategory.Beast,    Size:0, Difficulty:3, NpcParamId:419000, DefaultInitAnim:9000),
        new("c5360", "Great Feline",      EnemyCategory.Beast,    Size:2, Difficulty:5, NpcParamId:536000, DefaultInitAnim:9000),
        new("c5351", "Lightning Gargoyle",EnemyCategory.Boss,     BossCapable:true, Size:2, Difficulty:4, NpcParamId:535100),

        // ── Mushrooms ─────────────────────────────────────────────────────────
        new("c2270", "Mushroom Parent",   EnemyCategory.Mushroom, Size:1, Difficulty:5, NpcParamId:227000),
        new("c2280", "Mushroom Child",    EnemyCategory.Mushroom, Size:0, Difficulty:1, NpcParamId:228000),

        // ── Boss-Capable enemies (Type=1 in reference data) ───────────────────
        new("c2230", "Stray Demon",       EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:5, NpcParamId:223000),
        new("c2231", "Demon Firesage",    EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:5, NpcParamId:223100),
        new("c2240", "Capra Demon",       EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:4, NpcParamId:224000),
        new("c2250", "Taurus Demon",      EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:4, NpcParamId:225000, DefaultInitAnim:9001),
        new("c2320", "Iron Golem",        EnemyCategory.Boss,     BossCapable:true, Size:4, Difficulty:6, NpcParamId:232000, DefaultInitAnim:9000),
        new("c2360", "Smough",            EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:5, NpcParamId:236000),
        new("c2730", "Crossbreed Priscilla", EnemyCategory.Boss,  BossCapable:true, Size:3, Difficulty:6, NpcParamId:273000, DefaultInitAnim:9000),
        new("c3320", "Pinwheel",          EnemyCategory.Boss,     BossCapable:true, Size:1, Difficulty:3, NpcParamId:332000),
        new("c3471", "Sanctuary Guardian",EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:6, NpcParamId:347100, CanFly:true),
        new("c4100", "Knight Artorias",   EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:7, NpcParamId:410000),
        new("c4500", "Manus",             EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:7, NpcParamId:450000),
        new("c4510", "Black Dragon Kalameet", EnemyCategory.Boss, BossCapable:true, Size:4, Difficulty:7, NpcParamId:451000, CanFly:true),
        new("c5200", "Centipede Demon",   EnemyCategory.Boss,     BossCapable:true, Size:4, Difficulty:5, NpcParamId:520000),
        new("c5210", "Sif",               EnemyCategory.Boss,     BossCapable:true, Size:4, Difficulty:5, NpcParamId:521000, DefaultInitAnim:10000),
        new("c5220", "Gravelord Nito",    EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:6, NpcParamId:522000, DefaultInitAnim:9000),
        new("c5260", "Gaping Dragon",     EnemyCategory.Boss,     BossCapable:true, Size:5, Difficulty:4, NpcParamId:526000),
        new("c5270", "Ornstein",          EnemyCategory.Boss,     BossCapable:true, Size:2, Difficulty:4, NpcParamId:527000),
        new("c5271", "Super Ornstein",    EnemyCategory.Boss,     BossCapable:true, Size:3, Difficulty:5, NpcParamId:527100),
        new("c5280", "Chaos Witch Quelaag", EnemyCategory.Boss,   BossCapable:true, Size:3, Difficulty:4, NpcParamId:528000),
        new("c5290", "Seath the Scaleless", EnemyCategory.Boss,   BossCapable:true, Size:4, Difficulty:6, NpcParamId:529000),
        new("c5350", "Bell Gargoyle",     EnemyCategory.Boss,     BossCapable:true, Size:2, Difficulty:3, NpcParamId:535000, DefaultInitAnim:7000),
        new("c5370", "Gwyn",              EnemyCategory.Boss,     BossCapable:true, Size:1, Difficulty:7, NpcParamId:537000),
        new("c5390", "Four Kings",        EnemyCategory.Boss,     BossCapable:true, Size:2, Difficulty:6, NpcParamId:539000),

        // ── IsIgnored — broken AI or scripted; never place as replacement ─────
        new("c2232", "Asylum Demon",      EnemyCategory.Boss,     IsIgnored:true, Size:3, Difficulty:3, NpcParamId:223200),
        new("c2300", "Titanite Demon",    EnemyCategory.Miniboss, IsIgnored:true, Size:3, Difficulty:4, NpcParamId:230000),
        new("c2670", "Ghost",             EnemyCategory.Ghost,    IsIgnored:true, Size:1, Difficulty:4, NpcParamId:267000),
        new("c2680", "Lightning Ghost",   EnemyCategory.Ghost,    IsIgnored:true, Size:1, Difficulty:4, NpcParamId:268000),
        new("c2780", "Mimic",             EnemyCategory.Beast,    IsIgnored:true, Size:2, Difficulty:5, NpcParamId:278000),
        new("c2791", "Black Knight (var)",EnemyCategory.Miniboss, IsIgnored:true, Size:1, Difficulty:5, NpcParamId:279000),
        new("c2792", "Black Knight (var)",EnemyCategory.Miniboss, IsIgnored:true, Size:1, Difficulty:5, NpcParamId:279000),
        new("c2793", "Black Knight (var)",EnemyCategory.Miniboss, IsIgnored:true, Size:1, Difficulty:5, NpcParamId:279000),
        new("c2940", "Skeleton Baby",     EnemyCategory.Skeleton, IsIgnored:true, Size:0, Difficulty:2, NpcParamId:294000),
        new("c3230", "Moonlight Butterfly",EnemyCategory.Boss,    IsIgnored:true, Size:4, Difficulty:3, NpcParamId:323000, CanFly:true),
        new("c3300", "Crystal Lizard",    EnemyCategory.Beast,    IsIgnored:true, Size:0, Difficulty:0, NpcParamId:330000),
        new("c3330", "Pisaca",            EnemyCategory.Ghost,    IsIgnored:true, Size:1, Difficulty:4, NpcParamId:333000),
        new("c3390", "Burrowing Rockworm",EnemyCategory.Beast,    IsIgnored:true, Size:3, Difficulty:5, NpcParamId:339000),
        new("c3420", "Undead Dragon",     EnemyCategory.Dragon,   IsIgnored:true, Size:5, Difficulty:5, NpcParamId:342000),
        new("c3421", "Bounding Demon",    EnemyCategory.Demon,    IsIgnored:true, Size:4, Difficulty:5, NpcParamId:342100),
        new("c3430", "Hellkite Drake",    EnemyCategory.Dragon,   IsIgnored:true, Size:5, Difficulty:6, NpcParamId:343000, CanFly:true),
        new("c3480", "Chaos Bug",         EnemyCategory.Beast,    IsIgnored:true, Size:0, Difficulty:0, NpcParamId:348000),
        new("c3490", "Good Vagrant",      EnemyCategory.Misc,     IsIgnored:true, Size:0, Difficulty:1, NpcParamId:349000),
        new("c3530", "Hydra",             EnemyCategory.Dragon,   IsIgnored:true, Size:5, Difficulty:5, NpcParamId:353000),
        new("c5201", "Centipede Arm",     EnemyCategory.Beast,    IsIgnored:true, Size:1, Difficulty:5, NpcParamId:520100),
        new("c5202", "Centipede Tail",    EnemyCategory.Beast,    IsIgnored:true, Size:1, Difficulty:5, NpcParamId:520200),
        new("c5240", "Parasitic Wall Hugger", EnemyCategory.Beast,IsIgnored:true, Size:2, Difficulty:4, NpcParamId:524000),
        new("c5250", "Ceaseless Discharge",EnemyCategory.Boss,    IsIgnored:true, Size:5, Difficulty:5, NpcParamId:525000),
        new("c5320", "Gwyndolin",         EnemyCategory.Boss,     IsIgnored:true, Size:1, Difficulty:4, NpcParamId:532000, DefaultInitAnim:9000),
    };

    private static readonly IReadOnlyDictionary<string, EnemyDef> _byModelId =
        All.ToDictionary(e => e.ModelId);

    public static EnemyDef? ByModelId(string modelId) =>
        _byModelId.TryGetValue(modelId, out var e) ? e : null;

    /// <summary>Enemies that can fill any slot including boss arenas.</summary>
    public static IEnumerable<EnemyDef> BossCapable =>
        All.Where(e => e.BossCapable && !e.IsIgnored);

    /// <summary>Standard boss enemies (BossCapable = original bosses).</summary>
    public static IEnumerable<EnemyDef> Bosses =>
        All.Where(e => e.Category == EnemyCategory.Boss && e.BossCapable && !e.IsIgnored);

    /// <summary>Regular (non-boss) enemies safe to use as replacements.</summary>
    public static IEnumerable<EnemyDef> Regular =>
        All.Where(e => !e.IsIgnored
                    && e.Category != EnemyCategory.NPC
                    && e.Category != EnemyCategory.Misc);

    /// <summary>
    /// Model IDs for important story NPCs that must never be randomized.
    /// These use c0xxx IDs in the MSB and aren't in the replacement catalog.
    /// </summary>
    public static readonly IReadOnlySet<string> ProtectedNPCModels = new HashSet<string>
    {
        "c0010", // Blacksmith Andre
        "c0100", // Kingseeker Frampt
        "c0101", // Darkstalker Kaathe
        "c0110", // Firekeeper
    };

    /// <summary>
    /// Model IDs that have Type=2 (NPC) in the reference data — never place
    /// as enemies even if they somehow end up in MSB parts lists.
    /// </summary>
    public static readonly IReadOnlySet<string> NpcModels = new HashSet<string>
    {
        "c2510", // Undead Merchant
        "c2640", // Andre of Astora
        "c2920", // Vamos
        "c4110", // Hawkeye Gough
    };
}
