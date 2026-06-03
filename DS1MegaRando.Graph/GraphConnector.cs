using DS1MegaRando.Annotations;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Graph;

/// <summary>
/// Connects fog gate edges using Fisher-Yates shuffle + BFS-based constraint solving.
/// Ported and modernised from FogMod GraphConnector.cs.
/// </summary>
public class GraphConnector
{
    private readonly GraphChecker _checker = new();

    public void Connect(
        FogGateSettings settings,
        WorldGraph graph,
        AnnotationData ann,
        Random rng)
    {
        ApplyFixedConnections(graph, ann);
        ConnectRandomizable(graph, settings, rng);

        if (settings.EnemyScalingMode != Settings.EnemyScalingMode.None)
            ComputeAreaRatios(graph, settings);
    }

    private void ApplyFixedConnections(WorldGraph graph, AnnotationData ann)
    {
        foreach (var entrance in ann.Entrances.Concat(ann.Warps))
        {
            if (!entrance.IsFixed || entrance.HasTag("unused")) continue;
            if (entrance.ASide == null || entrance.BSide == null) continue;

            var exitA = graph.Nodes[entrance.ASide.Area].To
                            .FirstOrDefault(e => e.Name == entrance.FullName && e.IsFixed);
            var entB  = graph.Nodes[entrance.BSide.Area].From
                            .FirstOrDefault(e => e.Name == entrance.FullName && e.IsFixed);
            if (exitA != null && entB != null && exitA.Link == null)
                graph.Connect(exitA, entB);
        }
    }

    private void ConnectRandomizable(WorldGraph graph, FogGateSettings settings, Random rng)
    {
        // Collect all unconnected randomizable exits and entrances
        var allExits     = graph.Nodes.Values.SelectMany(n => n.To)
                               .Where(e => !e.IsFixed && e.To == null).ToList();
        var allEntrances = graph.Nodes.Values.SelectMany(n => n.From)
                               .Where(e => !e.IsFixed && e.From == null).ToList();

        Shuffle(allExits, rng);
        Shuffle(allEntrances, rng);

        // Process two silos like FogMod: paired (bidirectional) then unpaired (one-way)
        foreach (bool paired in new[] { true, false })
        {
            var exits     = allExits    .Where(e => (e.Pair != null) == paired).ToList();
            var entrances = allEntrances.Where(e => (e.Pair != null) == paired).ToList();

            ConnectSilo(graph, exits, entrances, settings, rng, paired);
        }
    }

    private void ConnectSilo(
        WorldGraph graph,
        List<GraphEdge> exits,
        List<GraphEdge> entrances,
        FogGateSettings settings,
        Random rng,
        bool paired)
    {
        int maxSwapAttempts = exits.Count * 10 + 100;
        int swapAttempts    = 0;

        // Initial greedy shuffle-based pass (mirrors FogMod's while(true) loop)
        while (exits.Count > 0)
        {
            var exit = exits[0];
            exits.RemoveAt(0);

            // Also remove the reverse-pair from the entrances pool (FogMod does this)
            if (exit.Pair != null)
                entrances.Remove(exit.Pair);

            if (entrances.Count == 0)
            {
                // Only option is to self-link (connect gate to itself)
                if (exit.Pair != null)
                {
                    graph.Connect(exit, exit.Pair);
                }
                // else no paired partner — this shouldn't happen in a well-formed graph
                continue;
            }

            // Pick a candidate that isn't the gate's own entrance-pair
            GraphEdge? chosen = null;
            for (int i = 0; i < entrances.Count; i++)
            {
                var cand = entrances[i];
                if (exit.Pair == cand) continue;
                if (paired && (cand.Pair == null)) continue;
                if (!paired && (cand.Pair != null)) continue;

                chosen = cand;
                entrances.RemoveAt(i);
                // Also remove chosen's pair from exits (FogMod does this)
                if (chosen.Pair != null)
                    exits.Remove(chosen.Pair);
                break;
            }

            if (chosen == null) break; // ran out

            graph.Connect(exit, chosen);
        }

        // BFS-based swap loop: ensure all areas are reachable (mirrors FogMod's retry loop)
        while (swapAttempts++ < maxSwapAttempts)
        {
            var check    = _checker.Check(graph, graph.StartArea, graph.ItemAreas);
            if (check.Unreachable.Count == 0) break;

            // Find an entrance into an unreachable area
            GraphEdge? toFind = null;
            var unreachableList = check.Unreachable.ToList();
            Shuffle(unreachableList, rng);
            foreach (var area in unreachableList)
            {
                foreach (var edge in graph.Nodes[area].From)
                {
                    if (!edge.IsFixed && (edge.Pair != null) == paired)
                    {
                        toFind = edge;
                        break;
                    }
                }
                if (toFind != null) break;
            }
            if (toFind == null) break;

            // Find a redundant exit (leads somewhere already reachable, has multiple entrances)
            GraphEdge? victim = null;
            foreach (var area in check.Reachable.OrderBy(_ => rng.Next()))
            {
                foreach (var edge in graph.Nodes[area].To)
                {
                    if (edge.IsFixed || (edge.Pair != null) != paired) continue;
                    if (edge.Link == null) continue;
                    // Prefer edges whose destination has more than one incoming random edge
                    var dest = edge.To;
                    if (dest == null) continue;
                    int inCount = graph.Nodes[dest].From.Count(e => !e.IsFixed);
                    if (inCount > 1)
                    {
                        victim = edge;
                        break;
                    }
                }
                if (victim != null) break;
            }

            // Fallback: any reachable random exit
            if (victim == null)
            {
                victim = check.Reachable
                    .SelectMany(a => graph.Nodes[a].To)
                    .FirstOrDefault(e => !e.IsFixed && e.Link != null && (e.Pair != null) == paired);
            }

            if (victim == null) break;

            graph.SwapConnectedEdges(victim, toFind);
        }
    }

    private void ComputeAreaRatios(WorldGraph graph, FogGateSettings settings)
    {
        var distances = _checker.ComputeDistances(graph, graph.StartArea);
        int maxDist   = distances.Values.DefaultIfEmpty(1).Max();
        if (maxDist == 0) maxDist = 1;

        foreach (var (area, dist) in distances)
        {
            float ratio  = (float)dist / maxDist;
            float health = 1.0f + ratio * (settings.HealthScalingMax - 1.0f);
            float damage = 1.0f + ratio * (settings.DamageScalingMax - 1.0f);
            graph.AreaRatios[area] = (health, damage);
        }
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
