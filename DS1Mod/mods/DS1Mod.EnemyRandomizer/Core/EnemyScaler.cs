using DS1Mod.Modding;

namespace DS1Mod.EnemyRandomizer.Core;

public class EnemyScaler
{
    public Dictionary<int, (float HP, float Damage, float Poise)> Scale(
        List<EnemyPlacement> placements,
        Dictionary<string, (float Health, float Damage)>? areaRatios,
        Config.EnemySettings settings)
    {
        var result = new Dictionary<int, (float, float, float)>();

        foreach (var placement in placements)
        {
            int npcParamId = placement.NewNpcParam;
            if (npcParamId <= 0) continue;

            (float hpRatio, float dmgRatio) = GetScaleRatios(placement.Area, areaRatios, settings);
            float finalHP = hpRatio;
            float finalDmg = dmgRatio;
            float finalPoise = settings.ScalePoiseArmor ? hpRatio : 1.0f;

            result[npcParamId] = (finalHP, finalDmg, finalPoise);
        }

        return result;
    }

    private static (float hp, float dmg) GetScaleRatios(
        string area,
        Dictionary<string, (float Health, float Damage)>? areaRatios,
        Config.EnemySettings settings)
    {
        if (settings.EnemyScalingSource == Config.EnemyScalingSource.Fixed)
            return (settings.ScaleHPFactor, settings.ScaleDamageFactor);

        if (areaRatios != null && areaRatios.TryGetValue(area, out var ratio))
            return ratio;

        return VanillaDepthRatio(area);
    }

    private static (float, float) VanillaDepthRatio(string area) => area switch
    {
        "burg_start" or "firelink" or "asylum" => (1.0f, 1.0f),
        "depths" or "parish_church" or "burg_upper" => (1.2f, 1.1f),
        "sens" or "anorlondo_main" => (1.8f, 1.5f),
        "newlondo_lower" or "totg_lower" => (2.2f, 1.8f),
        "kiln" => (3.0f, 2.5f),
        _ => (1.5f, 1.3f),
    };
}
