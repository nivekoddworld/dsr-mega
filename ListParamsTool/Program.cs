using SoulsFormats; using System; using System.IO; using System.Linq;

var bndPath = @"D:\SteamLibrary\steamapps\common\DARK SOULS REMASTERED\param\GameParam\GameParam.parambnd.dcx";
var bnd = BND3.Read(bndPath);
var asm = System.Reflection.Assembly.LoadFrom(@"Z:\Bonejam\VideoGameShop\dsr-mega\DS1MegaRando.Data\bin\Debug\net8.0-windows\DS1MegaRando.Data.dll");
var layouts = new System.Collections.Generic.Dictionary<string,PARAM.Layout>();
foreach (var n in asm.GetManifestResourceNames().Where(n=>n.EndsWith(".xml"))){
    using var s=asm.GetManifestResourceStream(n)!; var d=new System.Xml.XmlDocument(); d.Load(s);
    var p=n.Split('.'); try{layouts[p[^2]]=PARAM.Layout.ReadXMLDoc(d);}catch{}
}
var lotFile = bnd.Files.First(f=>Path.GetFileNameWithoutExtension(f.Name)=="ItemLotParam");
var lot = PARAM.Read(lotFile.Bytes);
if(layouts.TryGetValue(lot.ParamType,out var lay)&&lay.Size==lot.DetectedSize)
    lot.ApplyParamdef(lay.ToParamdef(lot.ParamType,out _));
static int G(PARAM.Row r,string f){try{return Convert.ToInt32(r[f]?.Value);}catch{return 0;}}

// Known DSR starting gift item IDs from DSR-Gadget:
// Goods:  371=Binoculars, 376=Pendant, 297=Black Firebomb, 500=Humanity, 501=Twin Humanities
//         2100=Master Key, 390-396=Firekeeper Souls
// Rings:  111=Tiny Being's Ring, 137=Old Witch's Ring
// Categories: Goods=1073741824(0x40000000), Ring=536870912(0x20000000)

var giftItems = new System.Collections.Generic.HashSet<(int id, int cat)>
{
    (371,  1073741824), // Binoculars
    (376,  1073741824), // Pendant
    (297,  1073741824), // Black Firebomb
    (500,  1073741824), // Humanity
    (501,  1073741824), // Twin Humanities
    (2100, 1073741824), // Master Key
    (390,  1073741824), // Fire Keeper Soul (Anastacia)
    (111,  536870912),  // Tiny Being's Ring
    (137,  536870912),  // Old Witch's Ring
};

Console.WriteLine("ItemLotParam rows containing known gift items:");
Console.WriteLine($"{"Row",8} | {"Item",7} | {"Cat",12} | {"Num",4} | {"getItemFlagId",14}");
Console.WriteLine(new string('-', 60));
foreach (var row in lot.Rows.OrderBy(r => r.ID))
{
    int item = G(row, "lotItemId01");
    int cat  = G(row, "lotItemCategory01");
    int num  = G(row, "lotItemNum01");
    int flag = G(row, "getItemFlagId");
    if (giftItems.Contains((item, cat)))
        Console.WriteLine($"{row.ID,8} | {item,7} | {cat,12} | {num,4} | {flag,14}");
}
