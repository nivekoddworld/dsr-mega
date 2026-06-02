using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Graph;
using DS1MegaRando.Core.Settings;

namespace DS1MegaRando.Core.FogGate;

public class FogGateRandomizer
{
    public event EventHandler<string>? Log;

    public FogGateResult Randomize(FogGateSettings settings, AnnotationData ann, Random rng)
    {
        Emit("Building world graph...");
        var graph = new WorldGraph();
        graph.Construct(settings, ann, rng);

        Emit("Connecting fog gates...");
        var connector = new GraphConnector();
        connector.Connect(settings, graph, ann, rng);

        Emit("Verifying reachability...");
        var checker  = new GraphChecker();
        var check    = checker.Check(graph, graph.StartArea, graph.ItemAreas);

        if (check.Unreachable.Count > 0)
        {
            Emit($"WARNING: {check.Unreachable.Count} areas unreachable: {string.Join(", ", check.Unreachable.Take(5))}");
        }

        var connections = BuildConnectionList(graph);

        Emit($"Fog gate randomization complete. {connections.Count} connections made.");

        return new FogGateResult
        {
            ConnectedGraph = graph,
            AreaRatios     = graph.AreaRatios,
            Connections    = connections,
            StartArea      = graph.StartArea,
        };
    }

    private static List<FogConnection> BuildConnectionList(WorldGraph graph)
    {
        var result = new List<FogConnection>();
        var seen   = new HashSet<string?>();

        foreach (var node in graph.Nodes.Values)
        {
            foreach (var exit in node.To.Where(e => !e.IsFixed && e.Link != null))
            {
                if (seen.Contains(exit.Name)) continue;
                seen.Add(exit.Name);
                result.Add(new FogConnection(
                    exit.From ?? "?",
                    exit.To   ?? "?",
                    exit.Name ?? "unknown",
                    exit.Text));
            }
        }

        return result;
    }

    private void Emit(string message) => Log?.Invoke(this, message);
}
