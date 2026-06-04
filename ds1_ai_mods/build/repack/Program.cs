// Swaps one compiled .lua (.luac) into a DSR luabnd.dcx, preserving DCX/BND format.
// Usage: repack <in.luabnd.dcx> <entryLeafName> <new.luac> <out.luabnd.dcx>
using SoulsFormats;
using System.Text;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

byte[] decompressed = DCX.Decompress(args[0], out DCX.Type dcxType);
var bnd = BND3.Read(decompressed);
byte[] newBytes = File.ReadAllBytes(args[2]);
int hit = 0;
foreach (var f in bnd.Files)
    if (string.Equals(Path.GetFileName(f.Name.Replace('\\','/')), args[1], StringComparison.OrdinalIgnoreCase))
    { Console.WriteLine($"Replacing {f.Name}: {f.Bytes.Length} -> {newBytes.Length} bytes"); f.Bytes = newBytes; hit++; }
if (hit != 1) { Console.Error.WriteLine($"ERROR: matched {hit} entries (expected 1)"); return 1; }
File.WriteAllBytes(args[3], DCX.Compress(bnd.Write(), dcxType));
Console.WriteLine($"Wrote {args[3]} (DCX {dcxType})");
return 0;
