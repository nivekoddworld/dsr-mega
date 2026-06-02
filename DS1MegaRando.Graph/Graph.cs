using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Settings;

namespace DS1MegaRando.Core.Graph;

/// <summary>
/// The game world as a directed graph of areas connected by fog gate edges.
/// Ported and modernised from FogMod Graph.cs.
/// </summary>
public class WorldGraph
{
    public Dictionary<string, AnnotationArea>     Areas       { get; private set; } = new();
    public Dictionary<string, GraphNode>          Nodes       { get; private set; } = new();
    public Dictionary<string, AnnotationEntrance> EntranceIds { get; private set; } = new();
    public Dictionary<string, List<string>>   ItemAreas   { get; private set; } = new();

    /// <summary>Area depth ratios computed after connecting the graph. (healthRatio, damageRatio)</summary>
    public Dictionary<string, (float Health, float Damage)> AreaRatios { get; set; } = new();

    public string      StartArea   { get; private set; } = "asylum";
    /// <summary>The selected CustomStart entry (null if using the default asylum start).</summary>
    public CustomStart? CustomStart { get; private set; }

    public void Construct(FogGateSettings settings, AnnotationData ann, Random? rng = null)
    {
        Areas     = ann.Areas.ToDictionary(a => a.Name);
        // Seed with vanilla locations; item randomizer overwrites these later if active
        ItemAreas = ann.KeyItems.ToDictionary(
            item => item.Name,
            item => item.Area != null ? new List<string> { item.Area } : new List<string>());

        foreach (var node in ann.Areas)
            Nodes[node.Name] = new GraphNode(node.Name);

        // Parse and validate conditions
        foreach (var area in ann.Areas)
        {
            if (area.To == null) continue;
            foreach (var side in area.To)
            {
                if (!Areas.ContainsKey(side.Area))
                    throw new InvalidDataException($"{area.Name} connects to unknown area {side.Area}");
                side.Expr = Expr.Parse(side.Cond);
            }
        }

        // Build entrance index and mark fixed/randomized
        foreach (var entrance in ann.Entrances.Concat(ann.Warps))
        {
            string id = entrance.FullName;
            if (EntranceIds.ContainsKey(id))
                throw new InvalidDataException($"Duplicate entrance id: {id}");
            EntranceIds[id] = entrance;

            MarkEntranceFixed(entrance, settings, ann);
        }

        // Add nodes for every entrance and warp side (FogMod uses Entrances.Concat(Warps) here)
        foreach (var entrance in ann.Entrances.Concat(ann.Warps))
        {
            if (entrance.HasTag("unused")) continue;
            if (entrance.ASide != null) AddEdgePair(entrance.ASide, entrance);
            if (entrance.BSide != null) AddEdgePair(entrance.BSide, entrance);
        }

        // Add fixed in-map connections (area.To sides).
        // These are immediately connected directed edges — NOT fog gates.
        // "shortcut" tag means bidirectional (both directions created).
        foreach (var area in ann.Areas)
        {
            if (area.To == null) continue;
            foreach (var side in area.To)
            {
                if (side.HasTag("temp")) continue;
                AddConnectedEdge(area.Name, side.Area, side.Expr);
                if (side.HasTag("shortcut"))
                {
                    // Reverse direction also traversable; requires reaching area first
                    Expr? reverseExpr = side.Expr == null
                        ? Expr.Named(area.Name)
                        : new Expr(new List<Expr> { Expr.Named(area.Name), side.Expr }, every: true).Simplify();
                    AddConnectedEdge(side.Area, area.Name, reverseExpr);
                }
            }
        }

        // Determine starting area
        if (settings.RandomizeStartingArea && ann.CustomStarts?.Count > 0)
        {
            // Use the caller's RNG so start-area selection is part of the same seed chain.
            var effectiveRng = rng ?? new Random();
            int idx = effectiveRng.Next(ann.CustomStarts.Count);
            CustomStart = ann.CustomStarts[idx];
            StartArea   = CustomStart.Area;
        }
        else if (ann.CustomStarts?.Count > 0 && ann.CustomStarts[0].Area != null)
        {
            // No randomization — first entry is the default (usually Firelink or Asylum)
            CustomStart = ann.CustomStarts[0];
            StartArea   = CustomStart.Area;
        }
    }

    private void MarkEntranceFixed(AnnotationEntrance entrance, FogGateSettings settings, AnnotationData ann)
    {
        if (entrance.HasTag("unused")) return;

        bool isDLC = entrance.HasTag("dlc");
        bool isBoss = entrance.HasTag("boss");
        bool isWorld = entrance.HasTag("world");
        bool isMinor = entrance.HasTag("pvp") && !entrance.HasTag("major");
        bool isMajor = entrance.HasTag("pvp") && entrance.HasTag("major");
        bool isWarp  = ann.Warps.Contains(entrance);
        bool isLordvessel = entrance.HasTag("lordvessel");

        if (entrance.HasTag("norandom") || entrance.HasTag("door"))
        {
            entrance.IsFixed = true;
            return;
        }
        if (!settings.IncludeDLC && isDLC)            { entrance.IsFixed = true; return; }
        if (!settings.RandomizeBossEntrances && isBoss) { entrance.IsFixed = true; return; }
        if (!settings.RandomizeWorldEntrances && isWorld) { entrance.IsFixed = true; return; }
        if (!settings.RandomizeMinorEntrances && isMinor) { entrance.IsFixed = true; return; }
        if (!settings.RandomizeMajorEntrances && isMajor) { entrance.IsFixed = true; return; }
        if (!settings.RandomizeWarps && isWarp)        { entrance.IsFixed = true; return; }
        if (!settings.LordvesselDoorsRandomized && isLordvessel) { entrance.IsFixed = true; return; }
        if (settings.FixKilnEntrance && entrance.Area == "kiln") { entrance.IsFixed = true; return; }
    }

    // Creates a pre-connected directed edge for area.To (non-fog-gate) connections.
    private void AddConnectedEdge(string fromArea, string toArea, Expr? expr)
    {
        var exit = new GraphEdge
        {
            Type = EdgeType.Exit, From = fromArea, To = toArea,
            IsFixed = true, Expr = expr, Text = "in map"
        };
        var ent = new GraphEdge
        {
            Type = EdgeType.Entrance, From = fromArea, To = toArea,
            IsFixed = true, Expr = expr, Text = "in map"
        };
        exit.Link = ent;
        ent.Link  = exit;
        exit.LinkedExpr = ent.LinkedExpr = expr;
        Nodes[fromArea].To.Add(exit);
        Nodes[toArea].From.Add(ent);
    }

    private (GraphEdge exit, GraphEdge entrance) AddEdgePair(AnnotationSide side, AnnotationEntrance? e)
    {
        string? name = e?.FullName;
        string text  = e?.Text ?? side.Text ?? (side.HasTag("hard") ? "hard skip" : "in map");
        bool isFixed = e == null || e.IsFixed;

        var exit = new GraphEdge
        {
            Type = EdgeType.Exit, From = side.Area, Name = name,
            Text = text, IsFixed = isFixed, Expr = side.Expr
        };
        var ent = new GraphEdge
        {
            Type = EdgeType.Entrance, To = side.Area, Name = name,
            Text = text, IsFixed = isFixed, Expr = side.Expr
        };
        exit.Pair = ent;
        ent.Pair  = exit;

        Nodes[side.Area].To.Add(exit);
        Nodes[side.Area].From.Add(ent);

        return (exit, ent);
    }

    public void Connect(GraphEdge exit, GraphEdge entrance)
    {
        if (exit.To != null || entrance.From != null)
            throw new InvalidOperationException("Already connected");

        exit.To     = entrance.To;
        entrance.From = exit.From;
        entrance.Link = exit;
        exit.Link   = entrance;

        Expr? combined = (entrance.Expr, exit.Expr) switch
        {
            (null, var e) => e,
            (var e, null) => e,
            (var a, var b) when a == b => a,
            (var a, var b) => new Expr(new List<Expr> { a, b }, every: true).Simplify(),
        };
        entrance.LinkedExpr = exit.LinkedExpr = combined;

        if (exit == entrance.Pair) return;
        if (exit.Pair != null)   exit.Pair.From   = exit.To;
        if (entrance.Pair != null) entrance.Pair.To = entrance.From;
        if (exit.Pair != null && entrance.Pair != null)
        {
            exit.Pair.Link = entrance.Pair;
            entrance.Pair.Link = exit.Pair;
            exit.Pair.LinkedExpr = entrance.Pair.LinkedExpr = combined;
        }
    }

    public void Disconnect(GraphEdge exit, bool forPair = false)
    {
        var entrance = exit.Link ?? throw new InvalidOperationException($"Can't disconnect {exit}");
        exit.Link = null;
        exit.To   = null;
        entrance.Link = null;
        entrance.From = null;
        entrance.LinkedExpr = exit.LinkedExpr = null;

        if (!forPair && exit.Pair != null && entrance.Pair != null && exit.Pair != entrance)
            Disconnect(entrance.Pair, forPair: true);
    }

    public void SwapConnectedEdges(GraphEdge oldExit, GraphEdge newEntrance)
    {
        var newExit     = newEntrance.Link!;
        var oldEntrance = oldExit.Link!;
        Disconnect(newExit);
        Disconnect(oldExit);

        if (newEntrance == newExit.Pair && oldEntrance == oldExit.Pair)
        {
            Connect(oldExit, newEntrance);
        }
        else if (newEntrance == newExit.Pair)
        {
            if (oldEntrance.Pair != null) { Connect(oldEntrance.Pair, oldEntrance); Connect(oldExit, newEntrance); }
            else if (oldExit.Pair != null) { Connect(oldExit, oldExit.Pair); Connect(newExit, oldEntrance); }
            else throw new InvalidOperationException($"Bad seed: can't self-link to reach {newEntrance}");
        }
        else if (oldEntrance == oldExit.Pair)
        {
            if (newEntrance.Pair != null) { Connect(newEntrance.Pair, newEntrance); Connect(newExit, oldEntrance); }
            else if (newExit.Pair != null) { Connect(newExit, newExit.Pair); Connect(oldExit, newEntrance); }
            else throw new InvalidOperationException($"Bad seed: can't self-link to reach {newEntrance}");
        }
        else
        {
            Connect(oldExit, newEntrance);
            Connect(newExit, oldEntrance);
        }
    }

    public IEnumerable<GraphEdge> AllEdges() =>
        Nodes.Values.SelectMany(n => n.To.Concat(n.From));

    public IEnumerable<GraphEdge> RandomizableExits() =>
        Nodes.Values.SelectMany(n => n.To).Where(e => !e.IsFixed);
}
