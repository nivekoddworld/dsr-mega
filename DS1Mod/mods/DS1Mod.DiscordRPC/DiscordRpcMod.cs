using DiscordRPC;
using DiscordRPC.Logging;
using DS1Mod.SDK;
using DS1Mod.Core;

namespace DS1Mod.DiscordRPC;

/// <summary>
/// Displays Dark Souls Remastered session info in Discord Rich Presence.
/// Art assets required in the Discord developer portal (Rich Presence → Art Assets):
///   Key "ds1_bonfire"  — any DS1 art (large image)
///   Key "ds1_skull"    — a skull icon (small image)
/// </summary>
public sealed class DiscordRpcMod : ModBase
{
    // ── Config ──────────────────────────────────────────────────────────────
    private const string AppId = "1511993705731719268";

    // ── Identity ─────────────────────────────────────────────────────────────
    public override string Name    => "Discord Rich Presence";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    // ── State ────────────────────────────────────────────────────────────────
    private DiscordRpcClient? _client;
    private IModContext?      _ctx;

    private int    _deaths;
    private string _lastBoss        = "None yet";
    private string _currentActivity = "Exploring";  // updated on fog-gate + clear
    private bool   _fightingBoss;

    private readonly DateTime _sessionStart = DateTime.UtcNow;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        _client = new DiscordRpcClient(AppId, logger: new NullLogger());
        _client.OnReady          += (_, e) => Console.WriteLine($"[DiscordRPC] Connected as {e.User.Username}");
        _client.OnConnectionFailed += (_, e) => Console.WriteLine($"[DiscordRPC] Connection failed: {e.FailedPipe}");
        _client.Initialize();

        ctx.Hooks.BossKilled      += OnBossKilled;
        ctx.Hooks.FogGateEntered  += OnFogGateEntered;
        ctx.Hooks.PlayerDied      += OnPlayerDied;

        PushPresence();
        Console.WriteLine("[DiscordRPC] Mod loaded.");
    }

    public override void OnTick()
    {
        _client?.Invoke();   // process Discord callbacks on our thread
        PushPresence();
    }

    public override void OnUnload()
    {
        _client?.ClearPresence();
        _client?.Dispose();
        _client = null;
        Console.WriteLine("[DiscordRPC] Mod unloaded.");
    }

    // ── Hook handlers ────────────────────────────────────────────────────────
    private void OnBossKilled(BossKill kill)
    {
        _lastBoss        = kill.BossName;
        _fightingBoss    = false;
        _currentActivity = "Exploring";
    }

    private void OnFogGateEntered(FogGate fog)
    {
        _fightingBoss    = true;
        _currentActivity = $"Fighting {fog.Name}";
    }

    private void OnPlayerDied()
    {
        _deaths++;
        if (_fightingBoss)
            _currentActivity = $"Died to {_currentActivity.Replace("Fighting ", "")}";
    }

    // ── Presence builder ─────────────────────────────────────────────────────
    private void PushPresence()
    {
        if (_client is null || !_client.IsInitialized) return;

        var stats = _ctx?.Reader.GetPlayerStats();
        var level = _ctx?.Reader.GetSoulLevel() ?? 0;
        var souls = _ctx?.Reader.GetSouls()     ?? 0;

        // Line 1 — activity (what the player is doing right now)
        string details = _currentActivity;

        // Line 2 — numeric summary
        string state = $"SL {level}  |  {souls:N0} souls  |  ☠ {_deaths}";

        // Small tooltip text
        string largeText = _lastBoss == "None yet"
            ? "No bosses slain yet"
            : $"Last kill: {_lastBoss}";

        // HP tooltip on the small image
        string smallText = stats is not null
            ? $"HP {stats.CurrentHp}/{stats.MaxHp}"
            : "Unknown HP";

        _client.SetPresence(new RichPresence
        {
            Details = details,
            State   = state,
            Timestamps = new Timestamps(_sessionStart),
            Assets = new Assets
            {
                LargeImageKey  = "ds1_bonfire",
                LargeImageText = largeText,
                SmallImageKey  = "ds1_skull",
                SmallImageText = smallText,
            },
        });
    }
}
