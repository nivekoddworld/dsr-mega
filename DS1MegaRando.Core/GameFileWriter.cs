using System.Numerics;
using DS1MegaRando.Annotations;
using DS1MegaRando.Enemies;
using DS1MegaRando.FogGate;
using DS1MegaRando.Items;
using DS1MegaRando.Settings;
using DS1MegaRando.Data.Enemies;
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
            WriteItemParams(outDir, gameData, itemResult);

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

    private void WriteItemParams(string outDir, GameData gameData, ItemResult itemResult)
    {
        if (gameData.ParamBnd == null) return;

        ApplyLotAssignments(gameData.ItemLotParam, itemResult);
        ApplyShopAssignments(gameData.ShopLineupParam, itemResult);
        ApplyStartingLoadout(gameData.CharaInitParam, itemResult);

        // Rewrite modified param bytes back into the BND entries
        RepackParamIntoBnd(gameData.ParamBnd, "ItemLotParam",    gameData.ItemLotParam);
        RepackParamIntoBnd(gameData.ParamBnd, "ShopLineupParam", gameData.ShopLineupParam);
        RepackParamIntoBnd(gameData.ParamBnd, "CharaInitParam",  gameData.CharaInitParam);

        string dest = Path.Combine(outDir, @"param\GameParam\GameParam.parambnd.dcx");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
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
            ApplyLoadoutToClassRow(param, 3000 + classIdx, loadouts[classIdx]);
            ApplyLoadoutToClassRow(param, 2000 + classIdx, loadouts[classIdx]);
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
    }

    private static void SetSlotUnlessKeep(PARAM.Row row, string field, int value)
    {
        if (value != StartingLoadout.Keep)
            TrySetCell(row, field, value);
    }

    // ── Enemy stat params ──────────────────────────────────────────────────

    private void WriteEnemyStatParams(string outDir, GameData gameData, EnemyResult enemyResult)
    {
        if (gameData.NpcParam?.AppliedParamdef == null || gameData.ParamBnd == null) return;

        foreach (var (npcParamId, mods) in enemyResult.StatModifications)
        {
            var row = gameData.NpcParam.Rows.FirstOrDefault(r => r.ID == npcParamId);
            if (row == null) continue;

            // Scale HP (u32 field)
            if (mods.HP != 1.0f && row["hp"]?.Value is uint baseHp)
                TrySetCell(row, "hp", (uint)(baseHp * mods.HP));

            // Scale poise (superArmorDurability, s16 field) when ScalePoiseArmor is on
            if (mods.Poise != 1.0f && row["superArmorDurability"]?.Value is short basePoise)
                TrySetCell(row, "superArmorDurability", (short)Math.Clamp(basePoise * mods.Poise, short.MinValue, short.MaxValue));
        }

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

        // Vanilla DS1 gives the player their starting Estus Flask via Oscar's
        // death scene. A randomized fog gate between the cell and Oscar's cell
        // would lock the flask away. Drop a corpse with the flask in the
        // starter area so the player always gets it before any fog gate.
        if (fogResult != null && fogSettings?.GuaranteeStartingEstus == true)
            ApplyStartingEstus(gameData);

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

    // Ported from FogMod GameDataWriter.cs:798-816. Drops an Estus Flask
    // corpse-treasure in the asylum starter area so the player gets the flask
    // even if a randomized fog gate blocks the path to Oscar.
    private static void ApplyStartingEstus(GameData gameData)
    {
        const string asylumMapId = "m18_01_00_00";
        if (!gameData.Maps.TryGetValue(asylumMapId, out var msb)) return;

        // Idempotent on re-rolls.
        if (msb.Parts.Objects.Any(p => p.Name == "o0500_0050")) return;

        // Any of the asylum starter-treasure objects works as a template — cloning
        // it preserves the existing draw/disp groups and collision linkage so the
        // new corpse spawns and renders correctly on map load.
        var templateIds = new HashSet<int> { 1811613, 1811616, 1811619, 1811622 };
        var template = msb.Parts.Objects.FirstOrDefault(o => templateIds.Contains(o.EntityID));
        if (template == null) return;

        var estus = (MSB1.Part.Object)template.DeepCopy();
        estus.Name       = "o0500_0050";
        estus.ModelName  = "o0500";
        estus.InitAnimID = 50;
        estus.Position   = new Vector3(13.279f, 202.015f, 20.8f);
        estus.Rotation   = new Vector3(0, 0, 0);
        estus.EntityID   = -1;
        msb.Parts.Objects.Add(estus);

        var treasure = new MSB1.Event.Treasure
        {
            Name             = "New Estus",
            EventID          = -1,   // let SoulsFormats assign; avoids vanilla ID collisions
            EntityID         = -1,
            TreasurePartName = "o0500_0050",
        };
        treasure.ItemLots[0] = 1082; // Estus Flask row in ItemLotParam
        msb.Events.Treasures.Add(treasure);

        // o0500 (corpse) should already be in the asylum's model list in vanilla,
        // but add it if missing so the objbnd streams in at load time.
        if (!msb.Models.Objects.Any(m => string.Equals(m.Name, "o0500", StringComparison.OrdinalIgnoreCase)))
        {
            msb.Models.Objects.Add(new MSB1.Model.Object
            {
                Name = "o0500",
                SibPath = @"N:\FRPG\data\Model\obj\o0500\sib\o0500.SIB",
            });
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
