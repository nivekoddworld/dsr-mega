using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.SDK;

namespace DS1Mod.DemoMod;

/// <summary>
/// Exercises every surface of the mod SDK:
///   IGamePatcher  — scans game dir, backs up a file before maps load
///   IGameHooks    — all four events (boss kills, fog gates, deaths, level-ups)
///   IGameReader   — HP/stamina, position, map ID, soul level, souls, event flags
///   IGameWriter   — round-trip flag read → write → verify
///   IGuiMod       — overlay window with a probe for ChrInsFactory::create
///   OnTick        — periodic state snapshot
///   OnUnload      — session summary
///
/// Output goes to <modsDir>/DemoMod.log and stdout.
/// </summary>
public sealed class DemoMod : ModBase, IGamePatcher, IGuiMod
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

    // ── ChrInsFactory probe state ─────────────────────────────────────────
    private bool                             _showProbe   = true;
    private ChrInsFactoryProbe.ProbeResult?  _probe;
    private string                           _callStatus  = "";

    // ── IGamePatcher ──────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        //ctx.Log("DemoMod patcher starting");
        //ctx.Log($"  Game dir : {ctx.GameDir}");
        //ctx.Log($"  Mods dir : {ctx.ModsDir}");

        //// Inspect the event/ folder to prove we have file access.
        //string eventDir = Path.Combine(ctx.GameDir, "event");
        //if (Directory.Exists(eventDir))
        //{
        //    int count = Directory.GetFiles(eventDir, "*.dcx").Length;
        //    ctx.Log($"  Found {count} DCX files in event/");
        //}
        //else
        //{
        //    ctx.Log("  event/ not found — is the game UXM-extracted?");
        //}

        //// Demonstrate BackupFile on common.emevd.dcx (most mods that touch EMEVD
        //// would back it up here before patching).
        //string common = Path.Combine(ctx.GameDir, "event", "common.emevd.dcx");
        //if (File.Exists(common))
        //{
        //    ctx.BackupFile(common);
        //    ctx.Log("  Backed up common.emevd.dcx (first run only)");
        //}

        //ctx.Log("DemoMod patcher done — no files modified this run");
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
        // Read flag 16 (Asylum Demon kill), write the same value back, then
        // verify. This is a no-op in practice but exercises the writer path.
        bool before = ctx.Reader.GetEventFlag(16);
        ctx.Writer.SetEventFlag(16, before);
        bool after  = ctx.Reader.GetEventFlag(16);
        Log($"Writer round-trip: flag 16 = {before} → wrote {before} → read {after} " +
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
        bool asylumDone = _ctx.Reader.GetEventFlag(16);   // Asylum Demon
        bool gargoyles  = _ctx.Reader.GetEventFlag(3);    // Bell Gargoyles
        Log($"[Flags] Asylum Demon killed: {asylumDone}  |  Bell Gargoyles killed: {gargoyles}");
    }

    public override void OnUnload()
    {
        Log("=== DemoMod unloading ===");
        Log($"Session summary: {_deaths} death(s), {_bossKills} boss kill(s), {_fogGates} fog gate(s)");
        _log?.Dispose();
        _log = null;
    }

    // ── IGuiMod ───────────────────────────────────────────────────────────────

    public void OnGui()
    {
        if (!_showProbe) return;

        DS1ImGui.SetNextWindowPos(320, 10, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(560, 360, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.85f);

        if (!DS1ImGui.Begin("ChrInsFactory probe", ref _showProbe))
        {
            DS1ImGui.End();
            return;
        }

        DS1ImGui.Text("Walks RTTI from the mangled name to vftable[0].");
        DS1ImGui.Text("Target: NS_FRPG::ChrInsFactory::create");
        DS1ImGui.Separator();

        if (DS1ImGui.Button("Resolve via RTTI walk"))
        {
            try
            {
                _probe = ChrInsFactoryProbe.Resolve();
                Log("[Probe] Resolve clicked. Result:\n" + _probe.Log);
            }
            catch (Exception ex)
            {
                _callStatus = "Resolve threw: " + ex.Message;
                Log("[Probe] Resolve threw: " + ex);
            }
        }

        DS1ImGui.Spacing();

        // ── Result panel ───────────────────────────────────────────────
        if (_probe is null)
        {
            DS1ImGui.Text("(no result — click Resolve)");
        }
        else
        {
            var p = _probe;
            DS1ImGui.Text($"name string  : 0x{(ulong)p.NameStringVA:X}");
            DS1ImGui.Text($"TypeDesc     : 0x{(ulong)p.TypeDescriptorVA:X}");
            DS1ImGui.Text($"COL          : 0x{(ulong)p.ColVA:X}");
            DS1ImGui.Text($"vftable      : 0x{(ulong)p.VftableVA:X}");
            DS1ImGui.Text($"create fn    : 0x{(ulong)p.FunctionVA:X}");

            if (p.EntryBytes.Length > 0)
                DS1ImGui.Text("entry bytes  : " + BytesToHex(p.EntryBytes));

            DS1ImGui.Spacing();
            if (p.Ok)
            {
                DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.4f, 0.9f, 0.4f, 1f);
                DS1ImGui.Text("RESOLVE PASSED  (first byte is 0xE9 — JMP trampoline)");
                DS1ImGui.PopStyleColor();
            }
            else
            {
                DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.95f, 0.5f, 0.4f, 1f);
                DS1ImGui.Text("RESOLVE FAILED — see DemoMod.log for trace.");
                DS1ImGui.PopStyleColor();
            }
        }

        DS1ImGui.Separator();

        // ── Vftable hook section ───────────────────────────────────────
        DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.7f, 0.85f, 1f, 1f);
        DS1ImGui.Text("SIGNATURE CAPTURE");
        DS1ImGui.PopStyleColor();
        DS1ImGui.Text("Swap vftable[0] for a stub that logs args from each call.");
        DS1ImGui.Text("Then load a map / kill enemies / cross a fog gate.");

        bool canHook = _probe is { Ok: true } && ChrInsFactoryHook.Current is not { Installed: true };
        bool canUnhook = ChrInsFactoryHook.Current is { Installed: true };

        if (canHook && DS1ImGui.Button("Install vftable hook"))
        {
            var (ok, msg) = ChrInsFactoryHook.Install(_probe!.VftableVA, _probe.FunctionVA);
            _callStatus = msg;
            Log("[Hook] Install: " + msg);
        }
        if (canUnhook && DS1ImGui.Button("Uninstall vftable hook"))
        {
            var (ok, msg) = ChrInsFactoryHook.Uninstall();
            _callStatus = msg;
            Log("[Hook] Uninstall: " + msg);
        }

        // Live capture display (read fresh every frame).
        var snap = ChrInsFactoryHook.Read();
        if (snap is not null)
        {
            DS1ImGui.Spacing();
            DS1ImGui.Text($"calls observed : {snap.CallCount}");
            DS1ImGui.Text($"this           : 0x{(ulong)snap.This:X}");
            DS1ImGui.Text($"arg1 (RDX)     : 0x{(ulong)snap.Arg1:X}");
            DS1ImGui.Text($"arg2 (R8)      : 0x{(ulong)snap.Arg2:X}");
            DS1ImGui.Text($"arg3 (R9)      : 0x{(ulong)snap.Arg3:X}");
            DS1ImGui.Text($"arg4 (stk+28)  : 0x{(ulong)snap.Arg4:X}");
            DS1ImGui.Text($"arg5 (stk+30)  : 0x{(ulong)snap.Arg5:X}");
            DS1ImGui.Text($"arg6 (stk+38)  : 0x{(ulong)snap.Arg6:X}");
            DS1ImGui.Text($"arg7 (stk+40)  : 0x{(ulong)snap.Arg7:X}");
        }

        DS1ImGui.Separator();

        // ── Dangerous call section ─────────────────────────────────────
        DS1ImGui.PushStyleColor(ImGuiCol.Text, 1f, 0.7f, 0.3f, 1f);
        DS1ImGui.Text("DANGER ZONE");
        DS1ImGui.PopStyleColor();
        DS1ImGui.Text("Calls vftable[0] with NULL this/args. Real signature is");
        DS1ImGui.Text("unknown — this is expected to crash DSR. Use only when");
        DS1ImGui.Text("you're ready to verify it crashes INSIDE the resolved fn.");

        bool canCall = _probe is { Ok: true, FunctionVA: not 0 };
        if (canCall && DS1ImGui.Button("Call create(null, null, null, null)"))
        {
            try
            {
                var (returned, result) = ChrInsFactoryProbe.TryCall(
                    _probe!.FunctionVA, 0, 0, 0, 0);
                _callStatus = returned
                    ? $"call returned 0x{(ulong)result:X} (did NOT crash — unexpected)"
                    : "call setup failed";
                Log($"[Probe] {_callStatus}");
            }
            catch (Exception ex)
            {
                _callStatus = "managed exception: " + ex.Message;
                Log("[Probe] call threw managed exception: " + ex);
            }
        }

        if (!string.IsNullOrEmpty(_callStatus))
        {
            DS1ImGui.Spacing();
            DS1ImGui.Text("last call: " + _callStatus);
        }

        DS1ImGui.End();
    }

    private static string BytesToHex(byte[] b)
    {
        var sb = new System.Text.StringBuilder(b.Length * 3);
        for (int i = 0; i < b.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(b[i].ToString("X2"));
        }
        return sb.ToString();
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
