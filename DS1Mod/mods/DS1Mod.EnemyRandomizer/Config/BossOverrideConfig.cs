using System.Text.Json;
using System.Text.Json.Serialization;
using DS1Mod.Core;

namespace DS1Mod.EnemyRandomizer.Config;

/// <summary>
/// Configuration for boss-specific overrides: pinned replacements, blocked combos, spawn positions.
/// Loaded from boss_overrides.json in the game directory.
/// </summary>
public class BossOverrideConfig
{
    [JsonPropertyName("pinned")]
    public Dictionary<string, string>? Pinned { get; set; }

    [JsonPropertyName("blocked")]
    public List<(string, string)>? Blocked { get; set; }

    [JsonPropertyName("positions")]
    public Dictionary<int, BossPosition>? Positions { get; set; }

    public static BossOverrideConfig? LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BossOverrideConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolve is a no-op in the mod version. Overrides are already keyed by EntityID or ModelID.
    /// </summary>
    public void Resolve()
    {
    }

    /// <summary>
    /// Get the forced model for a boss slot (pinned replacement).
    /// </summary>
    public string? GetForcedModel(int entityId)
    {
        var boss = BossIds.ByEntityId(entityId);
        if (boss == null) return null;
        return GetPinned(entityId);
    }

    /// <summary>
    /// Get blocked models for a boss slot.
    /// </summary>
    public HashSet<string> GetBlockedModels(int entityId)
    {
        var boss = BossIds.ByEntityId(entityId);
        if (boss == null) return new HashSet<string>();

        if (Blocked == null) return new HashSet<string>();

        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (oldModel, newModel) in Blocked)
        {
            if (oldModel == boss.ModelId)
                blocked.Add(newModel);
        }
        return blocked;
    }

    /// <summary>
    /// Check if a boss model is pinned to a specific replacement.
    /// </summary>
    public string? GetPinned(int entityId)
    {
        if (Pinned == null) return null;

        var boss = BossIds.ByEntityId(entityId);
        if (boss == null) return null;

        if (Pinned.TryGetValue(boss.ModelId, out var replacement))
            return replacement;

        return null;
    }

    /// <summary>
    /// Check if a combination is blocked.
    /// </summary>
    public bool IsBlocked(string oldModel, string newModel)
    {
        if (Blocked == null) return false;
        return Blocked.Any(b => b.Item1 == oldModel && b.Item2 == newModel);
    }

    /// <summary>
    /// Get position override for a boss entity.
    /// </summary>
    public BossPosition? GetPosition(int entityId)
    {
        if (Positions == null) return null;
        Positions.TryGetValue(entityId, out var pos);
        return pos;
    }

    public BossPosition? GetPositionOverride(int entityId) => GetPosition(entityId);
}

public class BossPosition
{
    [JsonPropertyName("x")]
    public float? X { get; set; }

    [JsonPropertyName("y")]
    public float? Y { get; set; }

    [JsonPropertyName("z")]
    public float? Z { get; set; }

    [JsonPropertyName("rotX")]
    public float? RotX { get; set; }

    [JsonPropertyName("rotY")]
    public float? RotY { get; set; }

    [JsonPropertyName("rotZ")]
    public float? RotZ { get; set; }
}
