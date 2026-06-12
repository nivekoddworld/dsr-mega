using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

/// <summary>
/// Fires <see cref="EnemyDamaged"/> whenever a loaded non-player character
/// loses HP.
///
/// How enemies are found: characters in DS1R are <c>NS_FRPG::ChrIns</c>
/// subclasses; each concrete class has its own vftable, recoverable at runtime
/// via the RTTI walk (<see cref="Rtti"/>). Live instances are discovered by
/// scanning private heap memory for objects whose vptr equals one of the
/// resolved vftables (<see cref="HeapScanner"/>), then validated by sane
/// HP/MaxHp at the verified ChrData1 offsets (0x3D8/0x3DC + version boost).
/// The player's own object is excluded by pointer.
///
/// Threading model:
///  - Pump thread (Poll, 500 ms): vftable resolution, adoption of scan
///    results, scan scheduling.
///  - Scan thread (lowest priority, one walk per ~10 s): single-pass heap
///    walk matching all vftables at once. Low priority + one pass keeps the
///    copy traffic from causing frame spikes.
///  - Fast diff thread (50 ms): HP diffing over the tracked set and event
///    dispatch — this is what makes damage reaction effectively instant
///    instead of riding the 500 ms pump. Handlers run on this thread and are
///    individually try/caught.
/// Everything is lazy: no subscribers, no threads, no work.
/// </summary>
internal sealed class EnemyDamageHook
{
    public event Action<EnemyDamage>? EnemyDamaged;

    private const int ScanIntervalMs   = 10_000;
    private const int DiffIntervalMs   = 50;
    private const int IdleIntervalMs   = 250;
    private const int MaxPlausibleHp   = 100_000;

    /// <summary>
    /// ChrIns subclasses that can take damage. The concrete enemy class varies
    /// (plain ChrIns hits in practice are factory templates that fail HP
    /// validation), so every plausible RTTI name is resolved and scanned.
    /// </summary>
    private static readonly string[] CandidateClasses =
    {
        ".?AVChrIns@NS_FRPG@@",
        ".?AVNpcIns@NS_FRPG@@",
        ".?AVEnemyIns@NS_FRPG@@",
        ".?AVNpcPlayerIns@NS_FRPG@@",
    };

    private readonly HashSet<nint> _vftables = new();
    private bool _resolved;
    private bool _playerClassLogged;

    private readonly object _sync = new();              // guards _lastHp
    private readonly Dictionary<nint, int> _lastHp = new();
    private int _lastReportedCount = -1;

    private volatile List<nint>? _scanResult;
    private bool _threadsStarted;

    public void Poll()
    {
        if (EnemyDamaged == null) return;
        if (!ResolveVftables()) return;
        DsrVersion.Ensure();
        LogPlayerClassOnce();
        StartThreadsOnce();

        int hpOff  = Offsets.Chr_HpOff    + DsrVersion.ChrData1Boost2;
        int maxOff = Offsets.Chr_MaxHpOff + DsrVersion.ChrData1Boost2;
        nint player = GamePointers.PlayerChr;

        // Adopt results from a finished background scan.
        List<nint>? found = Interlocked.Exchange(ref _scanResult, null);
        if (found == null) return;

        int tracked;
        lock (_sync)
        {
            foreach (nint chr in found)
            {
                if (chr == player || _lastHp.ContainsKey(chr)) continue;
                int hp    = GameMemory.Read<int>(chr + hpOff);
                int maxHp = GameMemory.Read<int>(chr + maxOff);
                // Factory templates / partially initialized objects fail HP
                // sanity and are skipped silently — only live characters track.
                if (maxHp <= 0 || maxHp > MaxPlausibleHp || hp < 0 || hp > maxHp)
                    continue;
                _lastHp[chr] = hp;
            }
            tracked = _lastHp.Count;
        }

        // One-time confirmation that the hook is live; per-scan counts are noise.
        if (_lastReportedCount < 0 && tracked > 0)
        {
            Console.WriteLine($"[EnemyDamageHook] active — tracking {tracked} character(s).");
            _lastReportedCount = tracked;
        }
    }

    private void StartThreadsOnce()
    {
        if (_threadsStarted) return;
        _threadsStarted = true;

        // Scan thread: one full heap walk per interval, lowest priority so the
        // memory traffic never competes with the game's frame.
        var scanThread = new Thread(ScanLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
            Name = "DS1Mod.EnemyScan",
        };
        scanThread.Start();

        // Diff thread: cheap reads at 50 ms — near-instant damage detection.
        var diffThread = new Thread(DiffLoop)
        {
            IsBackground = true,
            Name = "DS1Mod.EnemyDiff",
        };
        diffThread.Start();
    }

    private void ScanLoop()
    {
        ulong[] vfts = _vftables.Select(v => (ulong)v).ToArray();
        while (true)
        {
            try
            {
                if (EnemyDamaged != null && GameState.IsInGame())
                    _scanResult = HeapScanner.FindQwordValues(vfts);
            }
            catch { /* never die */ }
            Thread.Sleep(ScanIntervalMs);
        }
    }

    private void DiffLoop()
    {
        var dead = new List<nint>();
        var events = new List<EnemyDamage>();

        while (true)
        {
            try
            {
                if (EnemyDamaged == null || !GameState.IsInGame())
                {
                    Thread.Sleep(IdleIntervalMs);
                    continue;
                }

                int hpOff  = Offsets.Chr_HpOff    + DsrVersion.ChrData1Boost2;
                int maxOff = Offsets.Chr_MaxHpOff + DsrVersion.ChrData1Boost2;

                (float px, float py, float pz, bool hasPlayerPos) = ReadPosition(GamePointers.PlayerChr);
                dead.Clear();
                events.Clear();

                lock (_sync)
                {
                    foreach ((nint chr, int last) in _lastHp)
                    {
                        // vptr check: the object is gone (or its memory reused)
                        // the moment this stops matching. Read<T> returns
                        // default on unmapped pages.
                        if (!_vftables.Contains(GameMemory.Read<nint>(chr))) { dead.Add(chr); continue; }

                        int hp = GameMemory.Read<int>(chr + hpOff);
                        if (hp < last)
                        {
                            int maxHp = GameMemory.Read<int>(chr + maxOff);

                            float dist = -1f;
                            if (hasPlayerPos)
                            {
                                (float x, float y, float z, bool ok) = ReadPosition(chr);
                                if (ok)
                                {
                                    float dx = x - px, dy = y - py, dz = z - pz;
                                    dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                                }
                            }

                            events.Add(new EnemyDamage(chr, last - hp, hp, maxHp, dist));
                        }
                        _lastHp[chr] = hp;
                    }

                    foreach (nint chr in dead) _lastHp.Remove(chr);
                }

                // Dispatch outside the lock; isolate handler exceptions.
                foreach (EnemyDamage e in events)
                {
                    try { EnemyDamaged?.Invoke(e); }
                    catch (Exception ex) { Console.WriteLine($"[EnemyDamageHook] handler threw: {ex.Message}"); }
                }
            }
            catch { /* never die */ }

            Thread.Sleep(DiffIntervalMs);
        }
    }

    private bool ResolveVftables()
    {
        if (_resolved) return _vftables.Count > 0;
        _resolved = true;

        foreach (string cls in CandidateClasses)
        {
            nint vft = Rtti.FindVftable(cls);
            if (vft != 0)
            {
                _vftables.Add(vft);
                Console.WriteLine($"[EnemyDamageHook] {cls} vftable resolved at 0x{(ulong)vft:X}.");
            }
        }
        if (_vftables.Count == 0)
            Console.WriteLine("[EnemyDamageHook] no character vftables found — EnemyDamaged disabled.");
        return _vftables.Count > 0;
    }

    /// <summary>
    /// Log the player object's RTTI class once — ground truth for what the
    /// engine's character classes are actually called in this binary.
    /// </summary>
    private void LogPlayerClassOnce()
    {
        if (_playerClassLogged) return;
        nint player = GamePointers.PlayerChr;
        if (player == 0) return;
        _playerClassLogged = true;
        string cls = Rtti.GetClassName(GameMemory.Read<nint>(player)) ?? "(no RTTI)";
        Console.WriteLine($"[EnemyDamageHook] player chr 0x{(ulong)player:X} class: {cls}");
    }

    private static (float X, float Y, float Z, bool Ok) ReadPosition(nint chr)
    {
        if (chr == 0) return (0, 0, 0, false);
        nint mapData = GameMemory.Read<nint>(chr + Offsets.Chr_MapDataOff + DsrVersion.ChrData1Boost1);
        if (mapData == 0) return (0, 0, 0, false);
        nint posData = GameMemory.Read<nint>(mapData + Offsets.ChrMap_PosDataOff);
        if (posData == 0) return (0, 0, 0, false);
        return (GameMemory.Read<float>(posData + Offsets.Pos_XOff),
                GameMemory.Read<float>(posData + Offsets.Pos_YOff),
                GameMemory.Read<float>(posData + Offsets.Pos_ZOff),
                true);
    }
}
