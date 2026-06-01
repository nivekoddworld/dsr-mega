using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.FogGate;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Settings;

namespace DS1MegaRando.Core.Enemies;

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

        var allPlacements = new List<EnemyPlacement>();

        if (settings.RandomizeBosses && settings.EnemyPlacementMode != EnemyPlacementMode.BossOnly)
        {
            Emit($"Randomizing {bosses.Count} boss encounters...");
            var bossRando = new BossRandomizer();
            allPlacements.AddRange(bossRando.Randomize(settings, bosses, rng));
        }
        else if (settings.EnemyPlacementMode == EnemyPlacementMode.BossOnly)
        {
            Emit($"Boss-only mode: shuffling {bosses.Count} bosses...");
            var bossRando = new BossRandomizer();
            allPlacements.AddRange(bossRando.Randomize(settings, bosses, rng));
        }

        if (settings.RandomizeMinibosses)
        {
            Emit($"Randomizing {minibosses.Count} miniboss encounters...");
            var placer = new EnemyPlacer();
            allPlacements.AddRange(placer.Place(settings, minibosses, rng));
        }

        if (settings.EnemyPlacementMode != EnemyPlacementMode.BossOnly)
        {
            Emit($"Randomizing {regular.Count} regular enemies...");
            var placer = new EnemyPlacer();
            allPlacements.AddRange(placer.Place(settings, regular, rng));
        }

        Dictionary<int, (float HP, float Damage)> statMods = new();
        if (settings.ScaleEnemyStats)
        {
            Emit("Scaling enemy stats...");
            var scaler = new EnemyScaler();
            statMods = scaler.Scale(allPlacements, fogResult?.AreaRatios, settings, gameData);
        }

        // Group placements by map
        var byMap = new Dictionary<string, List<EnemyPlacement>>();
        var allEntities = bosses.Concat(minibosses).Concat(regular)
            .Where(e => e.EntityId > 0)
            .GroupBy(e => e.EntityId)
            .ToDictionary(g => g.Key, g => g.First());
        foreach (var p in allPlacements)
        {
            if (!allEntities.TryGetValue(p.EntityId, out var entity)) continue;
            if (!byMap.TryGetValue(entity.MapId, out var list))
                byMap[entity.MapId] = list = new List<EnemyPlacement>();
            list.Add(p);
        }

        var spoiler = allPlacements
            .Select(p =>
            {
                allEntities.TryGetValue(p.EntityId, out var e);
                return (p.OldModelId, p.NewModelId, e?.Area ?? "?");
            })
            .Where(x => x.OldModelId != x.NewModelId)
            .ToList();

        Emit($"Enemy randomization complete. {spoiler.Count} enemies replaced.");

        return new EnemyResult
        {
            Placements       = byMap,
            StatModifications = statMods,
            SpoilerLog       = spoiler,
        };
    }

    private void Emit(string msg) => Log?.Invoke(this, msg);
}
