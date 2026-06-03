using DS1MegaRando.Settings;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Handles placement for regular and miniboss enemies (non-boss slots).
/// Builds a replacement pool from the same set of regular enemies, then
/// assigns a replacement to each slot according to the selected mode.
///
/// Enemies with IsIgnored=true (broken AI) are always excluded from the
/// replacement pool — they can appear as vanilla enemies in their original
/// slot but will never be placed elsewhere.
/// </summary>
public class EnemyPlacer
{
    public List<EnemyPlacement> Place(
        EnemySettings settings,
        List<EnemyEntity> slots,
        HashSet<string> knownModels,
        Random rng)
    {
        if (slots.Count == 0) return new List<EnemyPlacement>();

        // Build replacement pool from the catalog, restricted to models that are
        // actually present in the game's vanilla MSBs.  This guarantees every
        // replacement model has a matching chrbnd.dcx file and won't crash on load.
        var replacementDefs = EnemyIds.All
            .Where(e => !e.IsIgnored
                     && e.Category != EnemyCategory.NPC
                     && e.Category != EnemyCategory.Misc
                     && knownModels.Contains(e.ModelId))
            .ToList();

        // Shuffle a working copy so positional selection feels random
        var shuffled = replacementDefs.ToList();
        Shuffle(shuffled, rng);

        var placements = new List<EnemyPlacement>();
        for (int i = 0; i < slots.Count; i++)
        {
            var target = slots[i];

            if (settings.EnemyPlacementMode == EnemyPlacementMode.BossOnly)
            {
                // Non-boss slots stay vanilla
                placements.Add(MakeVanilla(target));
                continue;
            }

            var candidates = GetCandidates(shuffled, target, i, settings);
            if (candidates.Count == 0) candidates = shuffled; // fallback

            var chosen = candidates[i % candidates.Count];
            placements.Add(MakePlacement(target, chosen, settings));
        }


        return placements;
    }

    // ── candidate selection ───────────────────────────────────────────────────

    private static List<EnemyDef> GetCandidates(
        List<EnemyDef> pool,
        EnemyEntity target,
        int index,
        EnemySettings settings)
    {
        // Never place flying enemies in doorways / tight spaces unless allowed
        bool blockFlying = !settings.AllowFlyingEnemiesInDoors
                           && IsTightSpace(target.Area);

        IEnumerable<EnemyDef> candidates = pool;

        if (blockFlying)
            candidates = candidates.Where(e => !e.CanFly);

        // Limit size to avoid colossal enemies in tiny rooms
        int maxSize = GetMaxSizeForArea(target.Area, target.MapId);
        candidates = candidates.Where(e => e.Size <= maxSize);

        switch (settings.EnemyPlacementMode)
        {
            case EnemyPlacementMode.CategoryRestricted:
            {
                var targetCat = target.Definition?.Category ?? EnemyCategory.Misc;
                var sameGroup = candidates.Where(e => AreSameGroup(targetCat, e.Category)).ToList();
                if (sameGroup.Count > 0) return sameGroup;
                break;
            }
            case EnemyPlacementMode.AreaCohesive:
            {
                var sameArea = candidates.Where(e =>
                    EnemyIds.ByModelId(e.ModelId) != null // ensure it's a catalogued model
                ).ToList();
                // Prefer enemies that appear in the same map per reference data locations
                // (we don't have that table here, so fall through to full-random with size filter)
                if (sameArea.Count > 0) return sameArea;
                break;
            }
        }

        return candidates.ToList();
    }

    // ── factories ─────────────────────────────────────────────────────────────

    private static EnemyPlacement MakePlacement(
        EnemyEntity target,
        EnemyDef chosen,
        EnemySettings settings)
    {
        // Always use the replacement model's NpcParam so its attack animations,
        // hitbox shapes, and damage values match the actual model geometry.
        // Using the original slot's NpcParam on a completely different model causes
        // T-pose on attack — the humanoid attack-event IDs don't exist in the new model.
        // We fall back to the target's NpcParam only when no NpcParamId is catalogued.
        //
        // PreserveAIBehavior now only gates ThinkParam (patrol routes / detection logic),
        // NOT NpcParam — preserving NpcParam for regular enemies has no scripting benefit
        // and is the primary cause of the T-pose / no-damage bug.
        int newNpc = chosen.NpcParamId > 0 ? chosen.NpcParamId : target.NpcParam;

        // ThinkParam: keep the original slot's AI routines unless the caller explicitly
        // wants full AI randomization.  This preserves detection range, patrol paths, and
        // combat distance — the model can still fight correctly even in a foreign location.
        int newThink = (settings.PreserveAIBehavior || !settings.RandomizeEnemyAI)
            ? target.ThinkParam
            : (chosen.NpcParamId > 0 ? chosen.NpcParamId : target.ThinkParam);

        return new EnemyPlacement
        {
            EntityId      = target.EntityId,
            PartIndex     = target.PartIndex,
            MapId         = target.MapId,
            Area          = target.Area,
            OldModelId    = target.ModelId,
            NewModelId    = chosen.ModelId,
            OldNpcParam   = target.NpcParam,
            NewNpcParam   = newNpc,
            OldThinkParam = target.ThinkParam,
            NewThinkParam = newThink,
            NewInitAnimId = chosen.DefaultInitAnim,
        };
    }

    private static EnemyPlacement MakeVanilla(EnemyEntity target) => new()
    {
        EntityId      = target.EntityId,
        PartIndex     = target.PartIndex,
        MapId         = target.MapId,
        Area          = target.Area,
        OldModelId    = target.ModelId,
        NewModelId    = target.ModelId,
        OldNpcParam   = target.NpcParam,
        NewNpcParam   = target.NpcParam,
        OldThinkParam = target.ThinkParam,
        NewThinkParam = target.ThinkParam,
        NewInitAnimId = -1,
    };

    // ── area/size helpers ─────────────────────────────────────────────────────

    private static bool IsTightSpace(string area) =>
        area.Contains("door", StringComparison.OrdinalIgnoreCase) ||
        area.Contains("corridor", StringComparison.OrdinalIgnoreCase) ||
        area.Contains("narrow", StringComparison.OrdinalIgnoreCase);

    private static int GetMaxSizeForArea(string area, string mapId)
    {
        // New Londo / narrow indoor maps — restrict large enemies
        if (mapId == "m16_00_00_00") return 2;
        if (IsTightSpace(area))      return 2;
        return 4; // default: large but not colossal
    }

    // ── category grouping ─────────────────────────────────────────────────────

    private static bool AreSameGroup(EnemyCategory a, EnemyCategory b)
    {
        if (a == b) return true;
        // Group 1: humanoid undead
        static bool G1(EnemyCategory x) => x is EnemyCategory.Undead or EnemyCategory.Hollow or EnemyCategory.Skeleton;
        // Group 2: large demons/golems
        static bool G2(EnemyCategory x) => x is EnemyCategory.Demon or EnemyCategory.Golem or EnemyCategory.Dragon;
        // Group 3: creatures
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
}
