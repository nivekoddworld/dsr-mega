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
    private sealed record AllocationClaim
    {
        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }
    }

    private sealed record AllocationSpace
    {
        [JsonPropertyName("base")]
        public int Base { get; set; }

        [JsonPropertyName("claimed")]
        public Dictionary<string, List<AllocationClaim>> Claimed { get; set; } = new();
    }

    private sealed record AllocationsRoot
    {
        [JsonPropertyName("allocations")]
        public Dictionary<string, AllocationSpace> Allocations { get; set; } = new();
    }

    private readonly string _allocationsPath;
    private readonly Dictionary<string, AllocationSpace> _spaces = new();
    private string? _currentMod;

    // Tracks how many times AllocateIds has been called for each (mod, space) pair
    // this session, so we can replay existing claims in order instead of appending.
    private readonly Dictionary<(string Mod, string Space), int> _callIndex = new();

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

        // Event flags (per-map) — full map ID format (e.g. "m18_01_00_00")
        // Each map has a reserved flag range; event IDs use a SEPARATE offset (+500)
        // so that the "event N is registered → flag N" DS1R mirroring never causes
        // an allocated event flag to collide with an allocated event ID.
        { "EventFlags_m18_00_00_00", 11809000 },
        { "EventFlags_m18_01_00_00", 11819000 },
        { "EventFlags_m10_00_00_00", 11009000 },
        { "EventFlags_m10_01_00_00", 11010900 },
        { "EventFlags_m14_01_00_00", 11415000 },
        { "EventFlags_m12_01_00_00", 11215000 },
        { "EventFlags_m12_01_00_01", 11215500 },
        { "EventFlags_m15_01_00_00", 11515000 },
        { "EventFlags_m11_00_00_00", 11110000 },

        // EMEVD events (per-map) — +500 offset from flags to avoid flag-mirror collision
        { "EmevdEvents_m18_00_00_00", 11809500 },
        { "EmevdEvents_m18_01_00_00", 11819500 },
        { "EmevdEvents_m10_00_00_00", 11009500 },
        { "EmevdEvents_m10_01_00_00", 11011400 },
        { "EmevdEvents_m14_01_00_00", 11415500 },
        { "EmevdEvents_m12_01_00_00", 11215500 },
        { "EmevdEvents_m12_01_00_01", 11216000 },
        { "EmevdEvents_m15_01_00_00", 11515500 },
        { "EmevdEvents_m11_00_00_00", 11110500 },

        // MSB entity IDs (per-map) — use high unused ranges
        { "MsbEntities_m18_00_00_00", 1809000 },
        { "MsbEntities_m18_01_00_00", 1811000 },
        { "MsbEntities_m10_00_00_00", 1009000 },
        { "MsbEntities_m10_01_00_00", 1010000 },
        { "MsbEntities_m14_01_00_00", 1410000 },
        { "MsbEntities_m12_01_00_00", 1210000 },
        { "MsbEntities_m15_01_00_00", 1510000 },

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

            // Replay existing claim if this is a repeat run (same call order = same IDs).
            var key = (_currentMod, space);
            _callIndex.TryGetValue(key, out int idx);
            _callIndex[key] = idx + 1;

            if (idx < modClaims.Count)
            {
                var existing = modClaims[idx];
                int existingSize = existing.End - existing.Start + 1;
                if (existingSize != count)
                    Console.Error.WriteLine(
                        $"[IdAllocator] WARNING: mod '{_currentMod}' re-requested {space} claim #{idx + 1} " +
                        $"with count={count} but original was count={existingSize}. Using original.");
                Console.WriteLine(
                    $"[IdAllocator] Mod '{_currentMod}' reusing {space} [{existing.Start}–{existing.End}] (claim #{idx + 1})");
                return existing.Start;
            }

            // New claim — find the next available slot after all existing claims.
            int nextStart = allocationSpace.Base;
            foreach (var (_, otherModClaims) in allocationSpace.Claimed)
            {
                foreach (var claim in otherModClaims)
                {
                    if (claim.End >= nextStart)
                        nextStart = claim.End + 1;
                }
            }

            int nextEnd = nextStart + count - 1;
            modClaims.Add(new AllocationClaim { Start = nextStart, End = nextEnd });
            Save();

            Console.WriteLine(
                $"[IdAllocator] Mod '{_currentMod}' allocated {space} [{nextStart}–{nextEnd}] (claim #{modClaims.Count})");
            return nextStart;
        }
    }
}
