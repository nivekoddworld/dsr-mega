using DS1Mod.Core;

namespace DS1Mod.EnemyRandomizer.Core;

/// <summary>
/// Represents a single enemy instance in the game world.
/// Populated by EnemyPool from MSB files.
/// </summary>
public class EnemyEntity
{
    public string MapId { get; set; } = "";
    public string Area { get; set; } = "";
    public int EntityId { get; set; }
    public int PartIndex { get; set; } = -1;
    public string ModelId { get; set; } = "";
    public int NpcParam { get; set; }
    public int ThinkParam { get; set; }
    public bool IsBoss { get; set; }
    public bool IsMiniboss { get; set; }
    public EnemyDef? Definition { get; set; }
    public BossDef? BossDef { get; set; }
}

/// <summary>
/// Scans loaded MSB maps and bins every enemy into one of three pools:
/// - Bosses: entity IDs present in BossIds.All (CanReplace=true)
/// - Minibosses: enemies whose EnemyDef.Category == Miniboss
/// - Regular: everything else (guards, hollows, beasts, etc.)
///
/// Enemies with IsIgnored=true in EnemyIds, protected NPCs, and multi-part
/// boss entities that cannot be replaced are silently skipped.
/// </summary>
public class EnemyPool
{
    private readonly Action<string>? _log;

    public EnemyPool(Action<string>? log = null)
    {
        _log = log;
    }

    public (List<EnemyEntity> Bosses, List<EnemyEntity> Minibosses, List<EnemyEntity> Regular)
        Collect(
            SoulsFormats.MSB1 msb,
            string mapId,
            Config.EnemySettings settings,
            HashSet<int>? seenEntitiesGlobal = null)
    {
        seenEntitiesGlobal ??= new HashSet<int>();

        var bosses = new List<EnemyEntity>();
        var minibosses = new List<EnemyEntity>();
        var regular = new List<EnemyEntity>();

        var bossLookup = BossIds.All.ToDictionary(b => b.EntityId);

        var enemies = msb.Parts.Enemies;
        for (int partIndex = 0; partIndex < enemies.Count; partIndex++)
        {
            var part = enemies[partIndex];
            if (string.IsNullOrEmpty(part.ModelName))
                continue;

            bool hasEntityId = part.EntityID > 0;
            if (hasEntityId && !seenEntitiesGlobal.Add(part.EntityID))
                continue;

            if (part.ModelName.Equals("c0110", StringComparison.OrdinalIgnoreCase))
                continue;

            if (settings.ProtectImportantNPCs &&
                (EnemyIds.ProtectedNPCModels.Contains(part.ModelName) ||
                 EnemyIds.NpcModels.Contains(part.ModelName)))
                continue;

            var def = EnemyIds.ByModelId(part.ModelName);
            bool isBossSlot = hasEntityId && bossLookup.TryGetValue(part.EntityID, out var bossDef);
            bossDef = isBossSlot ? bossLookup[part.EntityID] : null;

            if (!isBossSlot && !hasEntityId)
            {
                var modelMatch = BossIds.All.FirstOrDefault(b =>
                    b.CanReplace &&
                    b.ModelId.Equals(part.ModelName, StringComparison.OrdinalIgnoreCase) &&
                    b.MapId.Equals(mapId, StringComparison.OrdinalIgnoreCase));
                if (modelMatch != null)
                {
                    isBossSlot = true;
                    bossDef = modelMatch;
                }
            }

            if (isBossSlot && bossDef != null && !bossDef.CanReplace)
                continue;

            if (!isBossSlot && def != null && def.IsIgnored)
                continue;

            if (settings.ProtectImportantNPCs &&
                def == null && !isBossSlot && IsNpcRangeModel(part.ModelName))
                continue;

            if (!settings.IncludeDLCEnemies && IsDlcArea(mapId))
                continue;

            bool isMini = def?.Category == EnemyCategory.Miniboss;

            var entity = new EnemyEntity
            {
                MapId = mapId,
                Area = mapId,
                EntityId = part.EntityID,
                PartIndex = partIndex,
                ModelId = part.ModelName,
                NpcParam = part.NPCParamID,
                ThinkParam = part.ThinkParamID,
                IsBoss = isBossSlot,
                IsMiniboss = isMini,
                Definition = def,
                BossDef = bossDef,
            };

            if (isBossSlot) bosses.Add(entity);
            else if (isMini) minibosses.Add(entity);
            else regular.Add(entity);
        }

        return (bosses, minibosses, regular);
    }

    private static bool IsNpcRangeModel(string model) =>
        model.StartsWith("c0", StringComparison.OrdinalIgnoreCase) ||
        model.StartsWith("c1", StringComparison.OrdinalIgnoreCase);

    private static bool IsDlcArea(string mapId) =>
        mapId == "m12_01_00_00";

    private void Log(string msg) => _log?.Invoke(msg);
}
