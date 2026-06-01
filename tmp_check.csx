using System;
using System.IO;
using SoulsFormats;

var msbPath = @"D:\SteamLibrary\steamapps\common\DARK SOULS REMASTERED\map\MapStudio\m10_02_00_00.msb";
var msb = MSB1.Read(msbPath);

Console.WriteLine("=== Players in Firelink MSB ===");
foreach (var p in msb.Parts.Players)
    Console.WriteLine($"  {p.Name}  EntityID={p.EntityID}  Model={p.ModelName}  Pos=({p.Position.X:F1},{p.Position.Y:F1},{p.Position.Z:F1})");

Console.WriteLine($"\n=== Regions starting with FR:/BR: ===");
foreach (var r in msb.Regions.Regions.Where(r => r.Name.StartsWith("FR: ") || r.Name.StartsWith("BR: ")))
    Console.WriteLine($"  [{r.EntityID}] {r.Name}");
