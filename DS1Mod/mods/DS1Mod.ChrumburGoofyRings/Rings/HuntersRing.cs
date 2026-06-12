using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Core.Memory;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.ChrumburGoofyRings.Rings;

/// <summary>
/// HUNTERS RING — brings the Bloodborne rally health mechanic to Lordran.
///
/// While the ring is equipped, damage taken is not gone for good: for a short
/// window the lost HP forms a "rally pool"; landing hits on enemies converts
/// pool back into health, 1:1 with the damage dealt (the framework's
/// EnemyDamaged hook fires on a 50 ms diff thread, so the heal is instant).
///
/// Native HUD bar (no ImGui, no HP inflation): real HP stays honest — the
/// pool must never shield the player from death. Instead the game's own
/// "recently lost HP" lag segment (a ratio float at +0x60 in
/// FrpgMenuDlgObjSmoothGauge, auto-located by LagBar via RTTI scan) is pinned
/// at hp + pool while the window is open, so the native bar shows exactly
/// what is recoverable — like Bloodborne's orange segment.
/// </summary>
internal sealed class HuntersRing : IGoofyRing
{
    public string Name => "Hunters Ring";
    public int AccessoryId { get; private set; }

    // Donor accessory row: Havel's Ring (id 100) — exists in every DSR install.
    private const int DonorId = 100;

    // Ledge near the Asylum start.
    private const string Map = IdSpaces.Asylum;
    private static readonly Vector3 PickupPos = new(-13.797f, 190.249f, 15.844f);

    // ── allocated ids ─────────────────────────────────────────────────────
    private int _lotId;
    private int _getFlag;
    private int _entityId;
    private int _evtHide;

    // ── rally state ───────────────────────────────────────────────────────
    private ModConfig? _config;
    private float _rallyPool;                        // recoverable HP
    private float _rallyWindow;                      // seconds left to rally
    private int   _lastHp = -1;
    private long  _tick;
    private volatile bool _rallyActive;              // ring on — read by the hit handler
    private volatile bool _disposed;
    private readonly object _sync = new();           // rally state: fast loop + hit thread
    private bool _armedLogged;
    private string? _lastError;

    // ═════════════════════════════════════════════════════════════════════
    // Config
    // ═════════════════════════════════════════════════════════════════════

    public void InitializeConfig(ModConfig config)
    {
        config.AddFloat("rallyWindowSeconds", 6.0f)
            .Tooltip("Hunters Ring: how long after taking damage the lost HP can still be rallied back");

        config.AddFloat("rallyPercent", 0.85f)
            .Tooltip("Hunters Ring: fraction of damage taken that becomes recoverable rally HP (0.0 - 1.0)");

        config.AddFloat("rallyRegainMultiplier", 1.0f)
            .Tooltip("Hunters Ring: HP regained per hit = damage you dealt x this multiplier (capped by the rally pool). 1.0 = Bloodborne");

        config.AddFloat("maxRallyRangeMeters", 20.0f)
            .Tooltip("Hunters Ring: hits only count if the damaged enemy is within this distance of you (filters enemy infighting). 0 = no limit");

        config.AddBool("damageRefreshesWindow", true)
            .Tooltip("Hunters Ring: taking new damage restarts the rally window (stacking the pool)");

        config.AddBool("probeHudBar", false)
            .Tooltip("DIAGNOSTIC fallback: locate the HP bar lag value by differential scan after a hit. Normally unnecessary — the gauge is auto-located by RTTI");

        config.AddBool("logRally", false)
            .Tooltip("Hunters Ring: log rally gains/losses to the debug console");
    }

    // ═════════════════════════════════════════════════════════════════════
    // Patch — accessory row, text, pickup
    // ═════════════════════════════════════════════════════════════════════

    public void Patch(IPatchContext ctx, GamePatch g, byte[] paramdefs)
    {
        AccessoryId = ctx.AllocateId("EquipParamAccessory");
        _lotId      = ctx.AllocateId(IdSpaces.ItemLotParam);
        _getFlag    = ctx.AllocateId(IdSpaces.ItemObtainedFlags);
        _entityId   = ctx.AllocateId(IdSpaces.MsbEntities(Map));
        _evtHide    = ctx.AllocateId(IdSpaces.EmevdEvents(Map));

        // Passive effect is -1: the rally mechanic is implemented at runtime,
        // not via params.
        g.DefineRing(paramdefs, new RingDef
        {
            Id          = AccessoryId,
            DonorId     = DonorId,
            Name        = "Hunters Ring",
            Description = "Ring of a hunter from a far-off nightmare.\nLost health can be rallied back by pressing on.",
            LongDesc    = "Ring of a hunter from a far-off nightmare, where blood\n"
                        + "is currency and hesitation is defeat.\n\n"
                        + "While worn, health lost to wounds lingers for a moment —\n"
                        + "press the attack without fear, and it shall return to you.",
        });

        g.PlaceWorldPickup(paramdefs, new WorldPickupDef
        {
            LotId        = _lotId,
            ItemId       = AccessoryId,
            Category     = LotCategory.Accessory,
            ObtainedFlag = _getFlag,
            Map          = Map,
            Position     = PickupPos,
            EntityId     = _entityId,
            HideEventId  = _evtHide,
        });

        ctx.Log($"[GoofyRings] Hunters Ring defined: accessory {AccessoryId}, lot {_lotId}, pickup at {PickupPos} ({Map}).");
    }

    // ═════════════════════════════════════════════════════════════════════
    // Runtime
    // ═════════════════════════════════════════════════════════════════════

    public void OnLoad(IModContext ctx, ModConfig config)
    {
        _config = config;

        // The Bloodborne part: every hit landed on an enemy rallies HP back.
        ctx.Hooks.EnemyDamaged += OnEnemyDamaged;

        // Fast loop: damage banking, window countdown, native bar pinning.
        var t = new Thread(FastLoop)
        {
            IsBackground = true,
            Name = "DS1Mod.GoofyRings.HuntersRing",
        };
        t.Start();
    }

    public void OnUnload()
    {
        _disposed = true;
        ResetRally();
    }

    /// <summary>Pump tick (~500 ms): arm/disarm + gauge auto-locate.</summary>
    public void OnTick(bool equipped)
    {
        _tick++;

        (int hp, int maxHp) = PlayerHealth.Read();
        if (hp <= 0 || maxHp <= 0) { ResetRally(); _rallyActive = false; return; }   // dead / not loaded

        // Native HUD bar: find the HP gauge object by RTTI — no calibration
        // hit needed. Throttled; no-op once adopted. Self-heals if the HUD
        // object is freed (LagBar drops a stale address automatically).
        if (LagBar.Address == 0 && _tick % 4 == 0)
            LagBar.TryAutoLocate(Console.WriteLine);

        if (!equipped)
        {
            if (_armedLogged) { Console.WriteLine("[GoofyRings] Hunters Ring unequipped — rally disarmed."); _armedLogged = false; }
            ResetRally();
            _rallyActive = false;
            return;
        }

        _rallyActive = true;
        if (!_armedLogged)
        {
            Console.WriteLine("[GoofyRings] Hunters Ring active — rally armed.");
            _armedLogged = true;
        }
    }

    // ── fast loop (8–50 ms): HP diff, window countdown, bar pinning ──────

    private void FastLoop()
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        double lastSecs = 0;

        while (!_disposed)
        {
            bool pinned = false;
            try
            {
                double now = clock.Elapsed.TotalSeconds;
                float dt = (float)Math.Min(now - lastSecs, 0.5);
                lastSecs = now;

                if (_rallyActive)
                    pinned = RallyStep(dt);
            }
            catch (Exception ex)
            {
                if (_lastError != ex.Message)
                {
                    _lastError = ex.Message;
                    Console.WriteLine($"[GoofyRings] HuntersRing loop failed: {ex.Message}");
                }
            }
            // While pinning, match the game's frame rate — it drains the lag
            // value every frame, and a 50 ms rewrite cadence shows as sawtooth.
            Thread.Sleep(pinned ? 8 : 50);
        }
    }

    /// <summary>
    /// One fast-loop step: diff player HP (bank damage / absorb external
    /// heals), count the window down with real elapsed time, and pin the
    /// native lag bar at hp + pool. Returns true while actively pinning.
    /// </summary>
    private bool RallyStep(float dt)
    {
        (int hp, int maxHp) = PlayerHealth.Read();
        if (hp <= 0 || maxHp <= 0) { ResetRally(); return false; }

        lock (_sync)
        {
            if (_lastHp < 0) { _lastHp = hp; return false; }   // baseline

            int delta = hp - _lastHp;
            if (delta < 0)
            {
                // Took damage — bank a share of it as rally HP.
                int lastHpBeforeHit = _lastHp;
                float gained = -delta * Math.Clamp(_config?.GetFloat("rallyPercent", 0.85f) ?? 0.85f, 0f, 1f);
                if (_config?.GetBool("damageRefreshesWindow", true) == true || _rallyWindow <= 0f)
                    _rallyWindow = Math.Max(0.5f, _config?.GetFloat("rallyWindowSeconds", 6.0f) ?? 6.0f);
                _rallyPool = Math.Min(_rallyPool + gained, maxHp - hp);
                Log($"[GoofyRings] Rally: took {-delta} damage, pool now {(int)_rallyPool} HP ({_rallyWindow:0.0}s window).");

                // Diagnostic fallback: differential scan for the lag value.
                if (LagBar.Address == 0 && _config?.GetBool("probeHudBar", false) == true)
                    LagBarProbe.Start(lastHpBeforeHit, hp, maxHp,
                        result => LagBar.Adopt(result), Console.WriteLine);
            }
            else if (delta > 0)
            {
                // Healed by something else (estus, miracle) — that HP headroom
                // is no longer rallyable. Rally heals update _lastHp in
                // OnEnemyDamaged, so they never reach this branch.
                _rallyPool = Math.Max(0f, _rallyPool - delta);
            }

            _rallyWindow = Math.Max(0f, _rallyWindow - dt);
            if (_rallyWindow <= 0f && _rallyPool > 0f)
            {
                Log($"[GoofyRings] Rally window closed — {(int)_rallyPool} HP lost for good.");
                _rallyPool = 0f;
            }

            _lastHp = hp;

            // Pin the native lag bar while there is a pool to show.
            if (_rallyPool > 0f && _rallyWindow > 0f && LagBar.Address != 0)
            {
                LagBar.Write(Math.Min(hp + (int)_rallyPool, maxHp), maxHp);
                return true;
            }
            return false;
        }
    }

    // ── hit handler: pool → health, Bloodborne's exact rule ──────────────

    /// <summary>
    /// Fired by the framework's EnemyDamaged hook on its fast (50 ms) diff
    /// thread, so the heal lands essentially the moment the hit connects.
    /// </summary>
    private void OnEnemyDamaged(EnemyDamage hit)
    {
        lock (_sync)
        {
            if (!_rallyActive || _rallyPool <= 0f || _rallyWindow <= 0f)
            {
                Log($"[GoofyRings] hit seen ({hit.Damage} dmg, {hit.DistanceToPlayer:0.0}m) but no rally: " +
                    $"active={_rallyActive} pool={(int)_rallyPool} window={_rallyWindow:0.0}s");
                return;
            }

            // Attribute the hit to the player: it must have landed near us.
            float maxRange = _config?.GetFloat("maxRallyRangeMeters", 20.0f) ?? 20.0f;
            if (maxRange > 0f && hit.DistanceToPlayer >= 0f && hit.DistanceToPlayer > maxRange) return;

            (int hp, int maxHp) = PlayerHealth.Read();
            if (hp <= 0 || maxHp <= 0) return;

            float mult = Math.Max(0f, _config?.GetFloat("rallyRegainMultiplier", 1.0f) ?? 1.0f);
            int regain = (int)Math.Min(Math.Min(hit.Damage * mult, _rallyPool), maxHp - hp);
            if (regain <= 0) return;

            PlayerHealth.SetHp(hp + regain);
            _rallyPool -= regain;
            if (_lastHp >= 0) _lastHp += regain;   // our own write isn't "external healing"
            Log($"[GoofyRings] Rally hit for {hit.Damage} — regained {regain} HP ({hp + regain}/{maxHp}), {(int)_rallyPool} left in pool.");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private void ResetRally()
    {
        lock (_sync)
        {
            _rallyPool = 0f;
            _rallyWindow = 0f;
            _lastHp = -1;
        }
    }

    private void Log(string message)
    {
        if (_config?.GetBool("logRally", false) == true)
            Console.WriteLine(message);
    }
}
