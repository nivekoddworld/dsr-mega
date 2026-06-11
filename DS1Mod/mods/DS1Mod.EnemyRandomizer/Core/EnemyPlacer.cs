using DS1Mod.Core;
using DS1Mod.Modding;

namespace DS1Mod.EnemyRandomizer.Core;

/// <summary>
/// Handles placement for regular and miniboss enemies using the same rules as
/// the DS1MegaRando UI enemy randomizer.
/// </summary>
public class EnemyPlacer
{
    private readonly Action<string>? _log;

    public EnemyPlacer(Action<string>? log = null)
    {
        _log = log;
    }

    public List<EnemyPlacement> Place(
        Config.EnemySettings settings,
        List<EnemyEntity> slots,
        HashSet<string> knownModels,
        Random rng,
        IReadOnlyDictionary<string, HashSet<string>>? areaModels = null,
        HashSet<string>? bossModels = null)
    {
        if (slots.Count == 0)
            return new List<EnemyPlacement>();

        var replacementDefs = EnemyIds.All
            .Where(e => !e.IsIgnored
                     && e.Category != EnemyCategory.NPC
                     && e.Category != EnemyCategory.Misc
                     && knownModels.Contains(e.ModelId))
            .ToList();

        if (replacementDefs.Count == 0)
        {
            Log("WARNING: Empty replacement pool for enemies");
            return new List<EnemyPlacement>();
        }

        var shuffled = replacementDefs.ToList();
        Shuffle(shuffled, rng);

        var placements = new List<EnemyPlacement>();
        for (int i = 0; i < slots.Count; i++)
        {
            var target = slots[i];

            if (settings.EnemyPlacementMode == "BossOnly")
            {
                placements.Add(MakeVanilla(target));
                continue;
            }

            var candidates = GetCandidates(shuffled, target, settings, areaModels);
            if (candidates.Count == 0)
                candidates = shuffled;

            var chosen = candidates[i % candidates.Count];
            placements.Add(MakePlacement(target, chosen, settings));
        }

        return placements;
    }

    private static List<EnemyDef> GetCandidates(
        List<EnemyDef> pool,
        EnemyEntity target,
        Config.EnemySettings settings,
        IReadOnlyDictionary<string, HashSet<string>>? areaModels = null)
    {
        bool blockFlying = !settings.AllowFlyingEnemiesInDoors
                           && IsTightSpace(target.Area);

        IEnumerable<EnemyDef> candidates = pool;

        if (blockFlying)
            candidates = candidates.Where(e => !e.CanFly);

        int maxSize = GetMaxSizeForArea(target.Area, target.MapId);
        candidates = candidates.Where(e => e.Size <= maxSize);

        switch (settings.EnemyPlacementMode)
        {
            case "CategoryRestricted":
            {
                var targetCat = target.Definition?.Category ?? EnemyCategory.Misc;
                var sameGroup = candidates.Where(e => AreSameGroup(targetCat, e.Category)).ToList();
                if (sameGroup.Count > 0)
                    return sameGroup;
                break;
            }
            case "AreaCohesive":
            {
                if (areaModels != null &&
                    !string.IsNullOrEmpty(target.Area) &&
                    areaModels.TryGetValue(target.Area, out var nativeModels))
                {
                    var sameArea = candidates.Where(e => nativeModels.Contains(e.ModelId)).ToList();
                    if (sameArea.Count > 0)
                        return sameArea;
                }
                break;
            }
        }

        return candidates.ToList();
    }

    private static EnemyPlacement MakePlacement(
        EnemyEntity target,
        EnemyDef chosen,
        Config.EnemySettings settings)
    {
        int newNpc = chosen.NpcParamId > 0 ? chosen.NpcParamId : target.NpcParam;
        int newThink = (settings.PreserveAIBehavior || !settings.RandomizeEnemyAI)
            ? target.ThinkParam
            : (chosen.NpcParamId > 0 ? chosen.NpcParamId : target.ThinkParam);

        return new EnemyPlacement
        {
            EntityId = target.EntityId,
            PartIndex = target.PartIndex,
            MapId = target.MapId,
            Area = target.Area,
            OldModelId = target.ModelId,
            NewModelId = chosen.ModelId,
            OldNpcParam = target.NpcParam,
            NewNpcParam = newNpc,
            OldThinkParam = target.ThinkParam,
            NewThinkParam = newThink,
            NewInitAnimId = chosen.DefaultInitAnim,
        };
    }

    private static EnemyPlacement MakeVanilla(EnemyEntity target) => new()
    {
        EntityId = target.EntityId,
        PartIndex = target.PartIndex,
        MapId = target.MapId,
        Area = target.Area,
        OldModelId = target.ModelId,
        NewModelId = target.ModelId,
        OldNpcParam = target.NpcParam,
        NewNpcParam = target.NpcParam,
        OldThinkParam = target.ThinkParam,
        NewThinkParam = target.ThinkParam,
        NewInitAnimId = -1,
    };

    private static bool IsTightSpace(string area) =>
        area.Contains("door", StringComparison.OrdinalIgnoreCase) ||
        area.Contains("corridor", StringComparison.OrdinalIgnoreCase) ||
        area.Contains("narrow", StringComparison.OrdinalIgnoreCase);

    private static int GetMaxSizeForArea(string area, string mapId)
    {
        if (mapId == "m16_00_00_00") return 2;
        if (IsTightSpace(area)) return 2;
        return 4;
    }

    private static bool AreSameGroup(EnemyCategory a, EnemyCategory b)
    {
        if (a == b) return true;
        static bool G1(EnemyCategory x) => x is EnemyCategory.Undead or EnemyCategory.Hollow or EnemyCategory.Skeleton;
        static bool G2(EnemyCategory x) => x is EnemyCategory.Demon or EnemyCategory.Golem or EnemyCategory.Dragon;
        static bool G3(EnemyCategory x) => x is EnemyCategory.Beast or EnemyCategory.Mushroom or EnemyCategory.Slime;
        return (G1(a) && G1(b)) || (G2(a) && G2(b)) || (G3(a) && G3(b));
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void Log(string msg) => _log?.Invoke(msg);
}
