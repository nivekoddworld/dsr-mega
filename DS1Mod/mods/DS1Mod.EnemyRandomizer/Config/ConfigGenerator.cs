using System.Text.Json;
using System.Text.Json.Serialization;

namespace DS1Mod.EnemyRandomizer.Config;

/// <summary>
/// Generates default enemy_config.json if it doesn't exist.
/// </summary>
public static class ConfigGenerator
{
    public static EnemyConfig GenerateDefault()
    {
        return new EnemyConfig
        {
            Enabled = true,
            Seed = new Random().Next().ToString("X8"),
            EnemySettings = new EnemySettings
            {
                RandomizeBosses = true,
                AllowDuplicateBosses = false,
                BossRandomizationMode = BossRandomizationMode.BossForBoss,
                RandomizeMinibosses = true,
                RandomizeRegularEnemies = false,
                EnemyPlacementMode = EnemyPlacementMode.BossOnly,
                ScaleEnemyStats = false,
                EnemyScalingSource = EnemyScalingSource.FogGateDepth,
                ScaleHPFactor = 1.0f,
                ScaleDamageFactor = 1.0f,
                ScalePoiseArmor = false,
                PreserveAIBehavior = true,
                RandomizeEnemyAI = false,
                ProtectImportantNPCs = true,
                AllowFlyingEnemiesInDoors = false,
                IncludeDLCEnemies = true,
                RandomizePatrolPaths = false,
                EnemyDensityMode = EnemyDensityMode.Vanilla,
                AllowBossesAsRegularEnemies = false,
            },
            BossOverrides = null,
        };
    }

    public static void WriteDefault(string configPath, Action<string>? log = null)
    {
        try
        {
            var config = GenerateDefault();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, json);
            log?.Invoke($"[ConfigGenerator] Created default config at {configPath}");
        }
        catch (Exception ex)
        {
            log?.Invoke($"[ConfigGenerator] Failed to write config: {ex.Message}");
        }
    }
}
