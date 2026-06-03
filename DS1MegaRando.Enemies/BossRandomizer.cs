using DS1MegaRando.Settings;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Shuffles the canonical vanilla boss models among the replaceable boss slots.
/// Every slot gets a different boss (derangement when AllowDuplicateBosses is off).
/// NpcParam is always kept from the original slot so scripted HP/death triggers work.
/// The replacement pool is sourced from BossIds.All definitions, not from whatever
/// model happens to be in the MSB, so re-running on already-randomised files cannot
/// contaminate the pool with non-boss models.
/// </summary>
public class BossRandomizer
{
    public List<EnemyPlacement> Randomize(
        EnemySettings settings,
        List<EnemyEntity> bossSlots,
        HashSet<string> knownModels,
        Random rng)
    {
        if (bossSlots.Count == 0)
            return new List<EnemyPlacement>();

        // Build the replacement pool from canonical BossIds definitions so that
        // re-running on already-randomised game files cannot introduce non-boss models.
        // Only include bosses whose model is present in the game's vanilla MSBs and
        // whose slot is marked replaceable.
        var canonicalPool = BossIds.Replaceable
            .Where(b => knownModels.Contains(b.ModelId))
            .Select(b => b.ModelId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (canonicalPool.Count == 0)
            canonicalPool = bossSlots.Select(b => b.ModelId).Distinct().ToList();

        // Build a per-slot assignment list the same length as bossSlots.
        // Each slot is matched to one model from the canonical pool.
        var assignment = BuildAssignment(bossSlots, canonicalPool, settings, rng);

        var placements = new List<EnemyPlacement>();
        for (int i = 0; i < bossSlots.Count; i++)
        {
            var target    = bossSlots[i];
            string newModel = assignment[i];
            var    newDef   = EnemyIds.ByModelId(newModel);

            placements.Add(MakePlacement(target, newModel, newDef, settings));
        }
        return placements;
    }

    // ── Assignment ───────────────────────────────────────────────────────────

    private static List<string> BuildAssignment(
        List<EnemyEntity> bossSlots,
        List<string> pool,
        EnemySettings settings,
        Random rng)
    {
        // For each slot, look up the canonical vanilla model via BossIds so we
        // know which model to avoid placing back in the same slot (derangement).
        var vanillaModels = bossSlots
            .Select(b => BossIds.ByEntityId(b.EntityId)?.ModelId ?? b.ModelId)
            .ToList();

        if (settings.AllowDuplicateBosses)
        {
            // Simple shuffle of the pool; wrap-around if pool is smaller than slots.
            var shuffled = pool.ToList();
            Shuffle(shuffled, rng);
            return Enumerable.Range(0, bossSlots.Count)
                             .Select(i => shuffled[i % shuffled.Count])
                             .ToList();
        }

        // No duplicates: assign each slot a unique model from the pool,
        // ensuring no slot keeps its own vanilla model (derangement).
        // If the pool is smaller than the slot count we allow wrap-around
        // for the overflow slots but still avoid self-assignment.
        var poolShuffled = pool.ToList();
        Shuffle(poolShuffled, rng);

        var assignment = new List<string>(bossSlots.Count);
        var used       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < bossSlots.Count; i++)
        {
            string vanilla = vanillaModels[i];

            // Pick the first unused model that isn't the vanilla one for this slot.
            string? chosen = poolShuffled.FirstOrDefault(
                m => !used.Contains(m) &&
                     !string.Equals(m, vanilla, StringComparison.OrdinalIgnoreCase));

            // Fallback 1: allow reuse if pool is exhausted.
            if (chosen == null)
                chosen = poolShuffled.FirstOrDefault(
                    m => !string.Equals(m, vanilla, StringComparison.OrdinalIgnoreCase));

            // Fallback 2: all pool models equal vanilla (single-boss edge case).
            if (chosen == null)
                chosen = poolShuffled[i % poolShuffled.Count];

            assignment.Add(chosen);
            used.Add(chosen);
        }

        return assignment;
    }

    // ── Placement factory ─────────────────────────────────────────────────────

    private static EnemyPlacement MakePlacement(
        EnemyEntity target,
        string newModelId,
        EnemyDef? newDef,
        EnemySettings settings)
    {
        // Always keep the original slot's NpcParam so scripted boss triggers
        // (health thresholds, death events) fire correctly for this arena.
        int newThink = settings.RandomizeEnemyAI
            ? (newDef?.NpcParamId > 0 ? newDef.NpcParamId : target.ThinkParam)
            : target.ThinkParam;

        return new EnemyPlacement
        {
            EntityId      = target.EntityId,
            PartIndex     = target.PartIndex,
            MapId         = target.MapId,
            Area          = target.Area,
            OldModelId    = target.ModelId,
            NewModelId    = newModelId,
            OldNpcParam   = target.NpcParam,
            NewNpcParam   = target.NpcParam,
            OldThinkParam = target.ThinkParam,
            NewThinkParam = newThink,
            NewInitAnimId = newDef?.DefaultInitAnim ?? -1,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
