using DS1MegaRando.Graph;

namespace DS1MegaRando.FogGate;

public class FogGateResult
{
    public WorldGraph ConnectedGraph { get; set; } = new();
    public Dictionary<string, (float Health, float Damage)> AreaRatios { get; set; } = new();
    public List<FogConnection> Connections { get; set; } = new();
    public string StartArea { get; set; } = "asylum";
}

public record FogConnection(string FromArea, string ToArea, string EntranceName, string? Text);
