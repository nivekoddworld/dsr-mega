using DS1Mod.Core;
using DS1Mod.SDK;

namespace DS1Mod.Rando;

public sealed class HpLoggerMod : ModBase
{
    public override string Name    => "Enemy Randomizer";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private IModContext? _ctx;
    private StreamWriter? _log;

    private int _lastHp = -1;             // last HP we logged (-1 = none yet)
    private int _minHp  = int.MaxValue;   // lowest HP seen this session

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        _log = new StreamWriter(Path.Combine(ctx.ModsDir, "HpLogger.log"), append: true) { AutoFlush = true };
        Write("=== HP Logger v1.0.0 loaded — polling HP via OnTick ===");
    }

    public override void OnTick()
    {
        var stats = _ctx?.Reader.GetPlayerStats();
        if (stats is null)
        {
            // No player (menu / loading). Reset so the next spawn logs fresh.
            _lastHp = -1;
            return;
        }

        int hp = stats.CurrentHp;
        if (hp == _lastHp) return;        // OnTick runs ~2x/sec; only log on change

        // Format the change since the last logged value.
        string delta = _lastHp < 0           ? ""
                     : hp < _lastHp          ? $"  ▼{_lastHp - hp}"
                     :                          $"  ▲{hp - _lastHp}";

        _lastHp = hp;
        if (hp > 0 && hp < _minHp) _minHp = hp;

        Write($"HP {hp,4}/{stats.MaxHp} ({stats.HpFraction,4:P0}){delta}");
    }

    public override void OnUnload()
    {
        string low = _minHp == int.MaxValue ? "n/a" : _minHp.ToString();
        Write($"=== HP Logger unloading — lowest HP this session: {low} ===");
        _log?.Dispose();
        _log = null;
    }

    private void Write(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        _log?.WriteLine(line);
        Console.WriteLine($"[HpLogger] {line}");
    }
}
