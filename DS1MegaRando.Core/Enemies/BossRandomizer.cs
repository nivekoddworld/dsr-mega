using DS1MegaRando.Core.Settings;

namespace DS1MegaRando.Core.Enemies;

public class BossRandomizer
{
    public List<EnemyPlacement> Randomize(
        EnemySettings settings,
        List<EnemyEntity> bosses,
        Random rng)
    {
        if (!settings.RandomizeBosses) return new List<EnemyPlacement>();

        var placements = new List<EnemyPlacement>();
        var pool = settings.BossRandomizationMode == BossRandomizationMode.BossForBoss
            ? bosses.ToList()
            : bosses.ToList(); // could extend to include non-boss enemies

        Shuffle(pool, rng);

        if (!settings.AllowDuplicateBosses)
        {
            // Ensure each boss appears at most once
            var used = new HashSet<string>();
            var filtered = new List<EnemyEntity>();
            foreach (var e in pool)
            {
                if (used.Add(e.ModelId)) filtered.Add(e);
            }
            pool = filtered;
        }

        for (int i = 0; i < bosses.Count; i++)
        {
            var target      = bosses[i];
            var replacement = pool[i % pool.Count];

            placements.Add(new EnemyPlacement
            {
                EntityId      = target.EntityId,
                OldModelId    = target.ModelId,
                NewModelId    = replacement.ModelId,
                OldNpcParam   = target.NpcParam,
                NewNpcParam   = target.NpcParam,   // always keep original — EMEVD scripts reference the boss entity's NpcParam
                OldThinkParam = target.ThinkParam,
                NewThinkParam = settings.RandomizeEnemyAI ? replacement.ThinkParam : target.ThinkParam,
            });
        }

        return placements;
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
