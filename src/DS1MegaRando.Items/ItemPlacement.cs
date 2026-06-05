using DS1MegaRando.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Items;

/// <summary>
/// Priority-matrix item placement algorithm.
/// Ported from randomize_item_table.py:
/// - Easy items → easy locations, hard items → hard locations (with configurable bias)
/// </summary>
public class ItemPlacement
{
    private static readonly Dictionary<ItemDifficultyMode, float[,]> PlacementMatrix = new()
    {
        // Rows = location difficulty (Easy, Medium, Hard, VeryHard)
        // Cols = item difficulty    (Easy, Medium, Hard, VeryHard)
        // Values = probability weight for placing item of col-difficulty at row-location
        [ItemDifficultyMode.Easy] = new float[4, 4]
        {
            { 0.60f, 0.30f, 0.09f, 0.01f },
            { 0.30f, 0.40f, 0.20f, 0.10f },
            { 0.10f, 0.30f, 0.40f, 0.20f },
            { 0.01f, 0.09f, 0.30f, 0.60f },
        },
        [ItemDifficultyMode.Medium] = new float[4, 4]
        {
            { 0.40f, 0.35f, 0.20f, 0.05f },
            { 0.25f, 0.35f, 0.30f, 0.10f },
            { 0.10f, 0.25f, 0.40f, 0.25f },
            { 0.05f, 0.15f, 0.30f, 0.50f },
        },
        [ItemDifficultyMode.Hard] = new float[4, 4]
        {
            { 0.20f, 0.30f, 0.30f, 0.20f },
            { 0.15f, 0.25f, 0.35f, 0.25f },
            { 0.10f, 0.20f, 0.35f, 0.35f },
            { 0.05f, 0.10f, 0.30f, 0.55f },
        },
    };

    public Dictionary<int, (int ItemId, int Category)> PlaceAll(
        ItemSettings settings,
        List<ItemLocation> locations,
        List<PoolItem> items,
        Dictionary<int, (int ItemId, int Category)> keyAssignments,
        Random rng)
    {
        var assignments = new Dictionary<int, (int, int)>(keyAssignments);

        // Remaining items and locations after key items are placed
        var remainingItems = items
            .Where(i => !i.IsKey)
            .ToList();

        var remainingLocs = locations
            .Where(l => !assignments.ContainsKey(l.LotId))
            .ToList();

        // Group items by difficulty
        var itemsByDiff = new Dictionary<ItemDifficulty, List<PoolItem>>
        {
            [ItemDifficulty.Easy]     = new(),
            [ItemDifficulty.Medium]   = new(),
            [ItemDifficulty.Hard]     = new(),
            [ItemDifficulty.VeryHard] = new(),
        };
        foreach (var item in remainingItems)
        {
            var diff = GetItemDifficulty(item, settings);
            if (itemsByDiff.TryGetValue(diff, out var bucket))
                bucket.Add(item);
        }

        var matrix = PlacementMatrix[settings.Difficulty];

        // For each location, pick an item using the weighted probability matrix
        foreach (var loc in remainingLocs)
        {
            int locDiffIndex = DifficultyToIndex(loc.Difficulty);

            // Build weighted pool from remaining items
            var pool = PickWeightedItem(itemsByDiff, locDiffIndex, matrix, rng);
            if (pool == null) continue;

            assignments[loc.LotId] = (pool.ItemId, pool.LotCategory);
            itemsByDiff[GetItemDifficulty(pool, settings)].Remove(pool);
        }

        return assignments;
    }

    private PoolItem? PickWeightedItem(
        Dictionary<ItemDifficulty, List<PoolItem>> byDiff,
        int locDiffIndex,
        float[,] matrix,
        Random rng)
    {
        // Build a combined weighted list
        var candidates = new List<(PoolItem Item, float Weight)>();
        var diffs = new[] { ItemDifficulty.Easy, ItemDifficulty.Medium, ItemDifficulty.Hard, ItemDifficulty.VeryHard };

        for (int col = 0; col < 4; col++)
        {
            float w = matrix[locDiffIndex, col];
            if (w <= 0) continue;
            foreach (var item in byDiff[diffs[col]])
                candidates.Add((item, w));
        }

        if (candidates.Count == 0) return null;

        float total  = candidates.Sum(c => c.Weight);
        float target = (float)rng.NextDouble() * total;
        float acc    = 0;

        foreach (var (item, weight) in candidates)
        {
            acc += weight;
            if (acc >= target) return item;
        }

        return candidates[^1].Item;
    }

    private static int DifficultyToIndex(ItemDifficulty diff) => diff switch
    {
        ItemDifficulty.Easy     => 0,
        ItemDifficulty.Medium   => 1,
        ItemDifficulty.Hard     => 2,
        ItemDifficulty.VeryHard => 3,
        _                       => 1,
    };

    private static ItemDifficulty GetItemDifficulty(PoolItem item, ItemSettings settings)
    {
        if (item.IsKey)  return ItemDifficulty.Key;
        if (item.IsBoss) return ItemDifficulty.BossSoul;
        // Default classification — can be extended with full item difficulty table
        return ItemDifficulty.Medium;
    }
}
