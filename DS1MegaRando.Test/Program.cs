using DS1MegaRando.Core;
using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Graph;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Settings;
using DS1MegaRando.Core.Verification;

const string GAME_DIR   = @"D:\SteamLibrary\steamapps\common\DARK SOULS REMASTERED";
const int    FOG_SEEDS  = 500;
const int    COMBO_SEEDS = 5;
const int    MAX_RETRIES = 10;

// ═══════════════════════════════════════════════════════════════════════════
// PHASE 1 — Fog gate 500-seed regression (no game files needed)
// ═══════════════════════════════════════════════════════════════════════════
Console.WriteLine("══════════════════════════════════════════════════");
Console.WriteLine("PHASE 1 — Fog gate 500-seed regression");
Console.WriteLine("══════════════════════════════════════════════════");
{
    var ann = AnnotationLoader.LoadFog();
    var fogCfg = new FogGateSettings
    {
        EnableFogGateRandomizer = true, RandomizeBossEntrances  = true,
        RandomizeWorldEntrances = true, RandomizeMajorEntrances = true,
        RandomizeMinorEntrances = true, RandomizeWarps          = true,
        IncludeDLC = false, LordvesselDoorsRandomized = false, FixKilnEntrance = true,
    };
    var ms        = new MegaSettings { FogGate = fogCfg };
    var connector = new GraphConnector();
    var softlock  = new SoftlockChecker();
    int passed = 0, failed = 0;
    var failReasons = new Dictionary<string, int>(StringComparer.Ordinal);

    for (int s = 0; s < FOG_SEEDS; s++)
    {
        uint baseSeed = (uint)(s * 1234567 + 42);
        bool ok = false; List<string>? lastIssues = null;
        for (int t = 0; t < MAX_RETRIES && !ok; t++)
        {
            uint seed = baseSeed + (uint)t;
            try
            {
                var graph = new WorldGraph();
                graph.Construct(fogCfg, ann);
                connector.Connect(fogCfg, graph, ann, new Random((int)seed));
                var fr = new DS1MegaRando.Core.FogGate.FogGateResult
                    { ConnectedGraph = graph, StartArea = graph.StartArea };
                var vr = softlock.Verify(fr, null, ann, ms);
                if (vr.Passed) ok = true; else lastIssues = vr.Issues;
            }
            catch (Exception ex) { lastIssues = new List<string> { $"EXCEPTION: {ex.Message}" }; }
        }
        if (ok) passed++;
        else { failed++; foreach (var i in lastIssues ?? new List<string> { "?" }) failReasons[i.Length > 80 ? i[..80] : i] = failReasons.GetValueOrDefault(i.Length > 80 ? i[..80] : i) + 1; }
        if ((s + 1) % 100 == 0) Console.WriteLine($"  {s + 1}/{FOG_SEEDS} — pass {passed}, fail {failed}");
    }
    Console.WriteLine($"\nPhase 1: {passed}/{FOG_SEEDS} PASSED ({100.0 * passed / FOG_SEEDS:F1}%)");
    if (failReasons.Count > 0)
        foreach (var (r, c) in failReasons.OrderByDescending(kv => kv.Value).Take(5))
            Console.WriteLine($"  [{c,4}x] {r}");
}

// ═══════════════════════════════════════════════════════════════════════════
// PHASE 2 — Full MegaRandomizer settings matrix (DryRun, real game files)
// ═══════════════════════════════════════════════════════════════════════════
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════");
Console.WriteLine("PHASE 2 — Settings matrix (DryRun, real game files)");
Console.WriteLine("══════════════════════════════════════════════════");

if (!Directory.Exists(GAME_DIR)) { Console.WriteLine($"[SKIP] Game not found at {GAME_DIR}"); return; }

GameData gameData;
AnnotationData ann2;
try
{
    var g = new GlobalSettings { GameDirectory = GAME_DIR, DryRun = true };
    gameData = new GameFileReader(g).ReadAll();
    ann2     = AnnotationLoader.LoadFog();
    Console.WriteLine($"Loaded: {gameData.ItemLotParam?.Rows.Count} lots, {gameData.Maps.Count} maps\n");
}
catch (Exception ex) { Console.WriteLine($"[FAIL] Load: {ex.Message}"); return; }

var presets  = BuildPresets();
int totPass  = 0, totFail = 0;
var failures = new List<(string Preset, uint Seed, string Error)>();

Console.WriteLine($"Testing {presets.Count} presets × {COMBO_SEEDS} seeds = {presets.Count * COMBO_SEEDS} runs\n");

foreach (var (label, ms) in presets)
{
    int presPass = 0, presFail = 0;
    for (int s = 0; s < COMBO_SEEDS; s++)
    {
        uint seed = (uint)(s * 777777 + Math.Abs(label.GetHashCode()));
        ms.Global.Seed          = seed;
        ms.Global.DryRun        = true;
        ms.Global.GameDirectory = GAME_DIR;
        try
        {
            new MegaRandomizer().Randomize(ms, gameData, ann2);
            presPass++; totPass++;
        }
        catch (Exception ex)
        {
            presFail++; totFail++;
            failures.Add((label, seed, $"{ex.GetType().Name}: {ex.Message.Split('\n')[0]}"));
        }
    }
    string status = presFail == 0 ? "PASS" : $"FAIL({presFail})";
    Console.WriteLine($"  [{status,8}]  {label}");
}

Console.WriteLine();
Console.WriteLine($"Phase 2: {totPass}/{totPass + totFail} PASSED");
if (failures.Count == 0)
    Console.WriteLine("All presets passed!");
else
{
    Console.WriteLine($"\n═══ {failures.Count} FAILURES ═══");
    foreach (var (preset, seed, err) in failures)
        Console.WriteLine($"  {preset}  seed={seed:X8}\n    → {err}");
}

// ═══════════════════════════════════════════════════════════════════════════
// Preset factory
// ═══════════════════════════════════════════════════════════════════════════

static List<(string, MegaSettings)> BuildPresets()
{
    var L = new List<(string, MegaSettings)>();
    void AF(string n, FogGateSettings f) => L.Add((n, C(f, null, null)));
    void AI(string n, ItemSettings    i) => L.Add((n, C(null, i, null)));
    void AE(string n, EnemySettings   e) => L.Add((n, C(null, null, e)));
    void AC(string n, FogGateSettings? f, ItemSettings? i, EnemySettings? e) => L.Add((n, C(f, i, e)));

    // ── Fog only ──────────────────────────────────────────────────────────
    AF("Fog_Default",         F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:true));
    AF("Fog_All_On",          F(boss:true, world:true, major:true,  minor:true,  warps:true,  dlc:true,  lv:true,  kiln:false));
    AF("Fog_Boss_Only",       F(boss:true, world:false,major:false, minor:false, warps:false, dlc:false, lv:false, kiln:true));
    AF("Fog_No_Boss",         F(boss:false,world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:true));
    AF("Fog_DLC",             F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:true,  lv:false, kiln:true));
    AF("Fog_LordvesselRando", F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:true,  kiln:true));
    AF("Fog_Warps",           F(boss:true, world:true, major:true,  minor:false, warps:true,  dlc:false, lv:false, kiln:true));
    AF("Fog_Minor",           F(boss:true, world:true, major:true,  minor:true,  warps:false, dlc:false, lv:false, kiln:true));
    AF("Fog_KilnNotFixed",    F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:false));
    AF("Fog_StartingArea",    F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:true,  startArea:true));
    AF("Fog_Logic_Glitched",  F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:true,  logic:LogicMode.Glitched));
    AF("Fog_Logic_NoLogic",   F(boss:true, world:true, major:true,  minor:false, warps:false, dlc:false, lv:false, kiln:true,  logic:LogicMode.NoLogic));
    AF("Fog_Everything",      F(boss:true, world:true, major:true,  minor:true,  warps:true,  dlc:true,  lv:true,  kiln:false, startArea:true));

    // ── Items only ────────────────────────────────────────────────────────
    foreach (var km in Enum.GetValues<KeyItemMode>())
        AI($"Items_KeyMode_{km}",  I(keyMode:km));
    foreach (var d in Enum.GetValues<ItemDifficultyMode>())
        AI($"Items_Diff_{d}",      I(diff:d));
    foreach (var bs in Enum.GetValues<BossSoulHandling>())
        AI($"Items_BossSoul_{bs}", I(bossSoul:bs));
    foreach (var sp in Enum.GetValues<ShopPriceMode>())
        AI($"Items_ShopPrice_{sp}",I(shopPrice:sp));
    foreach (var lm in Enum.GetValues<StartingLoadoutMode>())
        AI($"Items_Loadout_{lm}",  I(loadout:lm));
    AI("Items_NoShops",         I(shops:false));
    AI("Items_NoLoadout",       I(startLoadout:false));
    AI("Items_ExcludeUseless",  I(excludeUseless:true));
    AI("Items_KeyInShops",      I(keyInShops:true));
    AI("Items_Fashion",         I(fashion:true));
    AI("Items_NPCArmor",        I(npcArmor:true));
    AI("Items_RandoLV",         I(randoLV:true));
    AI("Items_RandoLS",         I(randoLS:true));
    AI("Items_DLCLocs",         I(dlcLocs:true));
    AI("Items_NoShopsNoLoadout",I(shops:false, startLoadout:false));
    AI("Items_AllOn",           I(keyMode:KeyItemMode.RaceMode, diff:ItemDifficultyMode.Hard,
                                  bossSoul:BossSoulHandling.AllowTranspose, shops:true,
                                  shopPrice:ShopPriceMode.Random, startLoadout:true,
                                  loadout:StartingLoadoutMode.CombinedPool, fashion:true,
                                  npcArmor:true, randoLV:true, randoLS:true, dlcLocs:true,
                                  excludeUseless:false, keyInShops:true));

    // ── Enemies only ──────────────────────────────────────────────────────
    foreach (var pm in Enum.GetValues<EnemyPlacementMode>())
        AE($"Enemy_Mode_{pm}",       E(mode:pm));
    foreach (var bm in Enum.GetValues<BossRandomizationMode>())
        AE($"Enemy_BossMode_{bm}",   E(bossMode:bm));
    foreach (var src in Enum.GetValues<EnemyScalingSource>())
        AE($"Enemy_ScaleSrc_{src}",  E(scaleSrc:src));
    AE("Enemy_NoScale",         E(scale:false));
    AE("Enemy_NoBosses",        E(randoBosses:false));
    AE("Enemy_NoBossOrMini",    E(randoBosses:false, randoMini:false));
    AE("Enemy_DupBosses",       E(dupBosses:true));
    AE("Enemy_NoDLC",           E(dlc:false));
    AE("Enemy_NoProtectNPCs",   E(protectNPCs:false));
    AE("Enemy_Flying",          E(flying:true));
    AE("Enemy_BossOnly_DupOK",  E(mode:EnemyPlacementMode.BossOnly, dupBosses:true));
    AE("Enemy_AllOn",           E(mode:EnemyPlacementMode.FullRandom, bossMode:BossRandomizationMode.FreeForAll,
                                  dupBosses:true, scale:true, scaleSrc:EnemyScalingSource.FogGateDepth,
                                  dlc:true, flying:true, protectNPCs:false));

    // ── Fog + Items ───────────────────────────────────────────────────────
    AC("FogItem_Default",        Fd(), I(), null);
    AC("FogItem_Race",           Fd(), I(keyMode:KeyItemMode.RaceMode), null);
    AC("FogItem_Speedrun",       Fd(), I(keyMode:KeyItemMode.SpeedrunMode), null);
    AC("FogItem_DLC",            F(boss:true,world:true,major:true,minor:false,warps:false,dlc:true,lv:false,kiln:true), I(dlcLocs:true), null);
    AC("FogItem_LVandLS",        F(boss:true,world:true,major:true,minor:false,warps:false,dlc:false,lv:true,kiln:true), I(randoLV:true,randoLS:true), null);
    AC("FogItem_Warps+Race",     F(boss:true,world:true,major:true,minor:false,warps:true,dlc:false,lv:false,kiln:true), I(keyMode:KeyItemMode.RaceMode), null);
    AC("FogItem_AllOn",          F(boss:true,world:true,major:true,minor:true,warps:true,dlc:true,lv:true,kiln:false),  I(keyMode:KeyItemMode.RaceMode,diff:ItemDifficultyMode.Hard,randoLV:true,randoLS:true,dlcLocs:true), null);

    // ── Fog + Enemy ───────────────────────────────────────────────────────
    AC("FogEnemy_Default",       Fd(), null, E());
    AC("FogEnemy_BossOnly",      Fd(), null, E(mode:EnemyPlacementMode.BossOnly));
    AC("FogEnemy_FullRandom",    Fd(), null, E(mode:EnemyPlacementMode.FullRandom));
    AC("FogEnemy_DLC+Scale",     F(boss:true,world:true,major:true,minor:false,warps:false,dlc:true,lv:false,kiln:true), null, E(dlc:true,scaleSrc:EnemyScalingSource.FogGateDepth));
    AC("FogEnemy_NoLogic+Free",  F(boss:true,world:true,major:true,minor:false,warps:false,dlc:false,lv:false,kiln:true,logic:LogicMode.NoLogic), null, E(mode:EnemyPlacementMode.FullRandom,bossMode:BossRandomizationMode.FreeForAll));

    // ── Mega (all three) ──────────────────────────────────────────────────
    AC("Mega_Default",       Fd(), I(), E());
    AC("Mega_Minimal",       F(boss:true,world:true,major:false,minor:false,warps:false,dlc:false,lv:false,kiln:true),
                             I(keyMode:KeyItemMode.Vanilla,shops:false,startLoadout:false),
                             E(mode:EnemyPlacementMode.BossOnly,scale:false));
    AC("Mega_SpeedrunMode",  Fd(), I(keyMode:KeyItemMode.SpeedrunMode,shops:true,shopPrice:ShopPriceMode.Free), E(mode:EnemyPlacementMode.CategoryRestricted));
    AC("Mega_RaceMode",      F(boss:true,world:true,major:true,minor:false,warps:true,dlc:false,lv:false,kiln:true),
                             I(keyMode:KeyItemMode.RaceMode,shopPrice:ShopPriceMode.Scaled),
                             E(mode:EnemyPlacementMode.AreaCohesive));
    AC("Mega_AllFeatures",   F(boss:true,world:true,major:true,minor:true,warps:true,dlc:true,lv:true,kiln:false),
                             I(keyMode:KeyItemMode.RaceMode,diff:ItemDifficultyMode.Hard,
                               bossSoul:BossSoulHandling.AllowTranspose,shops:true,
                               shopPrice:ShopPriceMode.Random,startLoadout:true,
                               loadout:StartingLoadoutMode.CombinedPool,fashion:true,
                               npcArmor:true,randoLV:true,randoLS:true,dlcLocs:true),
                             E(mode:EnemyPlacementMode.FullRandom,bossMode:BossRandomizationMode.FreeForAll,
                               dupBosses:true,scale:true,scaleSrc:EnemyScalingSource.FogGateDepth,
                               dlc:true,flying:true,protectNPCs:false));
    AC("Mega_DLC_Everything", F(boss:true,world:true,major:true,minor:true,warps:true,dlc:true,lv:true,kiln:false,startArea:true),
                              I(keyMode:KeyItemMode.Shuffled,diff:ItemDifficultyMode.Medium,dlcLocs:true,randoLV:true),
                              E(mode:EnemyPlacementMode.AreaCohesive,dlc:true,scale:true));
    AC("Mega_Speedrun+Area",  F(boss:true,world:true,major:true,minor:false,warps:false,dlc:false,lv:false,kiln:true),
                              I(keyMode:KeyItemMode.SpeedrunMode,shops:true,shopPrice:ShopPriceMode.Free,startLoadout:true),
                              E(mode:EnemyPlacementMode.AreaCohesive,scaleSrc:EnemyScalingSource.VanillaDepth));

    return L;
}

// ── Builder helpers ────────────────────────────────────────────────────────

static FogGateSettings Fd() =>
    F(boss:true, world:true, major:true, minor:false, warps:false, dlc:false, lv:false, kiln:true);

static FogGateSettings F(
    bool boss, bool world, bool major, bool minor, bool warps,
    bool dlc, bool lv, bool kiln,
    bool startArea = false, LogicMode logic = LogicMode.Normal) =>
    new FogGateSettings
    {
        EnableFogGateRandomizer = true,
        RandomizeBossEntrances    = boss,  RandomizeWorldEntrances   = world,
        RandomizeMajorEntrances   = major, RandomizeMinorEntrances   = minor,
        RandomizeWarps            = warps, IncludeDLC                = dlc,
        LordvesselDoorsRandomized = lv,    FixKilnEntrance           = kiln,
        RandomizeStartingArea     = startArea, LogicMode             = logic,
    };

static ItemSettings I(
    KeyItemMode keyMode         = KeyItemMode.Shuffled,
    ItemDifficultyMode diff     = ItemDifficultyMode.Medium,
    BossSoulHandling bossSoul   = BossSoulHandling.Shuffle,
    bool shops                  = true,
    ShopPriceMode shopPrice     = ShopPriceMode.Scaled,
    bool startLoadout           = true,
    StartingLoadoutMode loadout = StartingLoadoutMode.ShieldAnd1H,
    bool fashion        = false, bool npcArmor   = false,
    bool randoLV        = false, bool randoLS     = false,
    bool dlcLocs        = false, bool excludeUseless = false,
    bool keyInShops     = false) =>
    new ItemSettings
    {
        EnableItemRandomizer    = true, KeyItemMode             = keyMode,
        Difficulty              = diff,  BossSoulHandling        = bossSoul,
        RandomizeShops          = shops, ShopPriceMode           = shopPrice,
        RandomizeStartingLoadout= startLoadout, StartingLoadoutMode = loadout,
        FashionSouls            = fashion, RandomizeNPCDropArmor = npcArmor,
        RandomizeLordvessel     = randoLV, RandomizeLordSouls    = randoLS,
        IncludeDLCItemLocations = dlcLocs, ExcludeUselessItems   = excludeUseless,
        AllowKeyItemsInShops    = keyInShops,
    };

static EnemySettings E(
    EnemyPlacementMode mode        = EnemyPlacementMode.CategoryRestricted,
    BossRandomizationMode bossMode = BossRandomizationMode.BossForBoss,
    bool randoBosses = true, bool randoMini = true, bool dupBosses = false,
    bool scale       = true,
    EnemyScalingSource scaleSrc    = EnemyScalingSource.FogGateDepth,
    bool dlc         = true, bool flying = false, bool protectNPCs = true) =>
    new EnemySettings
    {
        EnableEnemyRandomizer     = true,  EnemyPlacementMode        = mode,
        RandomizeBosses           = randoBosses, BossRandomizationMode = bossMode,
        AllowDuplicateBosses      = dupBosses,   RandomizeMinibosses   = randoMini,
        ScaleEnemyStats           = scale,        EnemyScalingSource   = scaleSrc,
        IncludeDLCEnemies         = dlc,           AllowFlyingEnemiesInDoors = flying,
        ProtectImportantNPCs      = protectNPCs,
    };

static MegaSettings C(FogGateSettings? fog, ItemSettings? items, EnemySettings? enemies) =>
    new MegaSettings
    {
        FogGate = fog     ?? new FogGateSettings { EnableFogGateRandomizer = false },
        Items   = items   ?? new ItemSettings    { EnableItemRandomizer    = false },
        Enemies = enemies ?? new EnemySettings   { EnableEnemyRandomizer  = false },
    };
