using Newtonsoft.Json;

namespace DS1MegaRando.Settings;

public class MegaSettings
{
    public GlobalSettings Global { get; set; } = new();
    public FogGateSettings FogGate { get; set; } = new();
    public ItemSettings Items { get; set; } = new();
    public EnemySettings Enemies { get; set; } = new();

    public uint GetFogSeed()   => Global.UseSeparateSeedsPerModule ? Global.FogSeed   : Global.Seed;
    public uint GetItemSeed()  => Global.UseSeparateSeedsPerModule ? Global.ItemSeed  : Global.Seed + 1;
    public uint GetEnemySeed() => Global.UseSeparateSeedsPerModule ? Global.EnemySeed : Global.Seed + 2;

    public string ToHashString() =>
        $"Seed:{Global.Seed:X8} Fog:{FogGate.EnableFogGateRandomizer} Items:{Items.EnableItemRandomizer} Enemies:{Enemies.EnableEnemyRandomizer}";

    public static MegaSettings LoadFromJson(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<MegaSettings>(json) ?? new MegaSettings();
    }

    public void SaveToJson(string path)
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }

    public MegaSettings Clone()
    {
        string json = JsonConvert.SerializeObject(this);
        return JsonConvert.DeserializeObject<MegaSettings>(json)!;
    }
}
