using System.Text.Json;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;
using DS1Mod.EnemyRandomizer.Core;
using SoulsFormats;

namespace DS1Mod.EnemyRandomizer;

/// <summary>
/// Standalone enemy randomizer mod. Implements IGamePatcher to randomize
/// boss, miniboss, and regular enemy placements at game launch.
///
/// Configuration is loaded from enemy_config.json in the game directory.
/// Boss overrides come from boss_overrides.json.
/// All randomization is self-contained — no dependencies on other mods.
/// </summary>
public sealed class EnemyRandomizerMod : ModBase, IGamePatcher
{
    public override string Name => "Enemy Randomizer";
    public override string Version => "1.0.0";
    public override string Author => "DS1MegaRando";

    private const string ConfigFileName = "enemy_config.json";
    private const string BossOverridesFileName = "boss_overrides.json";
    private const int DmgSpEffectBase = 7950;

    private static readonly string[] DmgRateFields =
    {
        "physicsAttackPowerRate", "magicAttackPowerRate",
        "fireAttackPowerRate", "thunderAttackPowerRate",
    };

    private static readonly string[] NpcSpEffectSlots =
    {
        "spEffectID0", "spEffectID1", "spEffectID2", "spEffectID3",
        "spEffectID4", "spEffectID5", "spEffectID6", "spEffectID7",
    };

    public void Patch(IPatchContext ctx)
    {
        ctx.Log("[EnemyRandomizer] Loading configuration...");

        string configPath = Path.Combine(ctx.GameDir, ConfigFileName);
        if (!File.Exists(configPath))
        {
            ctx.Log($"[EnemyRandomizer] Config not found. Generating default...");
            Config.ConfigGenerator.WriteDefault(configPath, ctx.Log);
            return;
        }

        Config.EnemyConfig? config;
        try
        {
            string json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<Config.EnemyConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            ctx.Log($"[EnemyRandomizer] Failed to load config: {ex.Message}");
            return;
        }

        if (config?.Enabled != true)
        {
            ctx.Log("[EnemyRandomizer] Disabled in config.");
            return;
        }

        if (config.EnemySettings == null)
        {
            ctx.Log("[EnemyRandomizer] No enemySettings in config.");
            return;
        }

        // Parse seed: if it's a hex number, use that; otherwise hash the string
        int seedValue;
        if (!string.IsNullOrEmpty(config.Seed))
        {
            if (config.Seed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Hex seed
                seedValue = int.Parse(config.Seed.Substring(2), System.Globalization.NumberStyles.HexNumber);
            }
            else if (int.TryParse(config.Seed, out var parsed))
            {
                // Decimal seed
                seedValue = parsed;
            }
            else
            {
                // String seed—hash it
                seedValue = config.Seed.GetHashCode();
            }
        }
        else
        {
            // No seed—use random
            seedValue = new Random().Next();
        }

        try
        {
            ctx.Log($"[EnemyRandomizer] Starting randomization with seed {seedValue:X8}");

            // Load boss overrides if present
            string overridesPath = Path.Combine(ctx.GameDir, BossOverridesFileName);
            var overrides = Config.BossOverrideConfig.LoadFromFile(overridesPath);

            RandomizeEnemies(ctx, config, overrides, seedValue);
            ctx.Log("[EnemyRandomizer] Randomization complete.");
        }
        catch (Exception ex)
        {
            ctx.Log($"[EnemyRandomizer] Randomization failed: {ex.Message}");
            if (ex.InnerException != null)
                ctx.Log($"[EnemyRandomizer] Inner: {ex.InnerException.Message}");
        }
    }

    private void RandomizeEnemies(
        IPatchContext ctx,
        Config.EnemyConfig config,
        Config.BossOverrideConfig? overrides,
        int seedValue)
    {
        var rng = new Random(seedValue);
        var settings = config.EnemySettings!;
        var result = new EnemyResult();

        // Read all game MSBs once upfront
        ctx.Log("[EnemyRandomizer] Reading game data...");
        var msbDir = Path.Combine(ctx.GameDir, "map", "MapStudio");
        if (!Directory.Exists(msbDir))
        {
            ctx.Log("[EnemyRandomizer] MSB directory not found!");
            return;
        }

        var allMsbs = new Dictionary<string, MSB1>(StringComparer.OrdinalIgnoreCase);
        var knownModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allEnemies = new List<EnemyEntity>();
        var mapsByEntity = new Dictionary<int, string>();
        var seenEntitiesGlobal = new HashSet<int>();

        // Scan all maps and keep MSBs in memory
        foreach (string msbPath in Directory.GetFiles(msbDir, "*.msb"))
        {
            string mapId = Path.GetFileNameWithoutExtension(msbPath);
            try
            {
                MSB1 msb = MSB1.Read(msbPath);
                allMsbs[mapId] = msb;

                var pool = new EnemyPool(ctx.Log);
                var (bosses, minibosses, regular) = pool.Collect(msb, mapId, settings, seenEntitiesGlobal);

                allEnemies.AddRange(bosses);
                allEnemies.AddRange(minibosses);
                allEnemies.AddRange(regular);

                // Collect known models
                foreach (var part in msb.Parts.Enemies)
                {
                    if (!string.IsNullOrEmpty(part.ModelName))
                        knownModels.Add(part.ModelName);
                    if (part.EntityID > 0)
                        mapsByEntity[part.EntityID] = mapId;
                }
            }
            catch (Exception ex)
            {
                ctx.Log($"[EnemyRandomizer] Error reading {mapId}: {ex.Message}");
            }
        }

        ctx.Log($"[EnemyRandomizer] Found {allEnemies.Count} total enemies, {knownModels.Count} known models");

        // Build area model map for AreaCohesive mode
        var areaModels = BuildAreaModelMap(allEnemies);

        // Categorize enemies
        var bossEnemies = allEnemies.Where(e => e.IsBoss).ToList();
        var minibossEnemies = allEnemies.Where(e => e.IsMiniboss && !e.IsBoss).ToList();
        var regularEnemies = allEnemies.Where(e => !e.IsBoss && !e.IsMiniboss).ToList();

        // Apply density filter to regular enemies
        regularEnemies = ApplyDensity(regularEnemies, settings, rng);
        if (settings.EnemyDensityMode != "Vanilla")
            ctx.Log($"[EnemyRandomizer] After density filter: {regularEnemies.Count} regular enemies");

        var allPlacements = new List<EnemyPlacement>();

        // Randomize bosses
        if ((settings.RandomizeBosses || settings.EnemyPlacementMode == "BossOnly") && bossEnemies.Count > 0)
        {
            ctx.Log($"[EnemyRandomizer] Randomizing {bossEnemies.Count} bosses...");
            var bossRandomizer = new BossRandomizer();

            var bossPlacements = bossRandomizer.Randomize(settings, bossEnemies, knownModels, rng, overrides);
            allPlacements.AddRange(bossPlacements);
        }

        // Randomize minibosses
        if (settings.RandomizeMinibosses && minibossEnemies.Count > 0)
        {
            ctx.Log($"[EnemyRandomizer] Randomizing {minibossEnemies.Count} minibosses...");
            var placer = new EnemyPlacer(ctx.Log);
            var minibossPlacements = placer.Place(settings, minibossEnemies, knownModels, rng, areaModels);
            allPlacements.AddRange(minibossPlacements);
        }

        // Randomize regular enemies
        if (settings.EnemyPlacementMode != "BossOnly" && regularEnemies.Count > 0)
        {
            ctx.Log($"[EnemyRandomizer] Randomizing {regularEnemies.Count} regular enemies...");
            var placer = new EnemyPlacer(ctx.Log);
            var regularPlacements = placer.Place(settings, regularEnemies, knownModels, rng, areaModels);

            // Shuffle patrol paths if enabled
            if (settings.RandomizePatrolPaths)
            {
                var thinkParams = regularPlacements.Select(p => p.NewThinkParam).ToList();
                Shuffle(thinkParams, rng);
                for (int i = 0; i < regularPlacements.Count; i++)
                    regularPlacements[i].NewThinkParam = thinkParams[i];
                ctx.Log($"[EnemyRandomizer] Shuffled patrol paths for {regularPlacements.Count} enemies");
            }

            allPlacements.AddRange(regularPlacements);
        }

        // Apply boss position overrides
        if (overrides?.Positions != null)
        {
            foreach (var placement in allPlacements.Where(p => p.EntityId > 0))
            {
                if (overrides.Positions.TryGetValue(placement.EntityId, out var pos))
                {
                    placement.PosX = pos.X;
                    placement.PosY = pos.Y;
                    placement.PosZ = pos.Z;
                    placement.RotX = pos.RotX;
                    placement.RotY = pos.RotY;
                    placement.RotZ = pos.RotZ;
                }
            }
        }

        // Group by map
        var byMap = new Dictionary<string, List<EnemyPlacement>>();
        foreach (var p in allPlacements)
        {
            if (!byMap.TryGetValue(p.MapId, out var list))
                byMap[p.MapId] = list = new List<EnemyPlacement>();
            list.Add(p);
        }
        result.Placements = byMap;

        if (settings.ScaleEnemyStats)
        {
            ctx.Log("[EnemyRandomizer] Scaling enemy stats...");
            var scaler = new EnemyScaler();
            result.StatModifications = scaler.Scale(allPlacements, null, settings);
        }

        ctx.Log($"[EnemyRandomizer] Generated {allPlacements.Count} placements");

        // Apply patches to in-memory MSBs and write them all back
        ApplyResultsToGameFiles(ctx, result, allMsbs);
    }

    private void ApplyResultsToGameFiles(IPatchContext ctx, EnemyResult result, Dictionary<string, MSB1> allMsbs)
    {
        if (result.StatModifications.Count > 0)
        {
            ctx.Log("[EnemyRandomizer] Patching enemy stat params...");
            ApplyEnemyStatParams(ctx, result);
        }

        // Apply MSB patches to in-memory MSBs
        if (result.Placements.Count > 0)
        {
            ctx.Log("[EnemyRandomizer] Patching MSB files...");
            foreach (var mapGroup in result.Placements)
            {
                string mapId = mapGroup.Key;
                if (!allMsbs.TryGetValue(mapId, out var msb))
                    continue;

                ApplyMsbPatchesToMsb(msb, mapGroup.Value);
            }
        }

        // Write all MSBs back
        foreach (var (mapId, msb) in allMsbs)
        {
            string msbPath = Path.Combine(ctx.GameDir, "map", "MapStudio", $"{mapId}.msb");
            msb.Write(msbPath);
        }

        // Apply EMEVD patches for bosses
        ctx.Log("[EnemyRandomizer] Patching EMEVD events...");
        ApplyEmevdPatches(ctx, result);

        // Merge ALL enemy effects from all map bundles into CommonEffects
        // This ensures any enemy can be placed anywhere without missing VFX
        ctx.Log("[EnemyRandomizer] Merging all enemy VFX into CommonEffects...");
        BossSfxMerger.MergeAllEnemyEffects(ctx.GameDir, ctx.Log);

        ctx.Log("[EnemyRandomizer] Patching complete");
    }

    private void ApplyEnemyStatParams(IPatchContext ctx, EnemyResult result)
    {
        byte[] defs = ReadEmbedded("paramdef.paramdefbnd.dcx");
        if (defs.Length == 0)
        {
            ctx.Log("[EnemyRandomizer] WARNING: paramdef resource missing; enemy stat scaling skipped.");
            return;
        }

        var g = new GamePatch(ctx);
        g.EditParams(defs, repo =>
        {
            var damageMods = result.StatModifications
                .Select(kvp => kvp.Value.Damage)
                .Where(d => d != 1.0f)
                .Distinct()
                .ToList();

            var dmgSpEffectMap = new Dictionary<float, int>();
            int nextSpId = DmgSpEffectBase;

            if (damageMods.Count > 0)
            {
                repo.Edit("SpEffectParam", sp =>
                {
                    foreach (float dmgMult in damageMods)
                    {
                        int spId = nextSpId++;
                        dmgSpEffectMap[dmgMult] = spId;

                        var baseRow = sp.Rows.FirstOrDefault(r => r.ID == 7000);
                        var newRow = new PARAM.Row(spId, null, sp.AppliedParamdef);
                        if (baseRow != null)
                            foreach (var cell in baseRow.Cells)
                                newRow[cell.Def.InternalName].Value = cell.Value;

                        foreach (var field in DmgRateFields)
                            if (newRow[field] != null)
                                TrySetCell(newRow, field, dmgMult);

                        sp.Rows.RemoveAll(r => r.ID == spId);
                        sp.Rows.Add(newRow);
                        sp.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));
                    }
                });
            }

            repo.Edit("NpcParam", npc =>
            {
                foreach (var (npcParamId, mods) in result.StatModifications)
                {
                    var row = npc.Rows.FirstOrDefault(r => r.ID == npcParamId);
                    if (row == null) continue;

                    if (mods.HP != 1.0f && row["hp"]?.Value is uint baseHp)
                        TrySetCell(row, "hp", (uint)(baseHp * mods.HP));

                    if (mods.Poise != 1.0f && row["superArmorDurability"]?.Value is short basePoise)
                        TrySetCell(row, "superArmorDurability",
                            (short)Math.Clamp(basePoise * mods.Poise, short.MinValue, short.MaxValue));

                    if (mods.Damage != 1.0f && dmgSpEffectMap.TryGetValue(mods.Damage, out int spId))
                    {
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
            });
        });
    }

    private static void TrySetCell(PARAM.Row row, string name, object value)
    {
        var cell = row[name];
        if (cell != null)
            cell.Value = value;
    }

    private static byte[] ReadEmbedded(string logicalName)
    {
        using Stream? s = typeof(EnemyRandomizerMod).Assembly.GetManifestResourceStream(logicalName);
        if (s is null) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private void ApplyMsbPatchesToMsb(MSB1 msb, List<EnemyPlacement> placements)
    {
        // Primary index: by part index (covers ALL enemies, including EntityID=0).
        // Secondary: by entity ID for placements that lack a valid part index.
        var byPartIndex = new Dictionary<int, EnemyPlacement>();
        var byEntityId = new Dictionary<int, EnemyPlacement>();

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

            string newModel = placement.NewModelId;
            bool modelChanged = !string.Equals(enemy.ModelName, newModel, StringComparison.OrdinalIgnoreCase);

            // Register the model in the MSB model list if not already present.
            // SibPath must follow FromSoft's editor convention or DSR won't stream
            // the chrbnd into this map at load time — instant crash on area load.
            if (!knownModels.Contains(newModel))
            {
                msb.Models.Enemies.Add(new SoulsFormats.MSB1.Model.Enemy
                {
                    Name = newModel,
                    SibPath = $@"N:\FRPG\data\Model\chr\{newModel}\sib\{newModel}.SIB",
                });
                knownModels.Add(newModel);
            }

            enemy.ModelName = newModel;
            enemy.NPCParamID = placement.NewNpcParam;
            enemy.ThinkParamID = placement.NewThinkParam;

            if (modelChanged)
            {
                enemy.InitAnimID = placement.NewInitAnimId;
                enemy.DamageAnimID = -1;
            }

            if (placement.PosX.HasValue || placement.PosY.HasValue || placement.PosZ.HasValue)
            {
                var pos = enemy.Position;
                if (placement.PosX.HasValue) pos.X = placement.PosX.Value;
                if (placement.PosY.HasValue) pos.Y = placement.PosY.Value;
                if (placement.PosZ.HasValue) pos.Z = placement.PosZ.Value;
                enemy.Position = pos;
            }

            if (placement.RotX.HasValue || placement.RotY.HasValue || placement.RotZ.HasValue)
            {
                var rot = enemy.Rotation;
                if (placement.RotX.HasValue) rot.X = placement.RotX.Value;
                if (placement.RotY.HasValue) rot.Y = placement.RotY.Value;
                if (placement.RotZ.HasValue) rot.Z = placement.RotZ.Value;
                enemy.Rotation = rot;
            }
        }
    }

    private void ApplyEmevdPatches(IPatchContext ctx, EnemyResult result)
    {
        var byMap = result.Placements
            .SelectMany(kvp => kvp.Value.Select(p => (mapId: kvp.Key, placement: p)))
            .GroupBy(x => x.mapId);

        foreach (var mapGroup in byMap)
        {
            string mapId = mapGroup.Key;
            var placements = mapGroup.Select(x => x.placement).ToList();

            var bossesByEntity = placements.GroupBy(p => p.EntityId)
                .ToDictionary(grp => grp.Key, grp => grp.First());

            var bossesToPatch = BossIds.All
                .Where(b => b.EmevdPatches != null && b.EmevdPatches.Count > 0)
                .Where(b => bossesByEntity.ContainsKey(b.EntityId))
                .Where(b => bossesByEntity[b.EntityId].OldModelId != bossesByEntity[b.EntityId].NewModelId);

            if (!bossesToPatch.Any()) continue;

            var g = new GamePatch(ctx);
            g.EditEmevd(mapId, editor =>
            {
                foreach (var boss in bossesToPatch)
                {
                    if (boss.EmevdPatches == null) continue;
                    editor.ApplyBossPatches(boss.EmevdPatches);
                    ctx.Log($"[EnemyRandomizer] Patched EMEVD for {boss.Name}");
                }
            });
        }
    }

    private static List<EnemyEntity> ApplyDensity(List<EnemyEntity> enemies, Config.EnemySettings settings, Random rng)
    {
        return settings.EnemyDensityMode switch
        {
            "Reduced" => enemies.OrderBy(_ => rng.Next()).Take((int)(enemies.Count * 0.6)).ToList(),
            "Increased" => enemies.Concat(enemies.OrderBy(_ => rng.Next()).Take(enemies.Count / 3)).ToList(),
            _ => enemies,
        };
    }

    private static Dictionary<string, HashSet<string>> BuildAreaModelMap(List<EnemyEntity> entities)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entities.Where(x => !string.IsNullOrEmpty(x.Area) && !string.IsNullOrEmpty(x.ModelId)))
        {
            if (!map.TryGetValue(e.Area, out var set))
                map[e.Area] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add(e.ModelId);
        }
        return map;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
