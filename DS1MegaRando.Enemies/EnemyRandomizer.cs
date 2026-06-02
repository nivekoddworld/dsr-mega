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

        var allPlacements = new List<EnemyPlacement>();

        if (settings.RandomizeBosses)
        {
            Emit($"Randomizing {bosses.Count} boss encounters ({settings.BossRandomizationMode})...");
            var bossRando = new BossRandomizer();
            allPlacements.AddRange(bossRando.Randomize(settings, bosses, gameData.KnownEnemyModels, rng));
        }

        if (settings.RandomizeMinibosses)
        {
            Emit($"Randomizing {minibosses.Count} miniboss encounters...");
            var placer = new EnemyPlacer();
            allPlacements.AddRange(placer.Place(settings, minibosses, gameData.KnownEnemyModels, rng));
        }

        if (settings.EnemyPlacementMode != EnemyPlacementMode.BossOnly)
        {
            Emit($"Randomizing {regular.Count} regular enemies...");
            var placer = new EnemyPlacer();
            allPlacements.AddRange(placer.Place(settings, regular, gameData.KnownEnemyModels, rng));
        }

        Dictionary<int, (float HP, float Damage)> statMods = new();
        if (settings.ScaleEnemyStats)
        {
            Emit("Scaling enemy stats...");
            var scaler = new EnemyScaler();
            statMods = scaler.Scale(allPlacements, fogResult?.AreaRatios, settings, gameData);
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

    private void Emit(string msg) => Log?.Invoke(this, msg);
}
