using System.Text.Json.Serialization;

namespace DS1Mod.EnemyRandomizer.Config;

public enum EnemyPlacementMode { CategoryRestricted, FreeForAll, AreaCohesive, BossOnly }
public enum BossRandomizationMode { BossForBoss, BossForAnything }
public enum EnemyScalingSource { FogGateDepth, VanillaDepth, Fixed }
public enum EnemyDensityMode { Vanilla, Reduced, Increased }

/// <summary>
/// Configuration for the enemy randomizer, loaded from enemy_config.json
/// in the game directory or mods folder.
/// </summary>
public class EnemyConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("seed")]
    public string? Seed { get; set; }

    [JsonPropertyName("enemySettings")]
    public EnemySettings? EnemySettings { get; set; }

    [JsonPropertyName("bossOverrides")]
    public Dictionary<string, object>? BossOverrides { get; set; }
}

/// <summary>
/// Settings that control how enemies are randomized.
/// </summary>
public class EnemySettings
{
    [JsonPropertyName("randomizeBosses")]
    public bool RandomizeBosses { get; set; } = true;

    [JsonPropertyName("allowDuplicateBosses")]
    public bool AllowDuplicateBosses { get; set; } = false;

    [JsonPropertyName("bossRandomizationMode")]
    public BossRandomizationMode BossRandomizationMode { get; set; } = BossRandomizationMode.BossForBoss;

    [JsonPropertyName("randomizeMinibosses")]
    public bool RandomizeMinibosses { get; set; } = true;

    [JsonPropertyName("randomizeRegularEnemies")]
    public bool RandomizeRegularEnemies { get; set; } = false;

    [JsonPropertyName("enemyPlacementMode")]
    public EnemyPlacementMode EnemyPlacementMode { get; set; } = EnemyPlacementMode.BossOnly;

    [JsonPropertyName("scaleEnemyStats")]
    public bool ScaleEnemyStats { get; set; } = true;

    [JsonPropertyName("enemyScalingSource")]
    public EnemyScalingSource EnemyScalingSource { get; set; } = EnemyScalingSource.FogGateDepth;

    [JsonPropertyName("scaleHPFactor")]
    public float ScaleHPFactor { get; set; } = 1.0f;

    [JsonPropertyName("scaleDamageFactor")]
    public float ScaleDamageFactor { get; set; } = 1.0f;

    [JsonPropertyName("scalePoiseArmor")]
    public bool ScalePoiseArmor { get; set; } = false;

    [JsonPropertyName("preserveAIBehavior")]
    public bool PreserveAIBehavior { get; set; } = true;

    [JsonPropertyName("randomizeEnemyAI")]
    public bool RandomizeEnemyAI { get; set; } = false;

    [JsonPropertyName("protectImportantNPCs")]
    public bool ProtectImportantNPCs { get; set; } = true;

    [JsonPropertyName("allowFlyingEnemiesInDoors")]
    public bool AllowFlyingEnemiesInDoors { get; set; } = false;

    [JsonPropertyName("includeDLCEnemies")]
    public bool IncludeDLCEnemies { get; set; } = true;

    [JsonPropertyName("randomizePatrolPaths")]
    public bool RandomizePatrolPaths { get; set; } = false;

    [JsonPropertyName("enemyDensityMode")]
    public EnemyDensityMode EnemyDensityMode { get; set; } = EnemyDensityMode.Vanilla;

    [JsonPropertyName("allowBossesAsRegularEnemies")]
    public bool AllowBossesAsRegularEnemies { get; set; } = false;
}
