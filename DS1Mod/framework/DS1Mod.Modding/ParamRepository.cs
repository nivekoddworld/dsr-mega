using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Edits PARAMs inside a GameParam archive. PARAMs are raw rows; to touch fields
/// you must apply the matching PARAMDEF (layout). The game doesn't ship
/// paramdefs, so load them from an embedded paramdefbnd (see
/// <see cref="LoadDefs"/> / <see cref="GamePatch.EditParams"/>).
/// </summary>
public sealed class ParamRepository
{
    private readonly IBinder _bnd;
    private readonly Dictionary<string, PARAMDEF> _defs;
    private readonly Action<string, int>? _record;

    public ParamRepository(IBinder gameParamBnd, Dictionary<string, PARAMDEF> defs,
        Action<string, int>? record = null)
    {
        _bnd = gameParamBnd;
        _defs = defs;
        _record = record;
    }

    /// <summary>Parse a paramdefbnd into a ParamType→def map. Accepts raw or
    /// DCX-wrapped bytes (SoulsFormats' Read auto-handles the DCX wrapper).</summary>
    public static Dictionary<string, PARAMDEF> LoadDefs(byte[] paramdefBnd)
    {
        var defs = new Dictionary<string, PARAMDEF>();
        try
        {
            var bnd = BND3.Read(paramdefBnd);
            foreach (BinderFile f in bnd.Files)
            {
                try
                {
                    PARAMDEF d = PARAMDEF.Read(f.Bytes);
                    defs[d.ParamType] = d;
                }
                catch { /* Skip manifest/non-def files */ }
            }
            Console.WriteLine($"[DEBUG] Loaded {defs.Count} ParamDefs from provided BND.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to load ParamDefs: {ex.Message}");
        }
        return defs;
    }

    /// <summary>
    /// Open the named param (e.g. "EquipParamGoods"), apply its def, run
    /// <paramref name="edit"/>, and write it back. No-op if the param or its def
    /// isn't present. Returns true if edited.
    /// </summary>
    public bool Edit(string paramName, Action<PARAM> edit)
    {
        bool foundFile = false;
        foreach (BinderFile f in _bnd.Files)
        {
            // DS1 filenames are often "SpEffectParam.param" or long paths
            string internalName = Path.GetFileNameWithoutExtension(f.Name);

            if (!internalName.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                continue;

            foundFile = true;
            Console.WriteLine($"[DEBUG] Located Param file: {f.Name}");

            try
            {
                PARAM p = PARAM.Read(f.Bytes);
                Console.WriteLine($"[DEBUG] Param Type: {p.ParamType} | Row Count: {p.Rows.Count}");

                if (!_defs.TryGetValue(p.ParamType, out PARAMDEF? def))
                {
                    Console.WriteLine($"[ERROR] No ParamDef found for Type: '{p.ParamType}'. Check your paramdefBnd!");
                    return false;
                }

                p.ApplyParamdef(def);
                edit(p);
                f.Bytes = p.Write();
                Console.WriteLine($"[DEBUG] Applied changes and wrote PARAM back to binder file.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed during PARAM processing: {ex.Message}");
                return false;
            }
        }

        if (!foundFile)
        {
            Console.WriteLine($"[ERROR] Could not find any binder file matching name: {paramName}");
        }
        return false;
    }

    /// <summary>
    /// Clone a donor row into a new one (so every field is valid), and add it
    /// idempotently (replacing any existing row with that id), keeping rows sorted.
    /// Returns the new row so you can tweak fields.
    /// </summary>
    public static PARAM.Row AddClone(PARAM p, int donorId, int newId, string name,
        Action<PARAM.Row>? configure = null)
    {
        PARAM.Row src = p[donorId] ?? throw new ArgumentException($"donor row {donorId} not found");
        var row = new PARAM.Row(newId, name, p.AppliedParamdef);
        for (int i = 0; i < src.Cells.Count; i++) row.Cells[i].Value = src.Cells[i].Value;
        configure?.Invoke(row);
        p.Rows.RemoveAll(r => r.ID == newId);
        p.Rows.Add(row);
        p.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));
        return row;
    }

    /// <summary>
    /// Instance overload — same as <see cref="AddClone(PARAM,int,int,string,Action{PARAM.Row}?)"/>
    /// but also records the edit for conflict detection.
    /// </summary>
    public PARAM.Row AddCloneTracked(PARAM p, int donorId, int newId, string name,
        Action<PARAM.Row>? configure = null)
    {
        _record?.Invoke(p.ParamType, newId);
        return AddClone(p, donorId, newId, name, configure);
    }
}

/// <summary>ItemLotParam <c>lotItemCategory</c> bitfield values.</summary>
public static class LotCategory
{
    public const int Weapon    = 0x10000000;
    public const int Protector = 0x20000000;  // armor
    public const int Accessory = 0x30000000;  // rings
    public const int Goods     = 0x40000000;  // consumables / key items
}
