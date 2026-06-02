using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Enemies;
using SoulsFormats;

namespace DS1MegaRando.Core.Enemies;

public class EnemyEntity
{
    public string    MapId       { get; set; } = "";
    public string    Area        { get; set; } = "";
    public int       EntityId    { get; set; }
    /// <summary>Zero-based index in this map's Parts.Enemies list.</summary>
    public int       PartIndex   { get; set; } = -1;
    public string    ModelId     { get; set; } = "";
    public int       NpcParam    { get; set; }
    public int       ThinkParam  { get; set; }
    public bool      IsBoss      { get; set; }
    public bool      IsMiniboss  { get; set; }
    public EnemyDef? Definition  { get; set; }
    public BossDef?  BossDef     { get; set; }
}

/// <summary>
/// Scans loaded MSB maps and bins every enemy into one of three pools:
///   Bosses    — entity IDs present in BossIds.All (CanReplace=true)
///   Minibosses — enemies whose EnemyDef.Category == Miniboss
///   Regular    — everything else (guards, hollows, beasts …)
///
/// Enemies with IsIgnored=true in EnemyIds, protected NPCs, and multi-part
/// boss entities that cannot be replaced are silently skipped.
/// </summary>
public class EnemyPool
{
    public (List<EnemyEntity> Bosses, List<EnemyEntity> Minibosses, List<EnemyEntity> Regular)
        Collect(GameData gameData, AnnotationData ann, EnemySettings settings)
    {
        var bossLookup   = BossIds.All.ToDictionary(b => b.EntityId);
        var bosses       = new List<EnemyEntity>();
        var minibosses   = new List<EnemyEntity>();
        var regular      = new List<EnemyEntity>();

        // Deduplicate entity IDs globally across all maps.
        // Some entity IDs appear in multiple MSB files (e.g. Sif in both
        // m12_00_00_00 and m12_00_00_01); we only want to randomize each
        // entity once, using its first occurrence.
        var seenEntitiesGlobal = new HashSet<int>();

        foreach (var (mapId, msb) in gameData.Maps)
        {
            // Build collision → area name table for this map
            var colAreaMap = BuildColAreaMap(ann, mapId);

            var enemies = msb.Parts.Enemies;
            for (int partIdx = 0; partIdx < enemies.Count; partIdx++)
            {
                var part = enemies[partIdx];

                if (string.IsNullOrEmpty(part.ModelName)) continue;

                // Bosses are identified by EntityID. For EntityID > 0 we must
                // deduplicate globally (Sif appears in two maps). For EntityID == 0
                // (the vast majority of regular enemies) we track by (mapId, partIdx)
                // so they are never double-counted and can still be randomized.
                bool hasEntityId = part.EntityID > 0;
                if (hasEntityId && !seenEntitiesGlobal.Add(part.EntityID)) continue;

                // Firekeeper must never be randomized — it breaks bonfires.
                // Case-insensitive because some MSBs capitalise model names.
                if (part.ModelName.Equals("c0110", StringComparison.OrdinalIgnoreCase)) continue;

                // Protected important NPCs (Andre, Frampt, Kaathe …)
                if (settings.ProtectImportantNPCs &&
                    (EnemyIds.ProtectedNPCModels.Contains(part.ModelName) ||
                     EnemyIds.NpcModels.Contains(part.ModelName)))
                    continue;

                var def        = EnemyIds.ByModelId(part.ModelName);
                bool isBossSlot = hasEntityId && bossLookup.TryGetValue(part.EntityID, out var bossDef);
                bossDef = isBossSlot ? bossLookup[part.EntityID] : null;

                // Skip boss slots that are marked non-replaceable (BoC, Gwyndolin, etc.)
                if (isBossSlot && bossDef != null && !bossDef.CanReplace) continue;

                // Enemies with broken AI (IsIgnored) stay vanilla in non-boss slots.
                if (!isBossSlot && def != null && def.IsIgnored) continue;

                // Non-catalogued c0xxx/c1xxx NPC models are story characters — protect them.
                if (settings.ProtectImportantNPCs &&
                    def == null && !isBossSlot && IsNpcRangeModel(part.ModelName))
                    continue;

                // DLC exclusion
                string area = colAreaMap.TryGetValue(part.CollisionName ?? "", out var a) ? a : mapId;
                if (!settings.IncludeDLCEnemies && IsDlcArea(mapId)) continue;

                bool isMini = def?.Category == EnemyCategory.Miniboss;

                var entity = new EnemyEntity
                {
                    MapId      = mapId,
                    Area       = area,
                    EntityId   = part.EntityID,
                    PartIndex  = partIdx,
                    ModelId    = part.ModelName,
                    NpcParam   = part.NPCParamID,
                    ThinkParam = part.ThinkParamID,
                    IsBoss     = isBossSlot,
                    IsMiniboss = isMini,
                    Definition = def,
                    BossDef    = bossDef,
                };

                if (isBossSlot)    bosses.Add(entity);
                else if (isMini)   minibosses.Add(entity);
                else               regular.Add(entity);
            }
        }

        return (bosses, minibosses, regular);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static bool IsNpcRangeModel(string model) =>
        model.StartsWith("c0", StringComparison.OrdinalIgnoreCase) ||
        model.StartsWith("c1", StringComparison.OrdinalIgnoreCase);

    private static bool IsDlcArea(string mapId) =>
        mapId == "m12_01_00_00";  // Oolacile township / DLC map

    private static Dictionary<string, string> BuildColAreaMap(AnnotationData ann, string mapId)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // ann.Enemies entries have a Col field like "m10_00_00_00_xxx" and an Area name
        string normalised = mapId.Replace("_", "");
        foreach (var col in ann.Enemies)
        {
            if (col.Col.Replace("_", "").StartsWith(normalised, StringComparison.OrdinalIgnoreCase))
                result[col.Col] = col.Area;
        }
        return result;
    }
}
