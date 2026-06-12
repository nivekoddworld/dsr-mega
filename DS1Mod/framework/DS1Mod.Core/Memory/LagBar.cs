namespace DS1Mod.Core.Memory;

/// <summary>
/// The HP bar's lag value(s) — the lighter "recently lost HP" segment.
/// Once located, mods can pin the segment to an arbitrary HP so the native
/// HUD displays a chosen value — e.g. a Bloodborne-style rally pool —
/// without touching real HP.
///
/// Ground truth (LagBarProbe differential scan + external RTTI walk): the
/// value is a 0..1 ratio float at +0x60 inside NS_FRPG::FrpgMenuDlgObjSmoothGauge.
/// The game keeps TWO live gauges tracking player HP (HUD + menu copy), so
/// both are adopted and pinned — whichever is rendered shows the pin.
/// Gauge objects are heap-allocated per session; they are re-found by RTTI
/// vftable scan, and stale entries are dropped automatically.
/// </summary>
public static class LagBar
{
    private const string GaugeClass = ".?AVFrpgMenuDlgObjSmoothGauge@NS_FRPG@@";
    private const int GaugeValueOff = 0x60;
    private const int MaxAdopt = 3;   // >3 simultaneous HP-tracking gauges means the match is wrong

    private static readonly object _lock = new();
    private static readonly List<LagBarResult> _results = new();

    private static List<(nint Vftable, int Offset)>? _gaugeVfts;
    private static bool _vftLookupDone;

    /// <summary>First resolved write address, or 0 when not located. Mods use
    /// this as the "is the native bar available" check.</summary>
    public static nint Address
    {
        get
        {
            lock (_lock) return _results.Count > 0 ? _results[0].Address : 0;
        }
    }

    /// <summary>
    /// Adopt a probe/diagnostic result — but never overwrite existing
    /// targets (the auto-locator may have already adopted the real gauges
    /// while a probe was still sampling).
    /// </summary>
    public static void Adopt(LagBarResult result)
    {
        lock (_lock)
        {
            if (_results.Count == 0) _results.Add(result);
        }
    }

    public static void Clear()
    {
        lock (_lock) _results.Clear();
    }

    /// <summary>
    /// Locate the player HP gauges automatically: scan the heap for live
    /// FrpgMenuDlgObjSmoothGauge instances (all vftables of the class, with
    /// sub-object offsets corrected) and adopt every one whose ratio tracks
    /// the player's HP. The game keeps two such gauges; both get pinned.
    /// Deferred while ambiguous (e.g. everything reads 1.0 at full bars).
    /// Call periodically while <see cref="Address"/> is 0. Returns true on
    /// adoption.
    /// </summary>
    public static bool TryAutoLocate(Action<string> log)
    {
        lock (_lock) { if (_results.Count > 0) return false; }

        if (!_vftLookupDone)
        {
            _vftLookupDone = true;
            _gaugeVfts = Rtti.FindVftables(GaugeClass);
            if (_gaugeVfts.Count == 0)
                log($"[LagBar] {GaugeClass} vftables not found — native bar pinning unavailable.");
        }
        if (_gaugeVfts == null || _gaugeVfts.Count == 0) return false;

        (int hp, int maxHp) = PlayerBody.ReadHp();
        if (hp <= 0 || maxHp <= 0) return false;
        // At (or near) full HP every idle gauge reads ~1.0 — unidentifiable.
        if (hp >= maxHp - 1) return false;

        ulong[] vftValues = _gaugeVfts.Select(v => (ulong)v.Vftable).ToArray();
        List<nint> hits = HeapScanner.FindQwordValues(vftValues, 128);

        var bases = new HashSet<nint>();
        foreach (nint hitAddr in hits)
        {
            ulong vptr = (ulong)GameMemory.Read<nint>(hitAddr);
            foreach ((nint vft, int off) in _gaugeVfts)
                if (vptr == (ulong)vft) { bases.Add(hitAddr - off); break; }
        }

        var matches = new List<nint>();
        foreach (nint objBase in bases)
        {
            nint addr = objBase + GaugeValueOff;
            if (!LagBarProbe.TryReadAsHp(addr, LagBarKind.Ratio, maxHp, out float v)) continue;
            if (v < 0f || v > maxHp * 1.05f) continue;
            // The gauge tracks hp when idle (it only diverges mid-decay).
            if (Math.Abs(v - hp) <= Math.Max(2f, maxHp * 0.01f))
                matches.Add(addr);
        }

        // None tracking HP yet, or too many matching (full bars are all 1.0):
        // silently retry on a later tick — these states resolve on their own.
        if (matches.Count == 0 || matches.Count > MaxAdopt)
            return false;

        lock (_lock)
        {
            if (_results.Count > 0) return false;   // someone adopted meanwhile
            foreach (nint addr in matches)
                _results.Add(new LagBarResult(addr, LagBarKind.Ratio, -1));
        }
        log($"[LagBar] HP gauge located: {matches.Count} {GaugeClass} value(s) at "
            + string.Join(", ", matches.Select(m => $"0x{(ulong)m:X}"))
            + " (auto, no calibration hit needed).");
        return true;
    }

    /// <summary>
    /// Write <paramref name="hp"/> into every adopted lag value. Entries
    /// whose memory no longer reads as plausible HP (object freed / reused)
    /// are dropped; when the last one goes, <see cref="Address"/> returns 0
    /// and the auto-locator re-finds the new gauges.
    /// </summary>
    public static void Write(int hp, int maxHp)
    {
        lock (_lock)
        {
            for (int i = _results.Count - 1; i >= 0; i--)
            {
                LagBarResult r = _results[i];
                nint addr = r.ChrRelOffset >= 0
                    ? (GamePointers.PlayerChr is var chr && chr != 0 ? chr + r.ChrRelOffset : 0)
                    : r.Address;
                if (addr == 0) continue;

                if (!LagBarProbe.TryReadAsHp(addr, r.Kind, maxHp, out float cur)
                    || cur < -1f || cur > maxHp * 1.5f)
                {
                    if (r.ChrRelOffset < 0) _results.RemoveAt(i);   // stale heap address
                    continue;
                }

                switch (r.Kind)
                {
                    case LagBarKind.Int32:
                        GameMemory.Write(addr, hp);
                        break;
                    case LagBarKind.Float:
                        GameMemory.Write(addr, (float)hp);
                        break;
                    case LagBarKind.Ratio:
                        GameMemory.Write(addr, maxHp > 0 ? (float)hp / maxHp : 0f);
                        break;
                }
            }
        }
    }
}
