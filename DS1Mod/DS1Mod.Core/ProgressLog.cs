using System.Text.Json;
using System.Text.Json.Serialization;

namespace DS1Mod.Core;

/// <summary>
/// Serializes session state to a JSON sidecar file that the randomizer UI
/// and external tools can read without attaching to the game process.
///
/// Written on every Flush() call; also written on session end.
/// Format is intentionally simple so future tools can consume it easily.
/// </summary>
public sealed class ProgressLog
{
    public string OutputPath { get; }

    private readonly object _lock = new();
    private ProgressSnapshot _snapshot = new();

    public ProgressLog(string outputPath) => OutputPath = outputPath;

    public static string DefaultPath(string seed) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DS1Mod", $"progress_{seed}.json");

    internal void RecordBossKill(BossKill kill)
    {
        lock (_lock)
            _snapshot = _snapshot with
            {
                BossKills = [.._snapshot.BossKills,
                             new BossKillEntry(kill.BossName, kill.KilledAt)]
            };
    }

    internal void RecordGateOpened(FogGate gate)
    {
        lock (_lock)
            _snapshot = _snapshot with
            {
                OpenedGates = [.._snapshot.OpenedGates,
                               new FogGateEntry(gate.Name, gate.MapId)]
            };
    }

    internal void UpdatePlayer(PlayerState? state)
    {
        if (state is null) return;
        lock (_lock)
            _snapshot = _snapshot with
            {
                Player = new PlayerEntry(state.X, state.Y, state.Z, state.MapId)
            };
    }

    public void Flush()
    {
        ProgressSnapshot snap;
        lock (_lock) snap = _snapshot with { SavedAt = DateTime.UtcNow };

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath)!);
        string json = JsonSerializer.Serialize(snap, ProgressJsonContext.Default.ProgressSnapshot);
        File.WriteAllText(OutputPath, json);
    }
}

public sealed record ProgressSnapshot
{
    public DateTime SavedAt     { get; init; } = DateTime.UtcNow;
    public PlayerEntry? Player  { get; init; }
    public IReadOnlyList<BossKillEntry> BossKills    { get; init; } = [];
    public IReadOnlyList<FogGateEntry>  OpenedGates  { get; init; } = [];
}

public sealed record BossKillEntry(string Name, DateTime KilledAt);
public sealed record FogGateEntry(string Name, string MapId);
public sealed record PlayerEntry(float X, float Y, float Z, string MapId);

[JsonSerializable(typeof(ProgressSnapshot))]
[JsonSerializable(typeof(BossKillEntry))]
[JsonSerializable(typeof(FogGateEntry))]
[JsonSerializable(typeof(PlayerEntry))]
internal partial class ProgressJsonContext : JsonSerializerContext { }
