using DS1MegaRando.Annotations;

namespace DS1MegaRando.Graph;

public enum EdgeType { Exit, Entrance }

public class GraphEdge
{
    public EdgeType Type { get; set; }
    public string? From { get; set; }
    public string? To   { get; set; }
    public string? Name { get; set; }
    public string? Text { get; set; }
    public bool IsFixed { get; set; }
    public Expr? Expr { get; set; }
    public Expr? LinkedExpr { get; set; }

    /// <summary>Bidirectional pair (the entrance that corresponds to this exit or vice versa).</summary>
    public GraphEdge? Pair { get; set; }
    /// <summary>The edge on the other side of the connection after randomization.</summary>
    public GraphEdge? Link { get; set; }

    public bool IsExit     => Type == EdgeType.Exit;
    public bool IsEntrance => Type == EdgeType.Entrance;

    public override string ToString() =>
        Type == EdgeType.Exit ? $"Exit({From}->{To ?? "?"} [{Name}])"
                              : $"Entrance({From ?? "?"}->{To} [{Name}])";
}
