using DS1MegaRando.Core.Graph;
using YamlDotNet.Serialization;

namespace DS1MegaRando.Core.Annotations;

// ── Top-level model ────────────────────────────────────────────────────────

public class AnnotationData
{
    public List<AnnotationArea>     Areas       { get; set; } = new();
    public List<AnnotationKeyItem>  KeyItems    { get; set; } = new();
    public List<AnnotationEntrance> Entrances   { get; set; } = new();
    public List<AnnotationEntrance> Warps       { get; set; } = new();
    public List<AnnotationEnemyCol> Enemies     { get; set; } = new();
    public List<CustomStart>        CustomStarts{ get; set; } = new();
    public Dictionary<int, string>  LotLocations{ get; set; } = new();
}

// ── Taggable base ─────────────────────────────────────────────────────────

public abstract class Taggable
{
    private string? _tags;

    public string? Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            TagList = value == null
                ? new List<string>()
                : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

    [YamlIgnore]
    public List<string> TagList { get; private set; } = new();

    public bool HasTag(string tag) => TagList.Contains(tag);
}

// ── Area ──────────────────────────────────────────────────────────────────

public class AnnotationArea : Taggable
{
    public string       Name         { get; set; } = "";
    public string?      Text         { get; set; }
    public string?      ScalingBase  { get; set; }
    public int          BossTrigger  { get; set; }
    public int          DefeatFlag   { get; set; }
    public int          TrapFlag     { get; set; }
    public List<AnnotationSide>? To  { get; set; }
}

// ── Side (area connection) ────────────────────────────────────────────────

public class AnnotationSide : Taggable
{
    public string  Area     { get; set; } = "";
    public string? Text     { get; set; }
    public string? Cond     { get; set; }
    public int     Flag     { get; set; }
    public int     TrapFlag { get; set; }
    public int     EntryFlag{ get; set; }
    public int     WarpFlag { get; set; }
    public int     BossTrigger { get; set; }
    public string? BossTriggerArea { get; set; }
    public int     BeforeWarpFlag  { get; set; }
    public int     ActionRegion    { get; set; }
    public float   AdjustHeight    { get; set; }
    public string? CustomWarp      { get; set; }
    public string? DestinationMap  { get; set; }
    public int     CustomActionWidth { get; set; }
    public string? Col             { get; set; }
    public string? ExcludeIfRandomized { get; set; }

    [YamlIgnore] public Expr? Expr { get; set; }
}

// ── Entrance / Warp ───────────────────────────────────────────────────────

public class AnnotationEntrance : Taggable
{
    public string?          Name    { get; set; }
    public int              ID      { get; set; }
    public string           Area    { get; set; } = "";
    public string?          Text    { get; set; }
    public string?          Comment { get; set; }
    public string?          DoorCond{ get; set; }
    public float            AdjustHeight { get; set; }
    public AnnotationSide?  ASide   { get; set; }
    public AnnotationSide?  BSide   { get; set; }

    [YamlIgnore] public bool IsFixed { get; set; }
    [YamlIgnore] public string FullName => Area + "_" + ID;

    public List<AnnotationSide> Sides()
    {
        var sides = new List<AnnotationSide>();
        if (ASide != null) sides.Add(ASide);
        if (BSide != null) sides.Add(BSide);
        return sides;
    }
}

// ── Enemy collision col ───────────────────────────────────────────────────

public class AnnotationEnemyCol
{
    public string       Col      { get; set; } = "";
    public string       Area     { get; set; } = "";
    public List<string> Includes { get; set; } = new();
}

// ── Key item ──────────────────────────────────────────────────────────────

public class AnnotationKeyItem : Taggable
{
    public string  Name { get; set; } = "";
    public string? ID   { get; set; }
    public string? Area { get; set; }
}

// ── Custom start ──────────────────────────────────────────────────────────

public class CustomStart
{
    public string  Name    { get; set; } = "";
    public string  Area    { get; set; } = "";
    public string? Respawn { get; set; }
}
