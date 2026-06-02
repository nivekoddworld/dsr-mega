using SoulsFormats;

// Check live game output files for:
// 1. Is event 8950 in the common.emevd?
// 2. What destination map/entity does it encode?
// 3. Are any new player spawns in the relevant MSBs?

string gameEventDir = @"D:\SteamLibrary\steamapps\common\DARK SOULS REMASTERED\event";
string gameMsbDir   = @"D:\SteamLibrary\steamapps\common\DARK SOULS REMASTERED\map\MapStudio";

// Check common.emevd for event 8950 injection
var common = EMEVD.Read(Path.Combine(gameEventDir, "common.emevd.dcx"));
var evt0 = common.Events.First(e => e.ID == 0);

Console.WriteLine("=== common.emevd event 0 — 8950 calls ===");
bool found8950 = false;
foreach (var instr in evt0.Instructions.Where(i => i.Bank == 2000 && i.ID == 0 && i.ArgData.Length >= 8))
{
    int evtId = BitConverter.ToInt32(instr.ArgData, 4);
    if (evtId != 8950) continue;
    found8950 = true;
    // Args: slot(4), eventId(4), destArea(1), destBlock(1), pad(2), softWarpId(4), destSpawnId(4)
    byte destArea  = instr.ArgData[8];
    byte destBlock = instr.ArgData[9];
    int  softWarp  = instr.ArgData.Length >= 16 ? BitConverter.ToInt32(instr.ArgData, 12) : -1;
    int  destSpawn = instr.ArgData.Length >= 20 ? BitConverter.ToInt32(instr.ArgData, 16) : -1;
    Console.WriteLine($"  Found 8950: destArea={destArea} destBlock={destBlock} → m{destArea:D2}_{destBlock:D2}_00_00  softWarp={softWarp}  destSpawn={destSpawn}");
}
if (!found8950) Console.WriteLine("  NOT FOUND — event 8950 was never injected");

// Check all relevant MSBs for new player spawns (entity IDs >= 5900100)
Console.WriteLine("\n=== Player spawns with high entity IDs (added by randomizer) ===");
foreach (var path in Directory.GetFiles(gameMsbDir, "*.msb").OrderBy(x => x))
{
    var msb = MSB1.Read(path);
    var added = msb.Parts.Players.Where(p => p.EntityID >= 5900100).ToList();
    if (added.Count == 0) continue;
    Console.WriteLine($"  {Path.GetFileName(path)}:");
    foreach (var p in added)
        Console.WriteLine($"    {p.Name} entity={p.EntityID} pos=({p.Position.X:F3}, {p.Position.Y:F3}, {p.Position.Z:F3})");
}

// Also show all player spawns in each of the 6 target maps
Console.WriteLine("\n=== All player spawns in target maps ===");
string[] targetMaps = { "m15_00_00_00", "m12_00_00_01", "m15_01_00_00", "m12_01_00_00", "m11_00_00_00" };
foreach (var mapId in targetMaps)
{
    string path = Path.Combine(gameMsbDir, mapId + ".msb");
    if (!File.Exists(path)) { Console.WriteLine($"  {mapId}: not found"); continue; }
    var msb = MSB1.Read(path);
    Console.WriteLine($"  {mapId}: {msb.Parts.Players.Count} spawns");
    foreach (var p in msb.Parts.Players)
        Console.WriteLine($"    {p.Name} entity={p.EntityID} pos=({p.Position.X:F3}, {p.Position.Y:F3}, {p.Position.Z:F3})");
}
