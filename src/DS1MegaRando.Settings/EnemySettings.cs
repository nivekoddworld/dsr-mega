namespace DS1MegaRando.Settings;

public enum EnemyPlacementMode { FullRandom, CategoryRestricted, AreaCohesive, BossOnly }
public enum BossRandomizationMode { BossForBoss }
public enum EnemyScalingSource { FogGateDepth, VanillaDepth, Fixed }
public enum EnemyDensityMode { Vanilla, Reduced, Increased }

public class EnemySettings
{
    public bool EnableEnemyRandomizer { get; set; } = true;
    public EnemyPlacementMode EnemyPlacementMode { get; set; } = EnemyPlacementMode.CategoryRestricted;
    public bool RandomizeBosses { get; set; } = true;
    public BossRandomizationMode BossRandomizationMode { get; set; } = BossRandomizationMode.BossForBoss;
    public bool AllowDuplicateBosses { get; set; } = false;
    public bool RandomizeMinibosses { get; set; } = true;
    public bool ScaleEnemyStats { get; set; } = true;
    public EnemyScalingSource EnemyScalingSource { get; set; } = EnemyScalingSource.FogGateDepth;
    public float ScaleHPFactor { get; set; } = 1.0f;
    public float ScaleDamageFactor { get; set; } = 1.0f;
    public bool ScalePoiseArmor { get; set; } = false;
    public bool PreserveAIBehavior { get; set; } = true;
    public bool RandomizeEnemyAI { get; set; } = false;
    public bool AllowFlyingEnemiesInDoors { get; set; } = false;
    public bool ProtectImportantNPCs { get; set; } = true;
    public bool RandomizePatrolPaths { get; set; } = false;
    public bool IncludeDLCEnemies { get; set; } = true;
    public EnemyDensityMode EnemyDensityMode { get; set; } = EnemyDensityMode.Vanilla;
    public bool SpoilerLogEnemyPlacements { get; set; } = true;
}
