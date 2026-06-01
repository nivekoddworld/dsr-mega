namespace DS1MegaRando.Core.Settings;

public enum LogicMode { Normal, Glitched, NoLogic }
public enum EnemyScalingMode { None, ByDistance, Proportional }

public class FogGateSettings
{
    public bool EnableFogGateRandomizer { get; set; } = true;
    public bool RandomizeBossEntrances { get; set; } = true;
    public bool RandomizeWorldEntrances { get; set; } = true;
    public bool RandomizeMinorEntrances { get; set; } = false;
    public bool RandomizeWarps { get; set; } = false;
    public bool RandomizeMajorEntrances { get; set; } = true;
    public bool IncludeDLC { get; set; } = true;
    public bool LordvesselDoorsRandomized { get; set; } = false;
    public bool FixKilnEntrance { get; set; } = true;
    public LogicMode LogicMode { get; set; } = LogicMode.Normal;
    public bool AllowHardSkips { get; set; } = false;
    public bool AllowInstawarps { get; set; } = false;
    public EnemyScalingMode EnemyScalingMode { get; set; } = EnemyScalingMode.ByDistance;
    public float HealthScalingMax { get; set; } = 3.0f;
    public float DamageScalingMax { get; set; } = 2.0f;
    public bool GuaranteeBlacksmithAccess { get; set; } = true;
    public bool RandomizeStartingArea { get; set; } = false;
    public bool SpoilerLogFogConnections { get; set; } = true;
}
