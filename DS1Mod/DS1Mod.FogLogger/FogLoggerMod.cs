using DS1Mod.Core;
using DS1Mod.SDK;

namespace DS1Mod.FogLogger;

/// <summary>
/// Logs every fog wall the player passes through, with a running count and the
/// soul level at the moment of crossing. Writes to the console and to
/// &lt;modsDir&gt;/FogLogger.log.
///
/// Note: DS1 only flags entry for boss fogs, so those are the gates this can
/// detect (see <see cref="DS1Mod.Core.Data.FogGateData"/>).
/// </summary>
public sealed class FogLoggerMod : ModBase
{
    public override string Name    => "Fog Wall Logger";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private IModContext? _ctx;
    private StreamWriter? _log;
    private int _count;

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        string logPath = Path.Combine(ctx.ModsDir, "FogLogger.log");
        _log = new StreamWriter(logPath, append: true) { AutoFlush = true };

        ctx.Hooks.FogGateEntered += OnFogGateEntered;

        Write("=== Fog Wall Logger v1.0.0 — armed, waiting for fog gates ===");
    }

    private void OnFogGateEntered(FogGate gate)
    {
        _count++;

        int soulLevel = _ctx?.Reader.GetSoulLevel() ?? 0;
        string where = string.IsNullOrEmpty(gate.MapId) ? "?" : gate.MapId;

        Write($"#{_count}  {gate.Name}  [{where}, flag {gate.FlagId}]  (SL {soulLevel})");
    }

    public override void OnUnload()
    {
        Write($"=== Fog Wall Logger unloading — {_count} fog wall(s) crossed this session ===");
        _log?.Dispose();
        _log = null;
    }

    private void Write(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _log?.WriteLine(line);
        Console.WriteLine($"[FogLogger] {line}");
    }
}
