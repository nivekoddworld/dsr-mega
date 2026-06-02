namespace DS1MegaRando.Enemies;

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
    public int    EntityId      { get; set; }

    /// <summary>
    /// Zero-based index of this enemy in its map's Parts.Enemies list.
    /// Used for matching in the writer — works for all enemies including EntityID=0.
    /// </summary>
    public int    PartIndex     { get; set; } = -1;

    /// <summary>Map this placement belongs to (e.g. "m10_02_00_00").</summary>
    public string MapId         { get; set; } = "";

    public string OldModelId    { get; set; } = "";
    public string NewModelId    { get; set; } = "";
    public int    OldNpcParam   { get; set; }
    public int    NewNpcParam   { get; set; }
    public int    OldThinkParam { get; set; }
    public int    NewThinkParam { get; set; }

    /// <summary>
    /// InitAnimID to write into the MSB part for the new model.
    /// -1 means let the game use the model/AI default (safest for most enemies).
    /// Set to a specific anim ID when the reference data provides a validated
    /// idle animation for the model.
    /// </summary>
    public int NewInitAnimId { get; set; } = -1;
}
