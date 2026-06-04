using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// Resolves DSR's manager pointers at runtime by AOB scan, then exposes the
/// live instance pointers each poll. The static *target* addresses (where the
/// game stores each manager pointer) are fixed for the process lifetime, so we
/// scan once and cache them; the manager instances themselves come and go as
/// saves load/unload, so those are read fresh on every access.
/// </summary>
internal static class GamePointers
{
    private static bool _scanned;
    private static int  _attempts;
    private const  int  MaxAttempts = 10;

    private static nint _worldChrTarget;   // holds WorldChrMan*
    private static nint _chrClassTarget;   // holds GameDataMan*
    private static nint _eventFlagTarget;  // holds EventFlagMan*

    private static void EnsureScanned()
    {
        if (_scanned) return;
        if (_attempts++ >= MaxAttempts) { _scanned = true; return; } // give up; stop scanning

        _worldChrTarget  = ResolveRip(Offsets.WorldChrBaseAOB);
        _chrClassTarget  = ResolveRip(Offsets.ChrClassBaseAOB);
        _eventFlagTarget = ResolveRip(Offsets.EventFlagsAOB);

        // Cache only once everything is found (the AOBs exist as soon as the
        // module is mapped, so this normally succeeds on the first attempt).
        if (_worldChrTarget != 0 && _chrClassTarget != 0 && _eventFlagTarget != 0)
            _scanned = true;
    }

    private static nint ResolveRip(string pattern)
    {
        nint hit = GameMemory.Scan(pattern);
        if (hit == 0) return 0;
        int disp = GameMemory.Read<int>(hit + Offsets.RipDispAt);
        return hit + Offsets.RipInstrLen + disp;
    }

    /// <summary>WorldChrMan instance, or 0 if not loaded.</summary>
    public static nint WorldChrMan
    {
        get { EnsureScanned(); return _worldChrTarget == 0 ? 0 : GameMemory.Read<nint>(_worldChrTarget); }
    }

    /// <summary>Player character (ChrData1), or 0 at menu/loading.</summary>
    public static nint PlayerChr
    {
        get { nint w = WorldChrMan; return w == 0 ? 0 : GameMemory.Read<nint>(w + Offsets.WCM_PlayerChrOff); }
    }

    /// <summary>PlayerGameData (ChrData2: soul level, souls), or 0.</summary>
    public static nint PlayerGameData
    {
        get
        {
            EnsureScanned();
            if (_chrClassTarget == 0) return 0;
            nint gdm = GameMemory.Read<nint>(_chrClassTarget);
            return gdm == 0 ? 0 : GameMemory.Read<nint>(gdm + Offsets.GDM_PlayerDataOff);
        }
    }

    /// <summary>Base of the event-flag bit region (double-dereferenced), or 0.</summary>
    public static nint EventFlagBlock
    {
        get
        {
            EnsureScanned();
            if (_eventFlagTarget == 0) return 0;
            nint man = GameMemory.Read<nint>(_eventFlagTarget);
            return man == 0 ? 0 : GameMemory.Read<nint>(man);
        }
    }

    /// <summary>ChrAnimData struct (player → ChrMapData → ChrAnimData), or 0.</summary>
    public static nint ChrAnimData
    {
        get
        {
            nint chr = PlayerChr;
            if (chr == 0) return 0;
            DsrVersion.Ensure();
            nint map = GameMemory.Read<nint>(chr + Offsets.Chr_MapDataOff + DsrVersion.ChrData1Boost1);
            return map == 0 ? 0 : GameMemory.Read<nint>(map + 0x18);  // ChrMapData.ChrAnimData
        }
    }

    // Fog-gate-pass animations (soulsmodding "DS1 animation IDs"): 7410 =
    // "Walking through fog gate", 7411-7414 related.
    private const int FogAnimLo = 7410;
    private const int FogAnimHi = 7414;

    /// <summary>
    /// Debug helper: scan the player and animation structs for any int field
    /// currently holding a fog-pass animation id (7410-7414). Used to locate the
    /// unknown "current animation id" offset by watching where it appears as the
    /// player crosses a fog. Returns "src+0xOFF=value" hits, or "" if none.
    /// </summary>
    public static string FindFogAnim()
    {
        var sb = new System.Text.StringBuilder();
        Probe(sb, "chr",  PlayerChr,   0x1800);
        Probe(sb, "anim", ChrAnimData, 0x0600);
        return sb.ToString();
    }

    private static void Probe(System.Text.StringBuilder sb, string name, nint baseAddr, int len)
    {
        if (baseAddr == 0) return;
        for (int off = 0; off < len; off += 4)
        {
            int v = GameMemory.Read<int>(baseAddr + off);
            if (v >= FogAnimLo && v <= FogAnimHi) sb.Append($" {name}+0x{off:X}={v}");
        }
    }

    public static string Describe()
    {
        EnsureScanned();
        return $"scan[world=0x{(ulong)_worldChrTarget:X} chrClass=0x{(ulong)_chrClassTarget:X} " +
               $"eventFlag=0x{(ulong)_eventFlagTarget:X}] live[WorldChrMan=0x{(ulong)WorldChrMan:X} " +
               $"Player=0x{(ulong)PlayerChr:X} GameData=0x{(ulong)PlayerGameData:X} FlagBlock=0x{(ulong)EventFlagBlock:X}]";
    }
}
