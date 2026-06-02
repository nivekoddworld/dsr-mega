using System.Xml;
using SoulsFormats;

// Inspect the real DSR ItemLotParam for the lots that ground-pickup Treasure
// events actually reference. Question: do these lots carry a deliverable item +
// non-zero weight/count, and what is their getItemFlagId?

string bndPath = @"Z:\Bonejam\VideoGameShop\dsr-mega\FogMod-master\dist\DS1R\param\GameParam\GameParam.parambnd.dcx";
string layoutXml = @"Z:\Bonejam\VideoGameShop\dsr-mega\DS1MegaRando.Data\Params\ITEMLOT_PARAM_ST.xml";

var bnd = BND3.Read(bndPath);
PARAM? itemLot = null;
string paramType = "";
int detected = 0;
foreach (var f in bnd.Files)
{
    if (Path.GetFileNameWithoutExtension(f.Name) != "ItemLotParam") continue;
    itemLot = PARAM.Read(f.Bytes);
    paramType = itemLot.ParamType;
    detected = (int)itemLot.DetectedSize;
    break;
}
if (itemLot == null) { Console.WriteLine("ItemLotParam not found"); return; }
Console.WriteLine($"ItemLotParam: ParamType='{paramType}', DetectedSize={detected}, rows={itemLot.Rows.Count}");

// Build a PARAM.Layout from the embedded XML the same way GameFileReader does.
var layout = PARAM.Layout.ReadXMLFile(layoutXml);
Console.WriteLine($"Embedded ITEMLOT layout: Size={layout.Size}  (match={(layout.Size == detected)})");
if (layout.Size != detected) { Console.WriteLine("*** SIZE MISMATCH -> paramdef would NOT apply -> ItemPool returns empty!"); }

var paramdef = layout.ToParamdef(paramType, out _);
itemLot.ApplyParamdef(paramdef);

int[] probe = { 1010000, 1010010, 1010020, 1300000, 1000000, 1200000, 1300010 };
Console.WriteLine();
foreach (var id in probe)
{
    var row = itemLot.Rows.FirstOrDefault(r => r.ID == id);
    if (row == null) { Console.WriteLine($"lot {id}: (no such row)"); continue; }
    Console.WriteLine(
        $"lot {id}: id01={Cell(row,"lotItemId01")} cat01={Cell(row,"lotItemCategory01")} " +
        $"base01={Cell(row,"lotItemBasePoint01")} num01={Cell(row,"lotItemNum01")} " +
        $"flag01={Cell(row,"getItemFlagId01")}");
}

// How many lots would ItemPool include (id01 != 0)?
int withItem = itemLot.Rows.Count(r => Conv(r["lotItemId01"]?.Value) != 0);
Console.WriteLine($"\nLots with lotItemId01 != 0 (would enter pool): {withItem} / {itemLot.Rows.Count}");

static string Cell(PARAM.Row r, string n) => r[n]?.Value?.ToString() ?? "<null>";
static long Conv(object? v) { try { return v == null ? 0 : System.Convert.ToInt64(v); } catch { return 0; } }
