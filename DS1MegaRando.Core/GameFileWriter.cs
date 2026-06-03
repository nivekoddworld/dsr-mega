using System.Numerics;
using DS1MegaRando.Annotations;
using DS1MegaRando.Enemies;
using DS1MegaRando.FogGate;
using DS1MegaRando.Items;
using DS1MegaRando.Settings;
using DS1MegaRando.Data.Enemies;
using DS1MegaRando.Data.Items;
using SoulsFormats;

namespace DS1MegaRando.IO;

/// <summary>
/// Writes randomization results back to DSR game files using SoulsFormats.
/// </summary>
public class GameFileWriter
{
    private readonly GlobalSettings _settings;

    public GameFileWriter(GlobalSettings settings)
    {
        _settings = settings;
    }

    public void WriteAll(
        GameData gameData,
        FogGateResult? fogResult,
        ItemResult? itemResult,
        EnemyResult? enemyResult,
        AnnotationData? ann = null,
        FogGateSettings? fogSettings = null)
    {
        string outDir = string.IsNullOrEmpty(_settings.OutputDirectory)
            ? _settings.GameDirectory
            : _settings.OutputDirectory;

        Directory.CreateDirectory(outDir);

        if (itemResult != null)
            WriteItemParams(outDir, gameData, itemResult, fogSettings);

        if (enemyResult != null && enemyResult.StatModifications.Count > 0)
            WriteEnemyStatParams(outDir, gameData, enemyResult);

        // Patch all bonfire TalkESDs to enable Level Up unconditionally.
        // In vanilla DS1R the Level Up bonfire option is gated behind a flag set
        // only when you first arrive at Firelink via the crow.  With fog gate
        // randomisation (or any run where the normal path to Firelink is disrupted)
        // the flag may never fire, leaving Level Up permanently unavailable.
        // The patched ESD (derived from FogMod) simply removes that condition so
        // Level Up is always present in every bonfire menu in the game.
        PatchBonfireEsds(outDir);

        // Maps are always written: in-memory MSB mutations can come from
        // sources outside of fog/enemy results (e.g. mimic position shuffles).
        WriteMapFiles(outDir, gameData, fogResult, enemyResult, ann, fogSettings);
    }

    // ── Items ──────────────────────────────────────────────────────────────

    private void WriteItemParams(string outDir, GameData gameData, ItemResult itemResult,
        FogGateSettings? fogSettings = null)
    {
        if (gameData.ParamBnd == null) return;

        ApplyLotAssignments(gameData.ItemLotParam, itemResult);
        ApplyShopAssignments(gameData.ShopLineupParam, itemResult);
        ApplyStartingLoadout(gameData.CharaInitParam, itemResult);

        // Vanilla DS1 hands the Estus Flask to the player during Oscar's death scene.
        // Fog-gate / start-area randomization can skip Oscar entirely, so guarantee the
        // flask by putting it directly in every starting class's inventory. (DS1 has no
        // "set charges" mechanism — the flask fills to 5 on the first bonfire rest, and
        // the randomizer enables every bonfire, so this matches vanilla post-Oscar.)
        if (fogSettings?.GuaranteeStartingEstus == true)
            ApplyStartingEstusToClasses(gameData.CharaInitParam);

        // Rewrite modified param bytes back into the BND entries
        RepackParamIntoBnd(gameData.ParamBnd, "ItemLotParam",    gameData.ItemLotParam);
        RepackParamIntoBnd(gameData.ParamBnd, "ShopLineupParam", gameData.ShopLineupParam);
        RepackParamIntoBnd(gameData.ParamBnd, "CharaInitParam",  gameData.CharaInitParam);

        string dest = Path.Combine(outDir, @"param\GameParam\GameParam.parambnd.dcx");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        System.Diagnostics.Debug.WriteLine($"[GameFileWriter] Writing GameParam to: {dest}");
        Console.WriteLine($"[GameFileWriter] Writing GameParam to: {dest}");
        gameData.ParamBnd.Write(dest, gameData.ParamBndCompression);
    }

    private static void ApplyLotAssignments(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;

        foreach (var (rowId, entry) in itemResult.LotAssignments)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;

            TrySetCell(row, "lotItemId01",        entry.ItemId);
            TrySetCell(row, "lotItemCategory01",  entry.Category);
            // The game rolls a slot weighted by lotItemBasePointNN, then delivers
            // lotItemNumNN copies of that slot's item. Without a non-zero weight AND
            // count on slot 1 the lot delivers nothing — no pickup, no interact prompt.
            TrySetCell(row, "lotItemBasePoint01", 100);
            TrySetCell(row, "lotItemNum01",       (byte)1);
            // Zero out slots 2-8 so the lot is clean and slot 1 is the guaranteed roll
            for (int i = 2; i <= 8; i++)
            {
                TrySetCell(row, $"lotItemId{i:D2}", 0);
                TrySetCell(row, $"lotItemCategory{i:D2}", 0);
                TrySetCell(row, $"lotItemBasePoint{i:D2}", 0);
                TrySetCell(row, $"lotItemNum{i:D2}", (byte)0);
            }
        }
    }

    private static void ApplyShopAssignments(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;

        foreach (var (rowId, newItemId) in itemResult.ShopAssignments)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;
            TrySetCell(row, "equipId", newItemId);
        }

        foreach (var (rowId, price) in itemResult.ShopPrices)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;
            TrySetCell(row, "value", price);
        }
    }

    // Minimum stat requirements (STR, DEX, INT, FTH) for every weapon that can appear
    // in a randomized starting loadout. Used when AdjustStatsForWeapons is enabled.
    // These are the one-hand requirements; two-handed weapons in SubRightHand use the
    // two-hand rule: effective STR = floor(baseSTR × 1.5), so minimum base = ceil(req × 2/3).
    private static readonly Dictionary<int, (int Str, int Dex, int Int, int Fth)> WeaponRequirements = new()
    {
        // Daggers
        [ItemIds.Dagger]                = ( 5,  8, 0, 0),
        [ItemIds.ParryingDagger]        = ( 5, 14, 0, 0),
        [ItemIds.GhostBlade]            = ( 5,  0, 0, 0),
        [ItemIds.BanditsKnife]          = ( 6, 12, 0, 0),
        [ItemIds.PriscillasDagger]      = ( 5, 14, 0, 0),
        // Straight Swords
        [ItemIds.Shortsword]            = ( 8, 10, 0, 0),
        [ItemIds.Longsword]             = (10, 10, 0, 0),
        [ItemIds.Broadsword]            = (10, 10, 0, 0),
        [ItemIds.BalderSideSword]       = (10, 14, 0, 0),
        [ItemIds.Darksword]             = (16, 10, 0, 0),
        [ItemIds.DrakeSwrd]             = (12, 12, 0, 0),
        [ItemIds.SilverKnightSword]     = (10, 10, 0, 0),
        [ItemIds.BarbedStraightSword]   = (10, 10, 0, 0),
        [ItemIds.AstoraStraightSword]   = (10, 10, 0, 0),
        [ItemIds.SunlightStraightSword] = (16, 10, 0, 0),
        [ItemIds.Weapon211000]          = (16, 10, 0, 0),
        // Curved Swords
        [ItemIds.Scimitar]              = ( 7, 13, 0, 0),
        [ItemIds.Falchion]              = ( 9, 13, 0, 0),
        [ItemIds.Shotel]                = ( 9, 14, 0, 0),
        [ItemIds.JaggedGhostBlade]      = ( 7,  0, 0, 0),
        [ItemIds.CurvedSword405]        = (11, 13, 0, 0),
        [ItemIds.CurvedSword406]        = (11, 13, 0, 0),
        [ItemIds.PaintingGuardianSword] = (10, 14, 0, 0),
        [ItemIds.GoldTracer]            = ( 9, 20, 0, 0),
        [ItemIds.DarkSilverTracer]      = ( 9, 20, 0, 0),
        // Katanas
        [ItemIds.Uchigatana]            = (14, 14, 0, 0),
        [ItemIds.Katana501]             = (14, 14, 0, 0),
        [ItemIds.Iaito]                 = (14, 14, 0, 0),
        // Thrusting Swords
        [ItemIds.MailBreaker]           = ( 5, 12, 0, 0),
        [ItemIds.Rapier]                = ( 7, 12, 0, 0),
        [ItemIds.Estoc]                 = (10, 12, 0, 0),
        [ItemIds.VelkasRapier]          = ( 8, 16, 0, 0),
        [ItemIds.RicardsRapier]         = ( 8, 16, 0, 0),
        // Axes
        [ItemIds.HandAxe]               = ( 8,  8, 0, 0),
        [ItemIds.BattleAxe]             = (12,  8, 0, 0),
        [ItemIds.CrescentAxe]           = (14, 10, 0, 0),
        [ItemIds.GargoyleTailAxe]       = (14, 14, 0, 0),
        [ItemIds.GolemAxe]              = (26, 10, 0, 0),
        [ItemIds.ButchersKnife]         = (14, 14, 0, 0),
        // Hammers
        [ItemIds.Club]                  = (10,  0, 0, 0),
        [ItemIds.Mace]                  = (12,  0, 0, 0),
        [ItemIds.MorningStar]           = (11,  0, 0, 0),
        [ItemIds.Pickaxe]               = (11, 10, 0, 0),
        [ItemIds.Hammer804]             = (14,  0, 0, 0),
        [ItemIds.ReinforcedClub]        = (12,  0, 0, 0),
        [ItemIds.BlacksmithHammer]      = (14,  0, 0, 0),
        [ItemIds.BlacksmithGiantHammer] = (16,  0, 0, 0),
        [ItemIds.DragonToothHammer]     = (14,  0, 0, 0),
        // Fists
        [ItemIds.Caestus]               = ( 5,  8, 0, 0),
        [ItemIds.Claw]                  = ( 6, 14, 0, 0),
        [ItemIds.Fist903]               = ( 5,  0, 0, 0),
        [ItemIds.DarkHand]              = ( 0,  0, 0, 0),
        // Spears
        [ItemIds.Spear]                 = (11, 10, 0, 0),
        [ItemIds.WingedSpear]           = (13, 15, 0, 0),
        [ItemIds.ChannelersTrident]     = (13, 12, 0, 0),
        [ItemIds.Partizan]              = (12, 10, 0, 0),
        [ItemIds.Spear1004]             = (13, 12, 0, 0),
        [ItemIds.SilverKnightSpear]     = (16, 14, 0, 0),
        // Whips
        [ItemIds.Whip]                  = ( 7, 14, 0, 0),
        [ItemIds.NotchedWhip]           = ( 7, 14, 0, 0),
        // Greatswords
        [ItemIds.BastardSword]          = (16, 10, 0, 0),
        [ItemIds.Claymore]              = (16, 10, 0, 0),
        [ItemIds.ManSerpentGreatsword]  = (22, 10, 0, 0),
        [ItemIds.Flamberge]             = (16, 14, 0, 0),
        [ItemIds.Greatsword304]         = (20, 10, 0, 0),
        [ItemIds.BlackKnightSword]      = (20, 18, 0, 0),
        [ItemIds.BlackKnightGreatsword] = (20, 18, 0, 0),
        [ItemIds.Greatsword309]         = (24, 10, 0, 0),
        [ItemIds.ArtorisasGreatsword]   = (24, 18, 0, 0),
        [ItemIds.AbyssGreatsword]       = (20, 10, 0, 0),
        [ItemIds.Greatsword314]         = (24, 14, 0, 0),
        // Ultra Greatswords
        [ItemIds.Zweihander]            = (24, 14, 0, 0),
        [ItemIds.Greatsword]            = (28, 10, 0, 0),
        [ItemIds.StoneGreatsword]       = (40,  0, 0, 0),
        [ItemIds.UltraGreatsword354]    = (30, 14, 0, 0),
        [ItemIds.MoonlightGreatsword]   = (16,  0, 11, 0),
        // Curved Greatswords / Katanas
        [ItemIds.Murakumo]              = (28, 13, 0, 0),
        [ItemIds.CurvedGreatsword453]   = (20, 20, 0, 0),
        [ItemIds.ChaosBlade]            = (16, 14, 0, 0),
        // Great Axes
        [ItemIds.Greataxe]              = (30,  8, 0, 0),
        [ItemIds.BlackKnightGreataxe]   = (36, 18, 0, 0),
        [ItemIds.DemonsGreataxe]        = (46,  0, 0, 0),
        [ItemIds.GreatAxe753]           = (36,  0, 0, 0),
        // Great Hammers
        [ItemIds.GreatClub]             = (28,  0, 0, 0),
        [ItemIds.GreatHammer851]        = (30,  0, 0, 0),
        [ItemIds.GreatHammer852]        = (36,  0, 0, 0),
        [ItemIds.GreatHammer854]        = (36,  0, 0, 0),
        [ItemIds.LargeClub]             = (26,  0, 0, 0),
        [ItemIds.GreatHammer856]        = (48,  0, 0, 0),
        [ItemIds.Grant]                 = (50,  0, 0, 30),
        // Great Spears
        [ItemIds.Pike]                  = (24, 14, 0, 0),
        [ItemIds.GreatSpear1051]        = (20, 14, 0, 0),
        [ItemIds.GreatSpear1052]        = (12,  0, 14, 0),  // Moonlight Butterfly Horn: INT req, not FTH
        [ItemIds.GreatSpear1054]        = (24, 14, 0, 0),
        // Halberds
        [ItemIds.Halberd]               = (16, 12, 0, 0),
        [ItemIds.BlackKnightHalberd]    = (32, 18, 0, 0),
        [ItemIds.TitaniteCatchPole]     = (16, 14, 0, 0),
        [ItemIds.GargoyleHalberd]       = (16, 12, 0, 0),
        [ItemIds.Halberd1105]           = (16, 12, 0, 0),
        [ItemIds.Lucerne]               = (15, 12, 0, 0),
        [ItemIds.Scythe]                = (14, 12, 0, 0),
        [ItemIds.GreatScythe]           = (14, 14, 0, 0),
        [ItemIds.Scythe1151]            = (16, 14, 0, 0),
        // Bows
        [ItemIds.Shortbow]              = ( 7, 12, 0, 0),
        [ItemIds.Longbow]               = ( 9, 14, 0, 0),
        [ItemIds.CompositeBow]          = (11, 12, 0, 0),
        [ItemIds.BlackBowOfPharis]      = ( 9, 18, 0, 0),
        [ItemIds.Bow1204]               = (11, 12, 0, 0),
        [ItemIds.Bow1205]               = (11, 12, 0, 0),
        [ItemIds.Greatbow]              = (20, 14, 0, 0),
        [ItemIds.Greatbow1251]          = (20, 14, 0, 0),
        [ItemIds.Greatbow1252]          = (20, 14, 0, 0),
        [ItemIds.GoughsGreatbow]        = (20, 14, 0, 0),
        // Crossbows
        [ItemIds.LightCrossbow]         = ( 8, 10, 0, 0),
        [ItemIds.HeavyCrossbow]         = (12, 10, 0, 0),
        [ItemIds.SniperCrossbow]        = (10, 14, 0, 0),
        [ItemIds.Avelyn]                = (11, 14, 0, 0),
        [ItemIds.Crossbow1304]          = ( 8, 10, 0, 0),
        [ItemIds.Crossbow1305]          = ( 8, 10, 0, 0),
        [ItemIds.Crossbow1306]          = ( 8, 10, 0, 0),
        [ItemIds.Crossbow1307]          = ( 8, 10, 0, 0),
        [ItemIds.Crossbow1308]          = ( 8, 10, 0, 0),
        // Shields
        [ItemIds.EastWestShield]      = ( 6,  0, 0, 0),
        [ItemIds.WoodenShield]        = ( 7,  0, 0, 0),
        [ItemIds.LargeLeatherShield]  = ( 7,  0, 0, 0),
        [ItemIds.SmallLeatherShield]  = ( 5,  0, 0, 0),
        [ItemIds.TargetShield]        = ( 8, 11, 0, 0),
        [ItemIds.BucklerShield]       = ( 7, 13, 0, 0),
        [ItemIds.CrackedRoundShield]  = ( 6,  0, 0, 0),
        [ItemIds.LeatherShield]       = ( 6,  0, 0, 0),
        [ItemIds.PlankShield]         = ( 7,  0, 0, 0),
        [ItemIds.HeaterShield]        = ( 8,  0, 0, 0),
        [ItemIds.KnightShield]        = (10,  0, 0, 0),
        [ItemIds.TowerKiteShield]     = (10,  0, 0, 0),
        [ItemIds.GrassCrestShield]    = (10,  0, 0, 0),
        [ItemIds.HollowSoldierShield] = (11,  0, 0, 0),
        [ItemIds.SpiderShield]        = (10,  0, 0, 0),
        [ItemIds.SpikedShield]        = (10, 12, 0, 0),
        [ItemIds.EagleShield]         = (14,  0, 0, 0),
        [ItemIds.TowerShield]         = (22,  0, 0, 0),
    };

    private static void ApplyStartingLoadout(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;

        var loadouts = itemResult.StartingLoadouts;
        if (loadouts.Count == 0) return;

        // DS1 keeps TWO linked CharaInitParam rows per starting class:
        //   3000+i — the character-creator PREVIEW row (drives the gear shown in the menu).
        //   2000+i — the row the new character ACTUALLY spawns with in the Asylum cell.
        // (3000=Warrior … 3009=Deprived; 2000=Warrior … 2009=Deprived.)
        // Vanilla 2000-2009 give only the Straight Sword Hilt, so writing 3000-3009 alone
        // makes the creator show the random gear while the player still spawns with the
        // vanilla hilt. Write each class's rolled loadout to BOTH rows so the preview and
        // the actual spawn match.
        for (int classIdx = 0; classIdx < loadouts.Count; classIdx++)
        {
            var loadout = loadouts[classIdx];
            ApplyLoadoutToClassRow(param, 3000 + classIdx, loadout);
            ApplyLoadoutToClassRow(param, 2000 + classIdx, loadout);

            if (itemResult.AdjustStatsForWeapons)
            {
                BoostStatsForWeapons(param, 3000 + classIdx, loadout);
                BoostStatsForWeapons(param, 2000 + classIdx, loadout);
            }
        }
    }

    private static void ApplyLoadoutToClassRow(PARAM param, int rowId, StartingLoadout loadout)
    {
        var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
        if (row == null) return;

        // Each slot is written unless it's StartingLoadout.Keep (which means "leave the
        // class's vanilla value"). A value of StartingLoadout.Empty (-1) IS written — that
        // explicitly clears the slot (e.g. the "no shield" roll, or an unused off-hand).
        // equip_Armer is the chest slot.
        SetSlotUnlessKeep(row, "equip_Wep_Right",    loadout.RightHand);
        SetSlotUnlessKeep(row, "equip_Wep_Left",     loadout.LeftHand);
        SetSlotUnlessKeep(row, "equip_Subwep_Right", loadout.SubRightHand);
        SetSlotUnlessKeep(row, "equip_Helm",         loadout.Helm);
        SetSlotUnlessKeep(row, "equip_Armer",        loadout.Chest);
        SetSlotUnlessKeep(row, "equip_Gaunt",        loadout.Gauntlets);
        SetSlotUnlessKeep(row, "equip_Leg",          loadout.Legs);
        SetSlotUnlessKeep(row, "equip_Arrow",        loadout.Arrow);
        SetSlotUnlessKeep(row, "equip_Bolt",         loadout.Bolt);
        if (loadout.Arrow != StartingLoadout.Keep && loadout.Arrow != StartingLoadout.Empty)
            TrySetCell(row, "arrowNum", (ushort)50);
        if (loadout.Bolt != StartingLoadout.Keep && loadout.Bolt != StartingLoadout.Empty)
            TrySetCell(row, "boltNum", (ushort)50);
    }

    private static void BoostStatsForWeapons(PARAM param, int rowId, StartingLoadout loadout)
    {
        var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
        if (row == null) return;

        // Resolve actual weapon IDs: if a slot is Keep, read the current
        // value from the row so vanilla weapons are still requirements-checked.
        int rh  = loadout.RightHand    == StartingLoadout.Keep
                    ? GetStatCell(row, "equip_Wep_Right")    : loadout.RightHand;
        int lh  = loadout.LeftHand     == StartingLoadout.Keep
                    ? GetStatCell(row, "equip_Wep_Left")     : loadout.LeftHand;
        int srh = loadout.SubRightHand == StartingLoadout.Keep
                    ? GetStatCell(row, "equip_Subwep_Right") : loadout.SubRightHand;

        int newStr = GetStatCell(row, "baseStr");
        int newDex = GetStatCell(row, "baseDex");
        int newInt = GetStatCell(row, "baseMag");
        int newFth = GetStatCell(row, "baseFai");

        // RightHand and LeftHand: full 1H requirements (shields use 1H req in the offhand).
        foreach (int wepId in new[] { rh, lh })
        {
            if (wepId <= 0) continue;
            if (!WeaponRequirements.TryGetValue(wepId, out var req)) continue;
            newStr = Math.Max(newStr, req.Str);
            newDex = Math.Max(newDex, req.Dex);
            newInt = Math.Max(newInt, req.Int);
            newFth = Math.Max(newFth, req.Fth);
        }

        // SubRightHand is the 2H weapon slot — apply the two-hand STR rule:
        //   effective STR = floor(base × 1.5), so min base = ceil(req × 2/3).
        if (srh > 0 && WeaponRequirements.TryGetValue(srh, out var twoHReq))
        {
            int minStr2H = (int)Math.Ceiling(twoHReq.Str * 2.0 / 3.0);
            newStr = Math.Max(newStr, minStr2H);
            newDex = Math.Max(newDex, twoHReq.Dex);
            newInt = Math.Max(newInt, twoHReq.Int);
            newFth = Math.Max(newFth, twoHReq.Fth);
        }

        // Compute delta against what's currently written (not the load-time base),
        // so the soul-level bump stays consistent with the actual stat change.
        int oldStr = GetStatCell(row, "baseStr");
        int oldDex = GetStatCell(row, "baseDex");
        int oldInt = GetStatCell(row, "baseMag");
        int oldFth = GetStatCell(row, "baseFai");

        int delta = (newStr - oldStr) + (newDex - oldDex)
                  + (newInt - oldInt) + (newFth - oldFth);
        if (delta <= 0) return;

        short currentSl = Convert.ToInt16(row["soulLv"]?.Value ?? (short)1);
        TrySetCell(row, "baseStr", (sbyte)Math.Clamp(newStr, 1, 99));
        TrySetCell(row, "baseDex", (sbyte)Math.Clamp(newDex, 1, 99));
        TrySetCell(row, "baseMag", (sbyte)Math.Clamp(newInt, 1, 99));
        TrySetCell(row, "baseFai", (sbyte)Math.Clamp(newFth, 1, 99));
        TrySetCell(row, "soulLv",  (short)Math.Clamp(currentSl + delta, 1, 713));
    }

    private static int GetStatCell(PARAM.Row row, string name)
    {
        try { return Convert.ToInt32(row[name]?.Value ?? 0); }
        catch { return 0; }
    }

    private static void SetSlotUnlessKeep(PARAM.Row row, string field, int value)
    {
        if (value != StartingLoadout.Keep)
            TrySetCell(row, field, value);
    }

    // ── Starting Estus Flask ────────────────────────────────────────────────

    /// <summary>DS1 goods ID for the Estus Flask.</summary>
    private const int EstusFlaskGoodsId = 200;

    /// <summary>
    /// Puts an Estus Flask in every starting class's inventory (CharaInitParam rows
    /// 2000-2009 spawn rows + 3000-3009 preview rows) so the player always has it,
    /// even when Oscar's flask-granting scene is skipped by fog/start randomization.
    /// </summary>
    private static void ApplyStartingEstusToClasses(PARAM? param)
    {
        if (param == null || param.AppliedParamdef == null) return;
        for (int classIdx = 0; classIdx < 10; classIdx++)
        {
            GiveEstusToClassRow(param, 2000 + classIdx);
            GiveEstusToClassRow(param, 3000 + classIdx);
        }
    }

    private static void GiveEstusToClassRow(PARAM param, int rowId)
    {
        var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
        if (row == null) return;

        // CharaInitParam stores up to 10 starting items. Despite the names, item_numNN
        // (s32) is the goods ID and itemNumN (s8) is the count. Empty slots hold -1.
        // Idempotent: skip if a flask is already present.
        for (int i = 1; i <= 10; i++)
            if (GetCellInt(row, $"item_num{i:D2}") == EstusFlaskGoodsId) return;

        for (int i = 1; i <= 10; i++)
        {
            if (GetCellInt(row, $"item_num{i:D2}") == -1)
            {
                TrySetCell(row, $"item_num{i:D2}", EstusFlaskGoodsId);
                TrySetCell(row, $"itemNum{i}", (sbyte)1);
                return;
            }
        }
    }

    private static int GetCellInt(PARAM.Row row, string name)
    {
        var cell = row[name];
        return cell?.Value == null ? 0 : Convert.ToInt32(cell.Value);
    }

    // ── Enemy stat params ──────────────────────────────────────────────────

    // SpEffectParam row IDs reserved for damage scaling (7950–7990).
    private const int DmgSpEffectBase = 7950;
    private static readonly string[] DmgRateFields =
    {
        "physicsAttackPowerRate", "magicAttackPowerRate",
        "fireAttackPowerRate",    "thunderAttackPowerRate",
    };
    private static readonly string[] NpcSpEffectSlots =
    {
        "spEffectID0", "spEffectID1", "spEffectID2", "spEffectID3",
        "spEffectID4", "spEffectID5", "spEffectID6", "spEffectID7",
    };

    private void WriteEnemyStatParams(string outDir, GameData gameData, EnemyResult enemyResult)
    {
        if (gameData.NpcParam?.AppliedParamdef == null || gameData.ParamBnd == null) return;

        // Build damage SpEffects if SpEffectParam is available
        // Maps damage-multiplier → SpEffect row ID so we reuse rows for identical multipliers
        var dmgSpEffectMap = new Dictionary<float, int>();
        int nextSpId = DmgSpEffectBase;

        foreach (var (npcParamId, mods) in enemyResult.StatModifications)
        {
            var row = gameData.NpcParam.Rows.FirstOrDefault(r => r.ID == npcParamId);
            if (row == null) continue;

            // Scale HP (u32 field)
            if (mods.HP != 1.0f && row["hp"]?.Value is uint baseHp)
                TrySetCell(row, "hp", (uint)(baseHp * mods.HP));

            // Scale poise (superArmorDurability, s16 field)
            if (mods.Poise != 1.0f && row["superArmorDurability"]?.Value is short basePoise)
                TrySetCell(row, "superArmorDurability", (short)Math.Clamp(basePoise * mods.Poise, short.MinValue, short.MaxValue));

            // Scale damage via SpEffectParam — write physicsAttackPowerRate etc.
            if (mods.Damage != 1.0f && gameData.SpEffectParam?.AppliedParamdef != null)
            {
                float dmgMult = mods.Damage;
                if (!dmgSpEffectMap.TryGetValue(dmgMult, out int spId))
                {
                    spId = nextSpId++;
                    dmgSpEffectMap[dmgMult] = spId;

                    // Clone base row 7000 if it exists, otherwise create blank
                    var baseRow = gameData.SpEffectParam.Rows.FirstOrDefault(r => r.ID == 7000);
                    var newRow = new SoulsFormats.PARAM.Row(spId, null, gameData.SpEffectParam.AppliedParamdef);
                    if (baseRow != null)
                        foreach (var cell in baseRow.Cells)
                            newRow[cell.Def.InternalName].Value = cell.Value;

                    foreach (var field in DmgRateFields)
                        if (newRow[field] != null)
                            TrySetCell(newRow, field, dmgMult);

                    gameData.SpEffectParam.Rows.Add(newRow);
                }

                // Assign to the first empty spEffectID slot on the NpcParam row
                foreach (var slot in NpcSpEffectSlots)
                {
                    if (row[slot]?.Value is int slotVal && slotVal <= 0)
                    {
                        TrySetCell(row, slot, spId);
                        break;
                    }
                }
            }
        }

        if (dmgSpEffectMap.Count > 0)
            RepackParamIntoBnd(gameData.ParamBnd, "SpEffectParam", gameData.SpEffectParam!);

        RepackParamIntoBnd(gameData.ParamBnd, "NpcParam", gameData.NpcParam);

        string dest = Path.Combine(outDir, @"param\GameParam\GameParam.parambnd.dcx");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        gameData.ParamBnd.Write(dest, gameData.ParamBndCompression);
    }

    // ── Maps ───────────────────────────────────────────────────────────────

    private void WriteMapFiles(
        string outDir,
        GameData gameData,
        FogGateResult? fogResult,
        EnemyResult? enemyResult,
        AnnotationData? ann,
        FogGateSettings? fogSettings)
    {
        // Apply fog gate changes to MSBs (adds trigger regions + player spawns)
        if (fogResult != null && ann != null)
        {
            string distEventDir = FogGateWriter.DefaultDistEventDir;
            new FogGateWriter(distEventDir).Write(outDir, gameData, fogResult, ann, fogSettings);
        }

        // Patch EMEVD intro sequences for all bosses that received new models.
        // This replaces the old single-boss ApplyAsylumDemonIntroFix; the
        // BossEmevdPatcher covers Asylum + every other boss with an intro event.
        if (enemyResult != null)
            BossEmevdPatcher.PatchAll(
                enemyResult.Placements.Values.SelectMany(x => x),
                _settings.GameDirectory,
                outDir);

        string msbOutDir = Path.Combine(outDir, "map", "MapStudio");
        Directory.CreateDirectory(msbOutDir);

        foreach (var (mapId, msb) in gameData.Maps)
        {
            if (enemyResult != null)
                ApplyEnemyChanges(msb, mapId, enemyResult);

            // Determine the output filename from the source path.
            // DSR uses .msb.dcx; UXM-unpacked installs use .msb.
            // Write back with the same extension so the game can load our changes.
            string sourcePath = gameData.MapSourcePaths.TryGetValue(mapId, out var sp) ? sp : "";
            string fileName = Path.GetFileName(sourcePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{mapId}.msb";

            // If output dir differs from game dir, keep the same filename.
            string destPath = Path.Combine(msbOutDir, fileName);
            msb.Write(destPath);
        }
    }

    private static void ApplyEnemyChanges(MSB1 msb, string mapId, EnemyResult enemyResult)
    {
        if (!enemyResult.Placements.TryGetValue(mapId, out var placements)) return;

        // Primary index: by part index (covers ALL enemies, including EntityID=0).
        // Secondary: by entity ID for any placements that lack a valid part index.
        var byPartIndex = new Dictionary<int, EnemyPlacement>();
        var byEntityId  = new Dictionary<int, EnemyPlacement>();

        foreach (var p in placements)
        {
            if (p.PartIndex >= 0)
                byPartIndex[p.PartIndex] = p;
            else if (p.EntityId > 0)
                byEntityId.TryAdd(p.EntityId, p);
        }

        // Collect model names already declared in this MSB
        var knownModels = msb.Models.Enemies
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var enemyList = msb.Parts.Enemies;
        for (int i = 0; i < enemyList.Count; i++)
        {
            var enemy = enemyList[i];

            // Resolve placement: prefer part-index match, fall back to entity-ID match.
            if (!byPartIndex.TryGetValue(i, out var placement))
            {
                if (enemy.EntityID <= 0 || !byEntityId.TryGetValue(enemy.EntityID, out placement))
                    continue;
            }

            string newModel     = placement.NewModelId;
            bool   modelChanged = !string.Equals(enemy.ModelName, newModel,
                                      StringComparison.OrdinalIgnoreCase);

            // Register the model in the MSB model list if not already present.
            // SibPath must follow FromSoft's editor convention or DSR won't stream
            // the chrbnd into this map at load time — instant crash on area load.
            if (!knownModels.Contains(newModel))
            {
                msb.Models.Enemies.Add(new MSB1.Model.Enemy
                {
                    Name    = newModel,
                    SibPath = $@"N:\FRPG\data\Model\chr\{newModel}\sib\{newModel}.SIB",
                });
                knownModels.Add(newModel);
            }

            enemy.ModelName    = newModel;
            enemy.NPCParamID   = placement.NewNpcParam;
            enemy.ThinkParamID = placement.NewThinkParam;

            if (modelChanged)
            {
                // Use a validated idle anim from the reference data if available,
                // otherwise -1 tells the game to use the model's own default.
                // This prevents T-pose spawns when the original InitAnimID
                // references an animation that only exists on the old model.
                enemy.InitAnimID   = placement.NewInitAnimId;
                enemy.DamageAnimID = -1;
            }

            // Apply per-component positional overrides from boss_overrides.json.
            // Null components keep the vanilla MSB value.
            if (placement.PosX.HasValue || placement.PosY.HasValue || placement.PosZ.HasValue)
                enemy.Position = new Vector3(
                    placement.PosX ?? enemy.Position.X,
                    placement.PosY ?? enemy.Position.Y,
                    placement.PosZ ?? enemy.Position.Z);

            if (placement.RotX.HasValue || placement.RotY.HasValue || placement.RotZ.HasValue)
                enemy.Rotation = new Vector3(
                    placement.RotX ?? enemy.Rotation.X,
                    placement.RotY ?? enemy.Rotation.Y,
                    placement.RotZ ?? enemy.Rotation.Z);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void RepackParamIntoBnd(BND3 bnd, string paramName, PARAM? param)
    {
        if (param == null) return;
        // Only repack params that have a fully applied paramdef.
        // Writing an unapplied PARAM can produce corrupt output that breaks the game.
        if (param.AppliedParamdef == null) return;

        var entry = bnd.Files.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f.Name)
                .Equals(paramName, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return;
        entry.Bytes = param.Write();
    }

    private static void TrySetCell(PARAM.Row row, string name, object value)
    {
        var cell = row[name];
        if (cell != null) cell.Value = value;
    }

    // ── TalkESD patching ───────────────────────────────────────────────────

    /// <summary>
    /// Replaces every vanilla bonfire ESD in every map's talkesdbnd with the
    /// patched version that enables Level Up unconditionally at all bonfires.
    ///
    /// Detection: vanilla bonfire ESDs are all exactly <see cref="VanillaBonfireEsdSize"/>
    /// bytes.  Any ESD entry of that size is a bonfire ESD and gets replaced.
    /// NPC/Firekeeper ESDs have different sizes and are left untouched.
    /// </summary>
    private void PatchBonfireEsds(string outDir)
    {
        byte[] patchedEsd = LoadPatchedBonfireEsd();
        if (patchedEsd.Length == 0) return;

        string gameDir = _settings.GameDirectory;
        string talkSrc  = Path.Combine(gameDir, @"script\talk");
        string talkDest = Path.Combine(outDir,  @"script\talk");

        if (!Directory.Exists(talkSrc)) return;
        Directory.CreateDirectory(talkDest);

        foreach (var srcPath in Directory.GetFiles(talkSrc, "*.talkesdbnd.dcx"))
        {
            string destPath = Path.Combine(talkDest, Path.GetFileName(srcPath));

            var bnd = BND3.Read(srcPath);
            bool changed = false;

            foreach (var entry in bnd.Files)
            {
                if (entry.Bytes.Length == VanillaBonfireEsdSize)
                {
                    entry.Bytes = patchedEsd;
                    changed = true;
                }
            }

            if (changed)
                bnd.Write(destPath, bnd.Compression);
        }
    }

    // Vanilla bonfire ESD is always exactly this size in DSR.
    // Any ESD entry of this size is a bonfire ESD; all others are NPC ESDs.
    private const int VanillaBonfireEsdSize = 23012;

    private static byte[]? _patchedBonfireEsd;

    private static byte[] LoadPatchedBonfireEsd()
    {
        if (_patchedBonfireEsd != null) return _patchedBonfireEsd;

        var asm  = System.Reflection.Assembly.Load("DS1MegaRando.Data");
        string resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("bonfire_patched.esd", StringComparison.OrdinalIgnoreCase))
            ?? "";

        if (string.IsNullOrEmpty(resourceName)) return Array.Empty<byte>();

        using var stream = asm.GetManifestResourceStream(resourceName)!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        _patchedBonfireEsd = bytes;
        return bytes;
    }
}
