using DS1MegaRando.IO;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Enemies;

public class EnemyScaler
{
    public Dictionary<int, (float HP, float Damage, float Poise)> Scale(
        List<EnemyPlacement> placements,
        Dictionary<string, (float Health, float Damage)>? areaRatios,
        EnemySettings settings,
        GameData gameData)
    {
        var result = new Dictionary<int, (float, float, float)>();

        var entityArea = BuildEntityAreaMap(gameData);

        foreach (var placement in placements)
        {
            if (!entityArea.TryGetValue(placement.EntityId, out string? area)) continue;

            (float hpRatio, float dmgRatio) = GetScaleRatios(area, areaRatios, settings);
            float finalHP    = hpRatio  * settings.ScaleHPFactor;
            float finalDmg   = dmgRatio * settings.ScaleDamageFactor;
            float finalPoise = settings.ScalePoiseArmor ? hpRatio * settings.ScaleHPFactor : 1.0f;

            int npcParamId = placement.NewNpcParam;
            if (npcParamId > 0)
                result[npcParamId] = (finalHP, finalDmg, finalPoise);
        }

        return result;
    }

    private static (float hp, float dmg) GetScaleRatios(
        string area,
        Dictionary<string, (float Health, float Damage)>? areaRatios,
        EnemySettings settings)
    {
        if (settings.EnemyScalingSource == EnemyScalingSource.Fixed)
            return (settings.ScaleHPFactor, settings.ScaleDamageFactor);

        if (areaRatios != null && areaRatios.TryGetValue(area, out var ratio))
            return ratio;

        return VanillaDepthRatio(area);
    }

    private static (float, float) VanillaDepthRatio(string area) => area switch
    {
        "burg_start" or "firelink" or "asylum"     => (1.0f, 1.0f),
        "depths" or "parish_church" or "burg_upper" => (1.2f, 1.1f),
        "sens" or "anorlondo_main"                  => (1.8f, 1.5f),
        "newlondo_lower" or "totg_lower"            => (2.2f, 1.8f),
        "kiln"                                      => (3.0f, 2.5f),
        _                                           => (1.5f, 1.3f),
    };

    private static Dictionary<int, string> BuildEntityAreaMap(GameData gameData)
    {
        var map = new Dictionary<int, string>();
        foreach (var (mapId, msb) in gameData.Maps)
            foreach (var enemy in msb.Parts.Enemies)
                if (enemy.EntityID > 0)
                    map[enemy.EntityID] = mapId;
        return map;
    }
}
