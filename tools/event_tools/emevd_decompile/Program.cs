using System.Text;
using System.Text.Json;
using SoulsFormats;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

string emedfPath = args[0];
string inDir     = args[1];
string outDir    = args[2];
Directory.CreateDirectory(outDir);

// ---- Load EMEDF (names, arg types, enums) ----
using var doc = JsonDocument.Parse(File.ReadAllText(emedfPath));
var root = doc.RootElement;

var enums = new Dictionary<string, Dictionary<long, string>>();
if (root.TryGetProperty("enums", out var enumsEl))
    foreach (var e in enumsEl.EnumerateArray())
    {
        var map = new Dictionary<long, string>();
        foreach (var kv in e.GetProperty("values").EnumerateObject())
            if (long.TryParse(kv.Name, out var k)) map[k] = kv.Value.GetString();
        enums[e.GetProperty("name").GetString()] = map;
    }

var defs = new Dictionary<(int,int), (string name, List<(string n, int t, string en)> args)>();
foreach (var c in root.GetProperty("main_classes").EnumerateArray())
{
    int bank = c.GetProperty("index").GetInt32();
    foreach (var ins in c.GetProperty("instrs").EnumerateArray())
    {
        int id = ins.GetProperty("index").GetInt32();
        var alist = new List<(string,int,string)>();
        if (ins.TryGetProperty("args", out var argsEl))
            foreach (var a in argsEl.EnumerateArray())
            {
                string an = a.GetProperty("name").GetString();
                int at = a.TryGetProperty("type", out var tEl) ? tEl.GetInt32() : 5;
                string en = a.TryGetProperty("enum_name", out var eEl) && eEl.ValueKind==JsonValueKind.String ? eEl.GetString() : null;
                alist.Add((an, at, en));
            }
        defs[(bank,id)] = (ins.GetProperty("name").GetString(), alist);
    }
}

static int Size(int t) => t switch { 0 or 3 => 1, 1 or 4 => 2, _ => 4 };
string FmtVal(object v, string enumName)
{
    if (enumName != null && enums.TryGetValue(enumName, out var m))
    {
        long key = Convert.ToInt64(v);
        if (m.TryGetValue(key, out var lbl)) return $"{lbl}({key})";
    }
    if (v is float f) return f.ToString("0.###");
    return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
}

foreach (var file in Directory.GetFiles(inDir, "*.emevd.dcx").OrderBy(x=>x))
{
    string name = Path.GetFileName(file).Replace(".dcx","");
    EMEVD evd;
    try { evd = EMEVD.Read(DCX.Decompress(file)); }
    catch (Exception ex) { Console.WriteLine($"{name}: ERROR {ex.Message}"); continue; }

    var sb = new StringBuilder();
    sb.AppendLine($"=== {name} | {evd.Events.Count} events ===\n");
    foreach (var ev in evd.Events)
    {
        // map: instruction index -> list of (argOffset) that are parameterized
        var paramAt = new Dictionary<long, List<EMEVD.Parameter>>();
        foreach (var p in ev.Parameters)
        {
            if (!paramAt.TryGetValue(p.InstructionIndex, out var l)) paramAt[p.InstructionIndex] = l = new();
            l.Add(p);
        }
        sb.AppendLine($"Event {ev.ID}  (rest={ev.RestBehavior}) {{");
        for (int ii = 0; ii < ev.Instructions.Count; ii++)
        {
            var ins = ev.Instructions[ii];
            if (!defs.TryGetValue((ins.Bank, ins.ID), out var def))
            {
                sb.AppendLine($"    UNKNOWN_{ins.Bank}[{ins.ID}]( {BitConverter.ToString(ins.ArgData ?? Array.Empty<byte>())} )");
                continue;
            }
            var types = def.args.Select(a => (EMEVD.Instruction.ArgType)a.t).ToList();
            List<object> vals;
            try { vals = types.Count>0 ? ins.UnpackArgs(types) : new(); }
            catch { sb.AppendLine($"    {def.name}( <unpack failed: {BitConverter.ToString(ins.ArgData ?? Array.Empty<byte>())}> )"); continue; }

            // compute arg offsets for parameter substitution
            var parts = new List<string>();
            int off = 0;
            for (int ai = 0; ai < def.args.Count; ai++)
            {
                int sz = Size(def.args[ai].t);
                if (off % sz != 0) off += sz - (off % sz);
                string rendered = FmtVal(vals[ai], def.args[ai].en);
                if (paramAt.TryGetValue(ii, out var ps))
                    foreach (var p in ps)
                        if (p.TargetStartByte == off) rendered = $"X{p.SourceStartByte}_{p.ByteCount}";
                parts.Add($"{def.args[ai].n}={rendered}");
                off += sz;
            }
            string layer = ins.Layer.HasValue ? $" [layer 0x{ins.Layer:X}]" : "";
            sb.AppendLine($"    {def.name}({string.Join(", ", parts)}){layer}");
        }
        sb.AppendLine("}\n");
    }
    File.WriteAllText(Path.Combine(outDir, name + ".evd.txt"), sb.ToString());
    Console.WriteLine($"{name}: {evd.Events.Count} events");
}
