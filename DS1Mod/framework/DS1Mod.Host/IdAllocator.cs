using System.Text.Json;
using System.Text.Json.Serialization;

namespace DS1Mod.Host;

/// <summary>
/// Manages deterministic ID allocation across mods to prevent conflicts.
/// Persists allocations to disk so the same mod always gets the same IDs
/// across runs (critical for save-game compatibility).
/// </summary>
internal sealed class IdAllocator
{
    // Schema for allocations.json
    private sealed record AllocationSpace
    {
        [JsonPropertyName("base")]
        public int Base { get; set; }

        [JsonPropertyName("claimed")]
        public Dictionary<string, List<(int Start, int End)>> Claimed { get; set; } = new();
    }

    private sealed record AllocationsRoot
    {
        [JsonPropertyName("allocations")]
        public Dictionary<string, AllocationSpace> Allocations { get; set; } = new();
    }

    private readonly string _allocationsPath;
    private readonly Dictionary<string, AllocationSpace> _spaces = new();
    private string? _currentMod;

    // Default starting points for each space
    private static readonly Dictionary<string, int> DefaultBases = new()
    {
        // PARAM rows
        { "EquipParamGoods", 8000 },
        { "ItemLotParam", 8500 },
        { "SpEffectParam", 9000 },
        { "ItemEquipParamGoods", 8000 },

        // FMG entries
        { "EventText", 6900000 },
        { "ItemName", 8000 },
        { "ItemDescription", 8000 },
        { "ItemLongDesc", 8000 },

        // Event flags (per-map) — derive base from map ID if possible
        { "EventFlags_m18_01", 11819000 },
        { "EventFlags_m10_01", 11010900 },
        { "EventFlags_m14_01", 11415000 },
        { "EventFlags_m12_01", 11215000 },

        // EMEVD events (per-map)
        { "EmevdEvents_m18_01", 11819000 },
        { "EmevdEvents_m10_01", 11010900 },
        { "EmevdEvents_m14_01", 11415000 },
        { "EmevdEvents_m12_01", 11215000 },

        // MSB entity IDs (per-map) — use high unused ranges
        { "MsbEntities_m18_01", 1811000 },
        { "MsbEntities_m10_01", 1010000 },
        { "MsbEntities_m14_01", 1410000 },

        // Item once-only flags (global)
        { "ItemObtainedFlags", 50000000 },

        // Generic fallback for unknown spaces
        { "__default__", 100000 },
    };

    public IdAllocator(string gameDir)
    {
        _allocationsPath = Path.Combine(gameDir, "allocations.json");
        Load();
    }

    /// <summary>Load allocations from disk; create empty if not present.</summary>
    private void Load()
    {
        if (File.Exists(_allocationsPath))
        {
            try
            {
                var json = File.ReadAllText(_allocationsPath);
                var root = JsonSerializer.Deserialize<AllocationsRoot>(json)
                    ?? new AllocationsRoot();
                foreach (var (spaceName, space) in root.Allocations)
                {
                    _spaces[spaceName] = space;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[IdAllocator] Failed to load allocations.json: {ex.Message}");
                _spaces.Clear();
            }
        }
    }

    /// <summary>Save allocations to disk (call after each mod patches).</summary>
    private void Save()
    {
        try
        {
            var root = new AllocationsRoot
            {
                Allocations = new Dictionary<string, AllocationSpace>(_spaces),
            };
            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            File.WriteAllText(_allocationsPath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IdAllocator] Failed to save allocations.json: {ex.Message}");
        }
    }

    /// <summary>Set the current mod name before allocating.</summary>
    public void SetCurrentMod(string modName) => _currentMod = modName;

    /// <summary>Allocate a contiguous block of IDs for the current mod.</summary>
    public int AllocateIds(string space, int count)
    {
        if (string.IsNullOrEmpty(_currentMod))
            throw new InvalidOperationException("No current mod set before AllocateIds");
        if (count < 1)
            throw new ArgumentException("count must be >= 1", nameof(count));

        lock (_spaces)
        {
            // Get or create the space
            if (!_spaces.TryGetValue(space, out var allocationSpace))
            {
                int baseId = DefaultBases.TryGetValue(space, out var base_) ? base_ : DefaultBases["__default__"];
                allocationSpace = new AllocationSpace { Base = baseId };
                _spaces[space] = allocationSpace;
            }

            if (!allocationSpace.Claimed.ContainsKey(_currentMod))
            {
                allocationSpace.Claimed[_currentMod] = new();
            }

            var modClaims = allocationSpace.Claimed[_currentMod];

            // Find the next available slot (after all allocations in this space)
            int nextStart = allocationSpace.Base;
            foreach (var (start, end) in modClaims)
            {
                if (end >= nextStart)
                    nextStart = end + 1;
            }

            // Also check claims from other mods
            foreach (var (_, otherModClaims) in allocationSpace.Claimed)
            {
                if (otherModClaims == modClaims) continue;  // Skip self
                foreach (var (start, end) in otherModClaims)
                {
                    if (end >= nextStart)
                        nextStart = end + 1;
                }
            }

            int nextEnd = nextStart + count - 1;
            modClaims.Add((nextStart, nextEnd));
            Save();

            Console.WriteLine(
                $"[IdAllocator] Mod '{_currentMod}' allocated {space} [{nextStart}–{nextEnd}] (claim #{modClaims.Count})");
            return nextStart;
        }
    }
}
