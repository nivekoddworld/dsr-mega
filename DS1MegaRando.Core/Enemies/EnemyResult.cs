namespace DS1MegaRando.Core.Enemies;

public class EnemyResult
{
    /// <summary>MapId → list of enemy entity modifications in that map.</summary>
    public Dictionary<string, List<EnemyPlacement>> Placements { get; set; } = new();

    /// <summary>NpcParam row ID → (hp multiplier, damage multiplier).</summary>
    public Dictionary<int, (float HP, float Damage)> StatModifications { get; set; } = new();

    public List<(string OldModel, string NewModel, string Area)> SpoilerLog { get; set; } = new();
}

public class EnemyPlacement
{
    public int    EntityId    { get; set; }
    public string OldModelId  { get; set; } = "";
    public string NewModelId  { get; set; } = "";
    public int    OldNpcParam { get; set; }
    public int    NewNpcParam { get; set; }
    public int    OldThinkParam { get; set; }
    public int    NewThinkParam { get; set; }
}
