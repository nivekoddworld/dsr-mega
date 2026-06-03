using System.Text.Json;
using System.Text.Json.Serialization;
using DS1MegaRando.Data.Enemies;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Per-component XYZ position / Euler-angle rotation override for a boss slot.
/// Any component left null keeps the vanilla MSB value.
/// Angles are in degrees, matching DSR's MSB convention.
/// </summary>
public sealed class BossPositionOverride
{
    [JsonPropertyName("x")]    public float? X    { get; set; }
    [JsonPropertyName("y")]    public float? Y    { get; set; }
    [JsonPropertyName("z")]    public float? Z    { get; set; }
    [JsonPropertyName("rotX")] public float? RotX { get; set; }
    [JsonPropertyName("rotY")] public float? RotY { get; set; }
    [JsonPropertyName("rotZ")] public float? RotZ { get; set; }
}

/// <summary>
/// User-editable boss placement overrides loaded from boss_overrides.json.
///
/// "pinned"    — slot name → replacement name.
///               That slot always receives exactly that boss, every seed.
///
/// "blocked"   — slot name → list of boss names that must never appear in that slot.
///               The normal random pool is filtered before the slot is assigned.
///
/// "positions" — slot name → { x, y, z, rotX, rotY, rotZ }.
///               Override the spawn position/rotation of whatever boss lands in
///               that slot.  Any component omitted keeps the vanilla MSB value.
///               Angles in degrees; XYZ are world-space coordinates.
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

    [JsonPropertyName("positions")]
    public Dictionary<string, BossPositionOverride> Positions { get; set; } = new();

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

    /// <summary>
    /// Resolved cache: slot entity-ID → positional override.
    /// </summary>
    private Dictionary<int, BossPositionOverride>? _resolvedPositions;

    /// <summary>Call once to convert all name-based config to entity-ID + model-ID lookups.</summary>
    public void Resolve()
    {
        _resolvedPinned    = new Dictionary<int, string>();
        _resolvedBlocked   = new Dictionary<int, HashSet<string>>();
        _resolvedPositions = new Dictionary<int, BossPositionOverride>();

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

        foreach (var (slotName, pos) in Positions)
        {
            var slot = FindBoss(slotName);
            if (slot != null)
                _resolvedPositions[slot.EntityId] = pos;
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

    public BossPositionOverride? GetPositionOverride(int entityId)
    {
        _resolvedPositions ??= new();
        return _resolvedPositions.TryGetValue(entityId, out var p) ? p : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BossDef? FindBoss(string name) =>
        BossIds.All.FirstOrDefault(
            b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
