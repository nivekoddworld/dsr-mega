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
        Random rng,
        BossOverrideConfig? overrides = null)
    {
        if (bossSlots.Count == 0)
            return new List<EnemyPlacement>();

        overrides?.Resolve();

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
        var assignment = BuildAssignment(bossSlots, canonicalPool, settings, rng, overrides);

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
        Random rng,
        BossOverrideConfig? overrides)
    {
        // Canonical vanilla models for derangement (avoid assigning a boss its own slot).
        var vanillaModels = bossSlots
            .Select(b => BossIds.ByEntityId(b.EntityId)?.ModelId ?? b.ModelId)
            .ToList();

        var assignment = new string[bossSlots.Count];

        // ── Pass 1: apply pinned overrides ────────────────────────────────────
        var pinnedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (overrides != null)
        {
            for (int i = 0; i < bossSlots.Count; i++)
            {
                string? forced = overrides.GetForcedModel(bossSlots[i].EntityId);
                if (forced != null)
                {
                    assignment[i] = forced;
                    pinnedModels.Add(forced);
                }
            }
        }

        // Collect indices still needing an assignment.
        var unassigned = Enumerable.Range(0, bossSlots.Count)
            .Where(i => assignment[i] == null)
            .ToList();

        if (unassigned.Count == 0)
            return assignment.ToList();

        // ── Pass 2: build per-slot candidate lists ────────────────────────────
        // Exclude models already consumed by pins when duplicates are off.
        var basePool = settings.AllowDuplicateBosses
            ? pool
            : pool.Where(m => !pinnedModels.Contains(m)).ToList();

        if (basePool.Count == 0)
            basePool = pool; // fallback: ignore pin exclusions if pool is exhausted

        var poolShuffled = basePool.ToList();
        Shuffle(poolShuffled, rng);

        // ── Pass 3: assign remaining slots ────────────────────────────────────
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (settings.AllowDuplicateBosses)
        {
            for (int n = 0; n < unassigned.Count; n++)
                assignment[unassigned[n]] = poolShuffled[n % poolShuffled.Count];
        }
        else
        {
            foreach (int i in unassigned)
            {
                string vanilla = vanillaModels[i];
                var blocked    = overrides?.GetBlockedModels(bossSlots[i].EntityId)
                                 ?? new HashSet<string>();

                // Best: unused, not vanilla, not blocked.
                string? chosen = poolShuffled.FirstOrDefault(m =>
                    !used.Contains(m) &&
                    !blocked.Contains(m) &&
                    !string.Equals(m, vanilla, StringComparison.OrdinalIgnoreCase));

                // Fallback 1: allow reuse if pool is exhausted.
                if (chosen == null)
                    chosen = poolShuffled.FirstOrDefault(m =>
                        !blocked.Contains(m) &&
                        !string.Equals(m, vanilla, StringComparison.OrdinalIgnoreCase));

                // Fallback 2: ignore blocked if every candidate is blocked.
                if (chosen == null)
                    chosen = poolShuffled.FirstOrDefault(m =>
                        !string.Equals(m, vanilla, StringComparison.OrdinalIgnoreCase));

                // Fallback 3: single-boss edge case — accept vanilla.
                chosen ??= poolShuffled[i % poolShuffled.Count];

                assignment[i] = chosen;
                used.Add(chosen);
            }
        }

        return assignment.ToList();
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
