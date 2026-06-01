using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Core.Enemies;

public class EnemyPlacer
{
    public List<EnemyPlacement> Place(
        EnemySettings settings,
        List<EnemyEntity> pool,
        Random rng)
    {
        var placements = new List<EnemyPlacement>();

        // Shuffle the replacement pool
        var shuffled = pool.ToList();
        Shuffle(shuffled, rng);

        for (int i = 0; i < pool.Count; i++)
        {
            var target      = pool[i];
            EnemyEntity? replacement = null;

            switch (settings.EnemyPlacementMode)
            {
                case EnemyPlacementMode.CategoryRestricted:
                    // Only replace with enemies of the same or similar category
                    replacement = FindByCategoryBias(shuffled, target, i, settings);
                    break;

                case EnemyPlacementMode.AreaCohesive:
                    // Prefer replacements from the same area
                    replacement = FindByAreaBias(shuffled, target, i);
                    break;

                case EnemyPlacementMode.FullRandom:
                    replacement = shuffled[i % shuffled.Count];
                    break;

                case EnemyPlacementMode.BossOnly:
                    // Only bosses are shuffled, everything else is vanilla
                    replacement = target;
                    break;
            }

            replacement ??= shuffled[i % shuffled.Count];

            // Skip flying enemies in doors unless allowed
            if (!settings.AllowFlyingEnemiesInDoors
                && (replacement.Definition?.CanFly ?? false)
                && target.Area.Contains("door"))
            {
                replacement = target;
            }

            placements.Add(new EnemyPlacement
            {
                EntityId     = target.EntityId,
                OldModelId   = target.ModelId,
                NewModelId   = replacement.ModelId,
                OldNpcParam  = target.NpcParam,
                NewNpcParam  = settings.PreserveAIBehavior ? target.NpcParam : replacement.NpcParam,
                OldThinkParam = target.ThinkParam,
                NewThinkParam = settings.RandomizeEnemyAI ? replacement.ThinkParam : target.ThinkParam,
            });
        }

        return placements;
    }

    private EnemyEntity FindByCategoryBias(
        List<EnemyEntity> shuffled,
        EnemyEntity target,
        int fallbackIndex,
        EnemySettings settings)
    {
        var targetCat = target.Definition?.Category ?? EnemyCategory.Misc;
        var same = shuffled.Where(e => e.Definition?.Category == targetCat).ToList();
        if (same.Count > 0) return same[fallbackIndex % same.Count];

        // Fallback: similar categories
        var similar = shuffled.Where(e => IsSimilarCategory(targetCat, e.Definition?.Category ?? EnemyCategory.Misc)).ToList();
        if (similar.Count > 0) return similar[fallbackIndex % similar.Count];

        return shuffled[fallbackIndex % shuffled.Count];
    }

    private EnemyEntity FindByAreaBias(List<EnemyEntity> shuffled, EnemyEntity target, int fallbackIndex)
    {
        var sameArea = shuffled.Where(e => e.Area == target.Area).ToList();
        if (sameArea.Count > 0) return sameArea[fallbackIndex % sameArea.Count];
        return shuffled[fallbackIndex % shuffled.Count];
    }

    private static bool IsSimilarCategory(EnemyCategory a, EnemyCategory b)
    {
        var group1 = new[] { EnemyCategory.Undead, EnemyCategory.Hollow, EnemyCategory.Skeleton };
        var group2 = new[] { EnemyCategory.Demon, EnemyCategory.Golem, EnemyCategory.Dragon };
        var group3 = new[] { EnemyCategory.Beast, EnemyCategory.Mushroom, EnemyCategory.Slime };
        return (group1.Contains(a) && group1.Contains(b))
            || (group2.Contains(a) && group2.Contains(b))
            || (group3.Contains(a) && group3.Contains(b));
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
