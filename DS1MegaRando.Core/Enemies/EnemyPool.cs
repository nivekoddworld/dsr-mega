using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Enemies;
using SoulsFormats;

namespace DS1MegaRando.Core.Enemies;

public class EnemyEntity
{
    public string MapId       { get; set; } = "";
    public string Area        { get; set; } = "";
    public int    EntityId    { get; set; }
    public string ModelId     { get; set; } = "";
    public int    NpcParam    { get; set; }
    public int    ThinkParam  { get; set; }
    public bool   IsBoss      { get; set; }
    public bool   IsMiniboss  { get; set; }
    public bool   IsProtected { get; set; }
    public EnemyDef? Definition { get; set; }
}

public class EnemyPool
{
    public (List<EnemyEntity> Bosses, List<EnemyEntity> Minibosses, List<EnemyEntity> Regular)
        Collect(GameData gameData, AnnotationData ann, EnemySettings settings)
    {
        var bossDefs  = BossIds.All.ToDictionary(b => b.EntityId);
        var bosses    = new List<EnemyEntity>();
        var minibosses= new List<EnemyEntity>();
        var regular   = new List<EnemyEntity>();

        foreach (var (mapId, msb) in gameData.Maps)
        {
            var colAreaMap  = BuildColAreaMap(ann, mapId);
            var seenEntities = new HashSet<int>();

            foreach (var part in msb.Parts.Enemies)
            {
                if (string.IsNullOrEmpty(part.ModelName)) continue;
                if (part.EntityID < 1000000) continue; // < 1000000 = scripted/trigger entity, not a randomizable enemy
                if (!seenEntities.Add(part.EntityID)) continue; // skip duplicate entity IDs in same map
                // Always protect Firekeeper (c0110) regardless of settings — randomizing her breaks bonfires
                if (part.ModelName == "c0110") continue;

                var def      = EnemyIds.ByModelId(part.ModelName);
                bool isBoss  = bossDefs.ContainsKey(part.EntityID);

                if (settings.ProtectImportantNPCs)
                {
                    if (EnemyIds.ProtectedNPCModels.Contains(part.ModelName)) continue;
                    // Anything outside the catalog is conventionally an NPC in DS1
                    // (c5xxx merchants and quest characters: Solaire, Lautrec, Petrus,
                    // Reah, Logan, Crestfallen Warrior, etc.). Many of these stand or
                    // sit at bonfires; randomizing them puts the wrong model at the
                    // bonfire's position and breaks their questlines.
                    if (def == null && !isBoss) continue;
                }

                string area = colAreaMap.TryGetValue(part.CollisionName ?? "", out var a) ? a : mapId;
                if (area.StartsWith("dlc") && !settings.IncludeDLCEnemies) continue;

                bool isMini  = def?.Category == EnemyCategory.Miniboss;

                var e = new EnemyEntity
                {
                    MapId      = mapId,
                    Area       = area,
                    EntityId   = part.EntityID,
                    ModelId    = part.ModelName,
                    NpcParam   = part.NPCParamID,
                    ThinkParam = part.ThinkParamID,
                    IsBoss     = isBoss,
                    IsMiniboss = isMini,
                    Definition = def,
                };

                if (isBoss)      bosses.Add(e);
                else if (isMini) minibosses.Add(e);
                else             regular.Add(e);
            }
        }

        return (bosses, minibosses, regular);
    }

    private static Dictionary<string, string> BuildColAreaMap(AnnotationData ann, string mapId)
    {
        var result = new Dictionary<string, string>();
        foreach (var col in ann.Enemies)
        {
            if (col.Col.StartsWith(mapId.Replace("_", "")))
                result[col.Col] = col.Area;
        }
        return result;
    }
}
