using DS1MegaRando.Settings;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Randomizes which model fills each boss slot.
///
/// BossForBoss  — shuffles the vanilla boss models among the boss slots.
///                Every slot gets a different boss.  NpcParam is kept from
///                the original slot so scripted HP/death triggers still work.
///
/// FreeForAll   — any non-ignored enemy (boss or regular) can fill any boss
///                slot.  NpcParam comes from the replacement model so the
///                enemy's stats feel authentic.  If a regular enemy fills a
///                boss slot it uses its own AI and animations.
/// </summary>
public class BossRandomizer
{
    public List<EnemyPlacement> Randomize(
        EnemySettings settings,
        List<EnemyEntity> bossSlots,
        HashSet<string> knownModels,
        Random rng)
    {
        if (!settings.RandomizeBosses || bossSlots.Count == 0)
            return new List<EnemyPlacement>();

        return settings.BossRandomizationMode == BossRandomizationMode.FreeForAll
            ? RandomizeFreeForAll(settings, bossSlots, knownModels, rng)
            : RandomizeBossForBoss(settings, bossSlots, rng);
    }

    // ── BossForBoss ──────────────────────────────────────────────────────────

    private static List<EnemyPlacement> RandomizeBossForBoss(
        EnemySettings settings,
        List<EnemyEntity> bossSlots,
        Random rng)
    {
        // Build a pool of boss models from the occupied slots
        var replacementPool = bossSlots.Select(b => b.ModelId).ToList();
        Shuffle(replacementPool, rng);

        if (!settings.AllowDuplicateBosses)
            replacementPool = Deduplicate(replacementPool);

        var placements = new List<EnemyPlacement>();
        for (int i = 0; i < bossSlots.Count; i++)
        {
            var target       = bossSlots[i];
            string newModel  = replacementPool[i % replacementPool.Count];
            var    newDef    = EnemyIds.ByModelId(newModel);

            placements.Add(MakePlacement(target, newModel, newDef,
                keepOriginalNpc: true, settings));
        }
        return placements;
    }

    // ── FreeForAll ────────────────────────────────────────────────────────────

    private static List<EnemyPlacement> RandomizeFreeForAll(
        EnemySettings settings,
        List<EnemyEntity> bossSlots,
        HashSet<string> knownModels,
        Random rng)
    {
        // Build free pool: boss-capable or large enemies, restricted to models
        // that exist in the game's vanilla MSBs (chrbnd files guaranteed).
        var freePool = EnemyIds.All
            .Where(e => !e.IsIgnored
                     && e.Category != EnemyCategory.NPC
                     && e.Category != EnemyCategory.Misc
                     && (e.BossCapable || e.Size >= 2)
                     && knownModels.Contains(e.ModelId))
            .ToList();

        // Also add vanilla boss models for this run from the actual slots
        var bossDefs = bossSlots
            .Select(b => EnemyIds.ByModelId(b.ModelId))
            .Where(d => d != null)
            .Cast<EnemyDef>()
            .ToList();

        var combined = freePool
            .Concat(bossDefs.Where(d => !freePool.Any(p => p.ModelId == d.ModelId)))
            .ToList();

        // Pre-filter invalid giant enemies for tiny arenas
        var placements = new List<EnemyPlacement>();
        foreach (var target in bossSlots)
        {
            var candidates = FilterCandidatesForSlot(combined, target, settings);
            if (candidates.Count == 0)
                candidates = combined; // fallback: ignore restrictions

            var chosen = candidates[rng.Next(candidates.Count)];
            // In FreeForAll: use the replacement's NpcParam so stats feel authentic
            placements.Add(MakePlacement(target, chosen.ModelId, chosen,
                keepOriginalNpc: false, settings));
        }
        return placements;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static EnemyPlacement MakePlacement(
        EnemyEntity target,
        string newModelId,
        EnemyDef? newDef,
        bool keepOriginalNpc,
        EnemySettings settings)
    {
        int newNpc = keepOriginalNpc
            ? target.NpcParam
            : (newDef?.NpcParamId > 0 ? newDef.NpcParamId : target.NpcParam);

        int newThink = settings.RandomizeEnemyAI
            ? (newDef?.NpcParamId > 0 ? newDef.NpcParamId : target.ThinkParam)
            : target.ThinkParam;

        return new EnemyPlacement
        {
            EntityId       = target.EntityId,
            PartIndex      = target.PartIndex,
            MapId          = target.MapId,
            Area           = target.Area,
            OldModelId     = target.ModelId,
            NewModelId     = newModelId,
            OldNpcParam    = target.NpcParam,
            NewNpcParam    = newNpc,
            OldThinkParam  = target.ThinkParam,
            NewThinkParam  = newThink,
            NewInitAnimId  = newDef?.DefaultInitAnim ?? -1,
        };
    }

    /// <summary>
    /// Returns the subset of <paramref name="pool"/> that is safe for
    /// <paramref name="slot"/>.  Excludes enemies that are too large for
    /// indoor/tight arenas, and flying enemies if the arena has no sky.
    /// </summary>
    private static List<EnemyDef> FilterCandidatesForSlot(
        List<EnemyDef> pool,
        EnemyEntity slot,
        EnemySettings settings)
    {
        int maxSize = GetMaxSizeForArena(slot.BossDef?.MapId ?? slot.MapId,
                                         slot.BossDef?.Name ?? "");
        bool noFly = !settings.AllowFlyingEnemiesInDoors
                     && IsIndoorArena(slot.BossDef?.MapId ?? slot.MapId);

        return pool.Where(e =>
            e.Size <= maxSize &&
            !(noFly && e.CanFly)).ToList();
    }

    /// <summary>
    /// Maximum enemy size that is safe to place in a given boss arena.
    /// Default is 4 (large but not colossal).  Tight arenas like Capra Demon
    /// or the Four Kings chamber get tighter limits.
    /// </summary>
    private static int GetMaxSizeForArena(string mapId, string bossName)
    {
        // Capra Demon / small courtyards — only medium-sized enemies fit
        if (bossName.Contains("Capra")) return 2;
        // Painted World, New Londo (small corridors)
        if (mapId == "m11_00_00_00" || mapId == "m16_00_00_00") return 3;
        // Default: allow up to size 4
        return 4;
    }

    private static bool IsIndoorArena(string mapId) =>
        mapId is "m10_00_00_00" or "m10_01_00_00" or "m10_02_00_00"
               or "m13_00_00_00" or "m13_01_00_00" or "m14_01_00_00"
               or "m15_01_00_00" or "m16_00_00_00" or "m17_00_00_00";

    private static List<T> Deduplicate<T>(List<T> list)
    {
        var seen   = new HashSet<T>();
        var result = new List<T>();
        foreach (var item in list)
            if (seen.Add(item!)) result.Add(item);
        return result;
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
