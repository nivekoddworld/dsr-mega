namespace DS1MegaRando.Core.Annotations;

/// <summary>
/// Logic expression tree for area/item access conditions.
/// Ported from FogMod AnnotationData.Expr.
/// </summary>
public class Expr
{
    public static readonly Expr TRUE  = new(new List<Expr>(), every: true,  name: null);
    public static readonly Expr FALSE = new(new List<Expr>(), every: false, name: null);

    private readonly List<Expr> _exprs;
    private readonly bool _every;
    private readonly string? _name;

    public Expr(List<Expr> exprs, bool every = true, string? name = null)
    {
        if (exprs.Count > 0 && name != null) throw new ArgumentException("Incorrect construction: cannot have both exprs and name");
        _exprs = exprs;
        _every = every;
        _name  = name;
    }

    public static Expr Named(string name) => new(new List<Expr>(), every: true, name: name);

    public bool IsTrue()  => _name == null && _exprs.Count == 0 && _every;
    public bool IsFalse() => _name == null && _exprs.Count == 0 && !_every;

    public SortedSet<string> FreeVars()
    {
        if (_name != null) return new SortedSet<string> { _name };
        return new SortedSet<string>(_exprs.SelectMany(e => e.FreeVars()));
    }

    public bool Needs(string check)
    {
        if (check == _name) return true;
        if (_every) return _exprs.Any(e => e.Needs(check));
        else        return _exprs.All(e => e.Needs(check));
    }

    public Expr Substitute(Dictionary<string, Expr> config)
    {
        if (_name != null)
        {
            return config.TryGetValue(_name, out var replacement) ? replacement : this;
        }
        return new Expr(_exprs.Select(e => e.Substitute(config)).ToList(), _every);
    }

    public (List<string> Vars, float Cost) Cost(Func<string, float> costFn)
    {
        if (_name != null) return (new List<string> { _name }, costFn(_name));
        if (_exprs.Count == 0) return (new List<string>(), 0);
        var subcosts = _exprs.Select(e => e.Cost(costFn));
        return _every
            ? subcosts.Aggregate((a, b) => (a.Vars.Concat(b.Vars).ToList(), a.Cost + b.Cost))
            : subcosts.OrderBy(c => c.Cost).First();
    }

    public Expr Simplify()
    {
        if (_name != null) return this;
        if (_exprs.Count == 0) return this;
        var simplified = _exprs.Select(e => e.Simplify()).ToList();
        // Flatten nested same-type expressions
        var flat = new List<Expr>();
        foreach (var e in simplified)
        {
            if (e._name == null && e._exprs.Count > 0 && e._every == _every)
                flat.AddRange(e._exprs);
            else
                flat.Add(e);
        }
        if (flat.Count == 1) return flat[0];
        return new Expr(flat, _every);
    }

    public bool Evaluate(ISet<string> available)
    {
        if (_name != null) return available.Contains(_name);
        if (_exprs.Count == 0) return _every;
        return _every
            ? _exprs.All(e => e.Evaluate(available))
            : _exprs.Any(e => e.Evaluate(available));
    }

    public static Expr Parse(string? s)
    {
        if (s == null) return TRUE;
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1) return Named(words[0]);
        if (words.Length > 1 && words[0] == "AND")
            return new Expr(words.Skip(1).Select(Named).ToList(), every: true);
        if (words.Length > 1 && words[0] == "OR")
            return new Expr(words.Skip(1).Select(Named).ToList(), every: false);
        throw new ArgumentException($"Unknown condition format: {s}");
    }

    public override string ToString() =>
        _name ?? (_exprs.Count == 0
            ? (_every ? "TRUE" : "FALSE")
            : $"({(_every ? "AND" : "OR")} {string.Join(" ", _exprs)})");
}
