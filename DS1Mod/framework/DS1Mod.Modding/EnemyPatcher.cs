using System.Numerics;
using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Utilities for patching enemy parts in MSB files.
/// Provides reusable functions for mods that need to modify enemy properties.
/// </summary>
public static class EnemyPatcher
{
    /// <summary>
    /// Edit all enemy parts in a map using a delegate.
    /// </summary>
    public static bool PatchEnemyParts(string gameDir, string mapId, Action<List<MSB1.Part.Enemy>> edit)
    {
        string msbPath = Path.Combine(gameDir, "map", "MapStudio", $"{mapId}.msb");
        if (!File.Exists(msbPath)) return false;

        MSB1 msb = MSB1.Read(msbPath);
        edit(msb.Parts.Enemies);
        msb.Write(msbPath);
        return true;
    }

    /// <summary>
    /// Update a single enemy part by entity ID.
    /// </summary>
    public static bool UpdateEnemyByEntityId(
        string gameDir,
        string mapId,
        int entityId,
        Action<MSB1.Part.Enemy> update)
    {
        return PatchEnemyParts(gameDir, mapId, enemies =>
        {
            var enemy = enemies.FirstOrDefault(e => e.EntityID == entityId);
            if (enemy != null)
                update(enemy);
        });
    }

    /// <summary>
    /// Update a single enemy part by index.
    /// </summary>
    public static bool UpdateEnemyByIndex(
        string gameDir,
        string mapId,
        int partIndex,
        Action<MSB1.Part.Enemy> update)
    {
        return PatchEnemyParts(gameDir, mapId, enemies =>
        {
            if (partIndex >= 0 && partIndex < enemies.Count)
                update(enemies[partIndex]);
        });
    }

    /// <summary>
    /// Batch update enemies by entity ID and delegate.
    /// Useful for applying multiple changes to different enemies in one pass.
    /// </summary>
    public static bool BatchUpdateEnemies(
        string gameDir,
        string mapId,
        Dictionary<int, Action<MSB1.Part.Enemy>> updates)
    {
        return PatchEnemyParts(gameDir, mapId, enemies =>
        {
            foreach (var enemy in enemies)
            {
                if (updates.TryGetValue(enemy.EntityID, out var update))
                    update(enemy);
            }
        });
    }

    /// <summary>
    /// Replace an enemy's model and associated parameters.
    /// </summary>
    public static bool ReplaceEnemyModel(
        string gameDir,
        string mapId,
        int entityId,
        string newModelId,
        int? newNpcParamId = null,
        int? newThinkParamId = null,
        int? newInitAnimId = null)
    {
        return UpdateEnemyByEntityId(gameDir, mapId, entityId, enemy =>
        {
            enemy.ModelName = newModelId;
            if (newNpcParamId.HasValue)
                enemy.NPCParamID = newNpcParamId.Value;
            if (newThinkParamId.HasValue)
                enemy.ThinkParamID = newThinkParamId.Value;
            if (newInitAnimId.HasValue)
                enemy.InitAnimID = newInitAnimId.Value;
        });
    }

    /// <summary>
    /// Move an enemy to a new position and/or rotation.
    /// </summary>
    public static bool RepositionEnemy(
        string gameDir,
        string mapId,
        int entityId,
        Vector3? newPosition = null,
        Vector3? newRotation = null)
    {
        return UpdateEnemyByEntityId(gameDir, mapId, entityId, enemy =>
        {
            if (newPosition.HasValue)
                enemy.Position = newPosition.Value;
            if (newRotation.HasValue)
                enemy.Rotation = newRotation.Value;
        });
    }
}
