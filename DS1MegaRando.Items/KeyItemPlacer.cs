using DS1MegaRando.Annotations;
using DS1MegaRando.FogGate;
using DS1MegaRando.Graph;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Items;

/// <summary>
/// Places key items using BFS reachability — ensures the game is always completable.
/// Ported from key_items_setup.py logic.
/// </summary>
public class KeyItemPlacer
{
    public Dictionary<int, (int ItemId, int Category)> Place(
        ItemSettings settings,
        AnnotationData ann,
        List<ItemLocation> locations,
        List<PoolItem> items,
        Random rng,
        FogGateResult? fogResult,
        WorldGraph? graph)
    {
        // Build key item assignments: lotId → (itemId, category)
        var assignments = new Dictionary<int, (int ItemId, int Category)>();

        var keyItems = items.Where(i => i.IsKey).ToList();
        var keyLocations = GetKeyItemLocations(settings, locations);

        // In Vanilla mode, preserve original positions
        if (settings.KeyItemMode == KeyItemMode.Vanilla)
        {
            foreach (var ki in keyItems)
                assignments[ki.SourceLot] = (ki.ItemId, ki.LotCategory);
            return assignments;
        }

        // Shuffle key items into accessible locations using BFS constraint solving
        Shuffle(keyLocations, rng);
        Shuffle(keyItems, rng);

        // Iterative BFS placement:
        // 1. Start with reachable areas
        // 2. Place key items that unlock more areas
        // 3. Repeat until all key items placed
        var reachable   = new HashSet<string> { graph?.StartArea ?? "asylum" };
        var placed      = new HashSet<int>();
        int maxIter     = keyItems.Count * 3;
        int iter        = 0;

        while (placed.Count < keyItems.Count && iter++ < maxIter)
        {
            // Find locations reachable right now
            var reachableLocs = keyLocations
                .Where(l => reachable.Contains(l.Area) && !assignments.ContainsKey(l.LotId))
                .ToList();

            // Place a key item there
            var unplaced = keyItems.Where(ki => !placed.Contains(ki.ItemId)).ToList();
            if (unplaced.Count == 0 || reachableLocs.Count == 0) break;

            var loc  = reachableLocs[rng.Next(reachableLocs.Count)];
            var item = unplaced[rng.Next(unplaced.Count)];

            assignments[loc.LotId] = (item.ItemId, item.LotCategory);
            placed.Add(item.ItemId);

            // Expand reachable areas based on placed key items
            if (graph != null)
            {
                var checker = new GraphChecker();
                var itemAreas = BuildItemAreas(ann, assignments);
                var check = checker.Check(graph, graph.StartArea, itemAreas);
                reachable.UnionWith(check.Reachable);
            }
        }

        // Place any remaining key items in whatever locations are left
        var unplacedItems = keyItems.Where(ki => !placed.Contains(ki.ItemId)).ToList();
        var remainingLocs = keyLocations.Where(l => !assignments.ContainsKey(l.LotId)).ToList();
        for (int i = 0; i < Math.Min(unplacedItems.Count, remainingLocs.Count); i++)
            assignments[remainingLocs[i].LotId] = (unplacedItems[i].ItemId, unplacedItems[i].LotCategory);

        return assignments;
    }

    private List<ItemLocation> GetKeyItemLocations(ItemSettings settings, List<ItemLocation> all)
    {
        return settings.KeyItemMode switch
        {
            KeyItemMode.RaceMode    => all.Where(l => l.IsKeySlot).ToList(),
            KeyItemMode.SpeedrunMode => all.Where(l => l.IsKeySlot || l.Difficulty == Data.Items.ItemDifficulty.Easy).ToList(),
            _                       => all.ToList(),
        };
    }

    private static Dictionary<string, List<string>> BuildItemAreas(
        AnnotationData ann,
        Dictionary<int, (int ItemId, int Category)> currentAssignments)
    {
        var itemAreas = ann.KeyItems.ToDictionary(ki => ki.Name, ki => new List<string>());

        // Build reverse map: itemId → key item name so we can look up placements
        var itemIdToName = new Dictionary<int, string>();
        foreach (var ki in ann.KeyItems)
        {
            if (ki.ID == null) continue;
            int id = ParseKeyItemId(ki.ID);
            if (id != 0) itemIdToName[id] = ki.Name;
        }

        // For each placed lot, find which area it's in and credit the key item there
        foreach (var (lotId, assignment) in currentAssignments)
        {
            if (!ann.LotLocations.TryGetValue(lotId, out var area)) continue;
            if (!itemIdToName.TryGetValue(assignment.ItemId, out var keyName)) continue;
            if (!itemAreas[keyName].Contains(area))
                itemAreas[keyName].Add(area);
        }

        return itemAreas;
    }

    private static int ParseKeyItemId(string id)
    {
        // Format: "category:itemId" e.g. "3:2510" or "2:138"
        var parts = id.Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], out int v) ? v : 0;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
