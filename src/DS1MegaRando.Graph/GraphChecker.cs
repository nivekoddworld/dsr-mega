using DS1MegaRando.Annotations;

namespace DS1MegaRando.Graph;

/// <summary>
/// BFS-based area reachability checker.
/// Ported and modernised from FogMod GraphChecker.cs.
/// </summary>
public class GraphChecker
{
    /// <summary>
    /// Returns all areas reachable from startArea given the current items/areas available.
    /// Items listed in itemAreas become available once their hosting area is reached.
    /// </summary>
    /// <param name="preAvailable">
    /// Optional tokens to seed into the available set before BFS begins.
    /// Pass "allow_glitched" to enable hard-skip edges; "allow_instawarps" for instawarp routes.
    /// </param>
    public CheckResult Check(
        WorldGraph graph,
        string startArea,
        Dictionary<string, List<string>> itemAreas,
        IEnumerable<string>? preAvailable = null)
    {
        var reachable = new HashSet<string>();
        var available = new HashSet<string>();  // areas + items currently known
        if (preAvailable != null)
            foreach (var tok in preAvailable) available.Add(tok);
        var queue = new Queue<string>();

        void Reach(string area)
        {
            if (reachable.Add(area))
            {
                available.Add(area);
                queue.Enqueue(area);

                // Unlock items placed in this area
                foreach (var (item, locations) in itemAreas)
                {
                    if (locations.Contains(area) && available.Add(item))
                    {
                        // Item unlocked — re-scan edges from all reachable nodes
                        foreach (var reached in reachable.ToList())
                            queue.Enqueue(reached);
                    }
                }
            }
        }

        Reach(startArea);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (!graph.Nodes.TryGetValue(current, out var node)) continue;

            foreach (var exit in node.To)
            {
                if (exit.To == null) continue;
                if (exit.LinkedExpr == null || exit.LinkedExpr.Evaluate(available))
                    Reach(exit.To);
            }
        }

        return new CheckResult(reachable, available, graph.Nodes.Keys.Except(reachable).ToList());
    }

    /// <summary>
    /// Computes the shortest-path distance (in edges) from startArea to every other reachable area.
    /// Used for enemy scaling.
    /// </summary>
    public Dictionary<string, int> ComputeDistances(WorldGraph graph, string startArea)
    {
        var dist  = new Dictionary<string, int> { [startArea] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(startArea);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            int d = dist[current];
            if (!graph.Nodes.TryGetValue(current, out var node)) continue;

            foreach (var exit in node.To)
            {
                if (exit.To == null) continue;
                if (!dist.ContainsKey(exit.To))
                {
                    dist[exit.To] = d + 1;
                    queue.Enqueue(exit.To);
                }
            }
        }

        return dist;
    }

    public record CheckResult(
        IReadOnlySet<string> Reachable,
        IReadOnlySet<string> Available,
        IReadOnlyList<string> Unreachable);
}
