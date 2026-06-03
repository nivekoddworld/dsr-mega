using DS1MegaRando.Annotations;
using DS1MegaRando.FogGate;
using DS1MegaRando.IO;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Enemies;

public class EnemyRandomizer
{
    public event EventHandler<string>? Log;

    public EnemyResult Randomize(
        EnemySettings settings,
        AnnotationData ann,
        GameData gameData,
        FogGateResult? fogResult,
        Random rng)
    {
        Emit("Collecting enemy pool from maps...");
        var pool = new EnemyPool();
        var (bosses, minibosses, regular) = pool.Collect(gameData, ann, settings);

        Emit($"  Found: {bosses.Count} boss slots, {minibosses.Count} minibosses, {regular.Count} regular enemies");
        Emit($"  Known game models: {gameData.KnownEnemyModels.Count}");

        // Apply density filter to regular enemies before placement
        regular = ApplyDensity(regular, settings.EnemyDensityMode, rng);
        if (settings.EnemyDensityMode != EnemyDensityMode.Vanilla)
            Emit($"  After density filter ({settings.EnemyDensityMode}): {regular.Count} regular enemies");

        // Build area → native-model set for AreaCohesive mode.
        // Enemies in each area will be replaced by models that natively appear there.
        var areaModels = BuildAreaModelMap(regular.Concat(minibosses));

        var allPlacements = new List<EnemyPlacement>();

        // BossOnly mode exists specifically to randomize bosses while leaving
        // everything else vanilla, so always run boss randomization in that mode
        // regardless of the standalone RandomizeBosses flag.
        if (settings.RandomizeBosses || settings.EnemyPlacementMode == EnemyPlacementMode.BossOnly)
        {
            Emit($"Randomizing {bosses.Count} boss encounters ({settings.BossRandomizationMode})...");
            var bossRando = new BossRandomizer();
            allPlacements.AddRange(bossRando.Randomize(settings, bosses, gameData.KnownEnemyModels, rng));
        }

        if (settings.RandomizeMinibosses)
        {
            Emit($"Randomizing {minibosses.Count} miniboss encounters...");
            var placer = new EnemyPlacer();
            allPlacements.AddRange(placer.Place(settings, minibosses, gameData.KnownEnemyModels, rng, areaModels));
        }

        if (settings.EnemyPlacementMode != EnemyPlacementMode.BossOnly)
        {
            Emit($"Randomizing {regular.Count} regular enemies...");
            var placer = new EnemyPlacer();
            var regularPlacements = placer.Place(settings, regular, gameData.KnownEnemyModels, rng, areaModels);

            // Shuffle patrol paths among regular enemies if enabled
            if (settings.RandomizePatrolPaths)
            {
                var thinkParams = regularPlacements.Select(p => p.NewThinkParam).ToList();
                Shuffle(thinkParams, rng);
                for (int i = 0; i < regularPlacements.Count; i++)
                    regularPlacements[i].NewThinkParam = thinkParams[i];
                Emit($"Shuffled patrol paths for {regularPlacements.Count} regular enemies.");
            }

            allPlacements.AddRange(regularPlacements);
        }

        Dictionary<int, (float HP, float Damage, float Poise)> statMods = new();
        if (settings.ScaleEnemyStats)
        {
            Emit("Scaling enemy stats...");
            var scaler = new EnemyScaler();
            statMods = scaler.Scale(allPlacements, fogResult?.AreaRatios, settings);
        }

        // Build entity → area lookup for the spoiler log (EntityID=0 enemies show "?").
        var entityArea = bosses.Concat(minibosses).Concat(regular)
            .Where(e => e.EntityId > 0)
            .GroupBy(e => e.EntityId)
            .ToDictionary(g => g.Key, g => g.First().Area);

        // Group placements by map for the writer.
        // Use placement.MapId directly — this works for ALL enemies including
        // those with EntityID=0 that were previously excluded.
        var byMap = new Dictionary<string, List<EnemyPlacement>>();
        foreach (var p in allPlacements)
        {
            if (string.IsNullOrEmpty(p.MapId)) continue;
            if (!byMap.TryGetValue(p.MapId, out var list))
                byMap[p.MapId] = list = new List<EnemyPlacement>();
            list.Add(p);
        }

        var spoiler = allPlacements
            .Select(p => (p.OldModelId, p.NewModelId,
                          entityArea.TryGetValue(p.EntityId, out var a) ? a : "?"))
            .Where(x => x.OldModelId != x.NewModelId)
            .ToList();

        Emit($"Enemy randomization complete. {spoiler.Count} enemies replaced.");

        return new EnemyResult
        {
            Placements        = byMap,
            StatModifications = statMods,
            SpoilerLog        = spoiler,
        };
    }

    private static List<EnemyEntity> ApplyDensity(List<EnemyEntity> enemies, EnemyDensityMode mode, Random rng)
    {
        return mode switch
        {
            // Reduced: randomize ~60% of regular enemies; the rest keep their vanilla model
            EnemyDensityMode.Reduced => enemies.OrderBy(_ => rng.Next())
                                                .Take((int)(enemies.Count * 0.6))
                                                .ToList(),
            // Increased: also include extra enemies from other areas (duplicate 30% of pool)
            EnemyDensityMode.Increased => enemies
                .Concat(enemies.OrderBy(_ => rng.Next()).Take(enemies.Count / 3))
                .ToList(),
            _ => enemies,
        };
    }

    private static Dictionary<string, HashSet<string>> BuildAreaModelMap(IEnumerable<EnemyEntity> entities)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entities)
        {
            if (string.IsNullOrEmpty(e.Area) || string.IsNullOrEmpty(e.ModelId)) continue;
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

    private void Emit(string msg) => Log?.Invoke(this, msg);
}
