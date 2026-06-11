using DS1Mod.Core;
using DS1Mod.SDK;

namespace DS1Mod.FogLogger;

/// <summary>
/// Logs every fog wall the player passes through, with a running count, the
/// player's position, and soul level at the moment of crossing. Detection is
/// animation-based (the "walking through fog gate" animation), so it catches
/// every fog wall — not just the boss fogs that set event flags. Writes to the
/// console and to &lt;modsDir&gt;/FogLogger.log.
/// </summary>
public sealed class FogLoggerMod : ModBase
{
    public override string Name    => "Fog Wall Logger";
    public override string Version => "1.1.0";
    public override string Author  => "DS1MegaRando";

    private IModContext? _ctx;
    private StreamWriter? _log;
    private int _count;

    public override void InitializeConfig(ModConfig config)
    {
        config.AddBool("enableFileLogging", true);
        config.AddBool("enableConsoleLogging", true);
        config.AddBool("logCoordinates", true);
    }

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        string logPath = Path.Combine(ctx.ModsDir, "FogLogger.log");
        _log = new StreamWriter(logPath, append: true) { AutoFlush = true };

        ctx.Hooks.FogGateEntered += OnFogGateEntered;

        Write("=== Fog Wall Logger v1.1.0 — armed, waiting for fog gates ===");
    }

    private void OnFogGateEntered(FogGate gate)
    {
        _count++;

        int soulLevel = _ctx?.Reader.GetSoulLevel() ?? 0;
        var pos = _ctx?.Reader.GetPlayerState();
        string where = pos is null
            ? "unknown position"
            : $"({pos.X,8:F2}, {pos.Y,8:F2}, {pos.Z,8:F2})";

        Write($"#{_count}  passed through a fog wall at {where}  (SL {soulLevel}, anim {gate.FlagId})");
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
