namespace DS1MegaRando.Core.Settings;

public class GlobalSettings
{
    public string GameDirectory { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string Language { get; set; } = "ENGLISH";
    public uint Seed { get; set; } = GenerateRandomSeed();
    public bool UseSeparateSeedsPerModule { get; set; } = false;
    public uint FogSeed { get; set; } = GenerateRandomSeed();
    public uint ItemSeed { get; set; } = GenerateRandomSeed();
    public uint EnemySeed { get; set; } = GenerateRandomSeed();
    public bool CreateSpoilerLog { get; set; } = true;
    public string SpoilerLogPath { get; set; } = "";
    public bool DryRun { get; set; } = false;

    public static uint GenerateRandomSeed() =>
        (uint)Random.Shared.Next(int.MinValue, int.MaxValue);
}
