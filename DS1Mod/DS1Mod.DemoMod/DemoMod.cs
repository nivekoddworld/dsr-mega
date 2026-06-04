using DS1Mod.Core;
using DS1Mod.SDK;

namespace DS1Mod.DemoMod;

/// <summary>
/// Exercises every surface of the mod SDK:
///   IGamePatcher  — scans game dir, backs up a file before maps load
///   IGameHooks    — all four events (boss kills, fog gates, deaths, level-ups)
///   IGameReader   — HP/stamina, position, map ID, soul level, souls, event flags
///   IGameWriter   — round-trip flag read → write → verify
///   OnTick        — periodic state snapshot
///   OnUnload      — session summary
///
/// Output goes to <modsDir>/DemoMod.log and stdout.
/// </summary>
public sealed class DemoMod : ModBase, IGamePatcher
{
    public override string Name    => "DS1 Demo Mod";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private IModContext? _ctx;
    private StreamWriter? _log;

    private int _tickCount  = 0;
    private int _deaths     = 0;
    private int _bossKills  = 0;
    private int _fogGates   = 0;

    // ── IGamePatcher ──────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        ctx.Log("DemoMod patcher starting");
        ctx.Log($"  Game dir : {ctx.GameDir}");
        ctx.Log($"  Mods dir : {ctx.ModsDir}");

        // Inspect the event/ folder to prove we have file access.
        string eventDir = Path.Combine(ctx.GameDir, "event");
        if (Directory.Exists(eventDir))
        {
            int count = Directory.GetFiles(eventDir, "*.dcx").Length;
            ctx.Log($"  Found {count} DCX files in event/");
        }
        else
        {
            ctx.Log("  event/ not found — is the game UXM-extracted?");
        }

        // Demonstrate BackupFile on common.emevd.dcx (most mods that touch EMEVD
        // would back it up here before patching).
        string common = Path.Combine(ctx.GameDir, "event", "common.emevd.dcx");
        if (File.Exists(common))
        {
            ctx.BackupFile(common);
            ctx.Log("  Backed up common.emevd.dcx (first run only)");
        }

        ctx.Log("DemoMod patcher done — no files modified this run");
    }

    // ── IGameMod ──────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        // Open session log.
        string logPath = Path.Combine(ctx.ModsDir, "DemoMod.log");
        _log = new StreamWriter(logPath, append: false) { AutoFlush = true };

        Log("=== DemoMod v1.0.0 loaded ===");
        Log($"Mods dir : {ctx.ModsDir}");

        // ── Subscribe to every hook ────────────────────────────────────────
        ctx.Hooks.BossKilled      += OnBossKilled;
        ctx.Hooks.FogGateEntered  += OnFogGateEntered;
        ctx.Hooks.PlayerDied      += OnPlayerDied;
        ctx.Hooks.PlayerLeveledUp += OnPlayerLeveledUp;

        Log("All hooks subscribed — waiting for game events");

        // ── IGameWriter round-trip demo ────────────────────────────────────
        // Read flag 11010000 (Asylum Demon kill), write the same value back,
        // then verify. This is a no-op in practice but exercises the writer path.
        bool before = ctx.Reader.GetEventFlag(11010000);
        ctx.Writer.SetEventFlag(11010000, before);
        bool after  = ctx.Reader.GetEventFlag(11010000);
        Log($"Writer round-trip: flag 11010000 = {before} → wrote {before} → read {after} " +
            $"({(before == after ? "OK" : "MISMATCH!")})");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;

        _tickCount++;

        // Log a full state snapshot every 10 ticks (≈5 s).
        if (_tickCount % 10 != 0) return;

        // ── IGameReader: player stats ──────────────────────────────────────
        var stats = _ctx.Reader.GetPlayerStats();
        if (stats is not null)
        {
            Log($"[Stats] HP {stats.CurrentHp}/{stats.MaxHp} ({stats.HpFraction,5:P0}) | " +
                $"Stamina {stats.CurrentStamina:F0}/{stats.MaxStamina:F0} ({stats.StaminaFraction,5:P0})");
        }
        else
        {
            Log("[Stats] No player stats (load screen / menu?)");
        }

        // ── IGameReader: player state (position + map) ─────────────────────
        var state = _ctx.Reader.GetPlayerState();
        if (state is not null)
        {
            Log($"[State] Map: {(string.IsNullOrEmpty(state.MapId) ? "unknown" : state.MapId)} | " +
                $"Pos ({state.X,8:F2}, {state.Y,8:F2}, {state.Z,8:F2})");
        }

        // ── IGameReader: souls + soul level ───────────────────────────────
        int souls = _ctx.Reader.GetSouls();
        int level = _ctx.Reader.GetSoulLevel();
        Log($"[Souls] SL {level}  —  {souls:N0} souls held");

        // ── IGameReader: spot-check a known event flag ─────────────────────
        bool asylumDone = _ctx.Reader.GetEventFlag(11010000);
        bool gargoyles  = _ctx.Reader.GetEventFlag(11010100);
        Log($"[Flags] Asylum Demon killed: {asylumDone}  |  Bell Gargoyles killed: {gargoyles}");
    }

    public override void OnUnload()
    {
        Log("=== DemoMod unloading ===");
        Log($"Session summary: {_deaths} death(s), {_bossKills} boss kill(s), {_fogGates} fog gate(s)");
        _log?.Dispose();
        _log = null;
    }

    // ── Hook handlers ─────────────────────────────────────────────────────────

    private void OnBossKilled(BossKill kill)
    {
        _bossKills++;
        Log($"[BOSS] {kill.BossName} killed at {kill.KilledAt:HH:mm:ss} (flag {kill.FlagId})");

        // Verify the kill flag is actually set via IGameReader.
        bool confirmed = _ctx?.Reader.GetEventFlag(kill.FlagId) ?? false;
        Log($"[BOSS] Flag {kill.FlagId} confirmed via Reader: {confirmed}");

        // Celebrate with a silly Windows beep fanfare on a background thread
        // so we don't hold up the hook dispatcher.
        Task.Run(PlayVictoryFanfare);
    }

    private static void PlayVictoryFanfare()
    {
        // Ascending major arpeggio — C E G C — classic "ta-daaa"
        (int freq, int ms)[] notes =
        [
            (523,  120),   // C4
            (659,  120),   // E4
            (784,  120),   // G4
            (1047, 350),   // C5
        ];

        foreach (var (freq, ms) in notes)
        {
            try { Console.Beep(freq, ms); }
            catch { /* Beep can fail in some console configurations */ }
        }
    }

    private void OnFogGateEntered(FogGate gate)
    {
        _fogGates++;
        Log($"[FOG]  Entered: {gate.Name} (map {gate.MapId}, flag {gate.FlagId})");
    }

    private void OnPlayerDied()
    {
        _deaths++;
        Log($"[DIED] You died! Session deaths: {_deaths}");

        // Read HP immediately to confirm it's 0.
        var stats = _ctx?.Reader.GetPlayerStats();
        int hp = stats?.CurrentHp ?? -1;
        Log($"[DIED] HP at death read: {hp}");
    }

    private void OnPlayerLeveledUp(int newLevel)
    {
        Log($"[LEVEL] Soul Level → {newLevel}");

        // Read remaining souls right after level-up.
        int souls = _ctx?.Reader.GetSouls() ?? 0;
        Log($"[LEVEL] Souls remaining: {souls:N0}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        _log?.WriteLine(line);
        Console.WriteLine($"[DemoMod] {line}");
    }
}
