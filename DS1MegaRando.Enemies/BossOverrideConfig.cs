using System.Text.Json;
using System.Text.Json.Serialization;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Enemies;

/// <summary>
/// User-editable boss placement overrides loaded from boss_overrides.json.
///
/// "pinned"  — slot name → replacement name.
///             That slot always receives exactly that boss, every seed.
///
/// "blocked" — slot name → list of boss names that must never appear in that slot.
///             The normal random pool is filtered before the slot is assigned.
///
/// Slot names and replacement names must match the Name field in BossIds.All
/// (case-insensitive).  Unknown names are silently ignored.
/// </summary>
public sealed class BossOverrideConfig
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true,
    };

    [JsonPropertyName("pinned")]
    public Dictionary<string, string> Pinned { get; set; } = new();

    [JsonPropertyName("blocked")]
    public Dictionary<string, List<string>> Blocked { get; set; } = new();

    // ── File paths ────────────────────────────────────────────────────────────

    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "boss_overrides.json");

    public static BossOverrideConfig LoadFrom(string path)
    {
        if (!File.Exists(path))
            return new BossOverrideConfig();
        try
        {
            return JsonSerializer.Deserialize<BossOverrideConfig>(
                       File.ReadAllText(path), _jsonOpts)
                   ?? new BossOverrideConfig();
        }
        catch
        {
            return new BossOverrideConfig();
        }
    }

    // ── Resolved lookups ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolved cache: slot entity-ID → forced replacement model ID.
    /// Null if the slot has no pin, or if the pin target name is unknown.
    /// </summary>
    private Dictionary<int, string>? _resolvedPinned;

    /// <summary>
    /// Resolved cache: slot entity-ID → set of blocked model IDs.
    /// </summary>
    private Dictionary<int, HashSet<string>>? _resolvedBlocked;

    /// <summary>Call once to convert all name-based config to entity-ID + model-ID lookups.</summary>
    public void Resolve()
    {
        _resolvedPinned  = new Dictionary<int, string>();
        _resolvedBlocked = new Dictionary<int, HashSet<string>>();

        foreach (var (slotName, targetName) in Pinned)
        {
            var slot   = FindBoss(slotName);
            var target = FindBoss(targetName);
            if (slot != null && target != null)
                _resolvedPinned[slot.EntityId] = target.ModelId;
        }

        foreach (var (slotName, blockedNames) in Blocked)
        {
            var slot = FindBoss(slotName);
            if (slot == null) continue;

            var models = blockedNames
                .Select(FindBoss)
                .Where(b => b != null)
                .Select(b => b!.ModelId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (models.Count > 0)
                _resolvedBlocked[slot.EntityId] = models;
        }
    }

    public string? GetForcedModel(int entityId)
    {
        _resolvedPinned ??= new();
        return _resolvedPinned.TryGetValue(entityId, out var m) ? m : null;
    }

    public HashSet<string> GetBlockedModels(int entityId)
    {
        _resolvedBlocked ??= new();
        return _resolvedBlocked.TryGetValue(entityId, out var s)
            ? s
            : new HashSet<string>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BossDef? FindBoss(string name) =>
        BossIds.All.FirstOrDefault(
            b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
