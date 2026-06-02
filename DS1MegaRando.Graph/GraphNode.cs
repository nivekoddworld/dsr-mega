namespace DS1MegaRando.Graph;

public class GraphNode
{
    public string Name { get; set; }
    public List<GraphEdge> From { get; } = new(); // entrance edges leading into this area
    public List<GraphEdge> To   { get; } = new(); // exit edges leading out of this area

    public GraphNode(string name) => Name = name;

    public override string ToString() => Name;
}
