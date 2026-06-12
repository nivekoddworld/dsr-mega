using System.Runtime.InteropServices;

namespace DS1Mod.Core.Memory;

/// <summary>How the lag-bar value encodes HP.</summary>
public enum LagBarKind
{
    /// <summary>32-bit int holding raw HP.</summary>
    Int32,
    /// <summary>32-bit float holding raw HP.</summary>
    Float,
    /// <summary>32-bit float holding hp / maxHp (0..1).</summary>
    Ratio,
}

/// <summary>One surviving candidate from a differential scan.</summary>
public sealed record LagBarResult(nint Address, LagBarKind Kind, int ChrRelOffset);

/// <summary>
/// Cheat-Engine-style differential scanner for the HP bar's "recently lost
/// HP" lag value — the lighter segment that appears at the pre-damage HP
/// level and drains down to the current HP over about a second.
///
/// Strategy (one-time calibration, triggered right after the player takes a
/// hit): pass 1 walks all committed private RW memory collecting every
/// 4-aligned dword that currently encodes the PRE-damage HP — as a raw int,
/// a raw float, or a 0..1 ratio of maxHp. Candidates are then re-sampled
/// every ~120 ms; only values that decay monotonically and finish at the
/// post-damage HP survive. That decay signature is unique to the lag bar.
///
/// All reads go through ReadProcessMemory on our own process so a page that
/// gets decommitted mid-scan returns false instead of faulting DSR.
/// </summary>
public static class LagBarProbe
{
    private const int ChunkSize     = 1 << 20;
    private const int MaxCandidates = 100_000;
    private const int SampleMs      = 120;
    private const int Samples       = 14;       // ~1.7 s — the bar lingers, then drains

    private static int _running;

    /// <summary>
    /// Launch the probe on a background thread. <paramref name="onFound"/>
    /// fires (on the probe thread) with the best survivor, if any.
    /// No-op if a probe is already in flight.
    /// </summary>
    public static void Start(int preHp, int postHp, int maxHp,
        Action<LagBarResult> onFound, Action<string> log)
    {
        if (preHp <= postHp || maxHp <= 0) return;
        if (preHp >= maxHp - 1)
        {
            // At full HP "encodes preHp" matches every maxHp copy and every
            // 1.0f in the process — pass 1 drowns in false positives.
            log("[LagBarProbe] skipped: hit taken at full HP is ambiguous (preHp == maxHp). " +
                "Take a hit while already below full HP to calibrate.");
            return;
        }
        if (Interlocked.Exchange(ref _running, 1) == 1) return;

        var t = new Thread(() =>
        {
            try { Run(preHp, postHp, maxHp, onFound, log); }
            catch (Exception ex) { log($"[LagBarProbe] failed: {ex.Message}"); }
            finally { _running = 0; }
        })
        {
            IsBackground = true,
            Name = "DS1Mod.LagBarProbe",
            // Above normal: pass 1 races the ~1 s decay of the very value
            // it is looking for. One-time calibration; a brief hitch is fine.
            Priority = ThreadPriority.AboveNormal,
        };
        t.Start();
    }

    private static void Run(int preHp, int postHp, int maxHp,
        Action<LagBarResult> onFound, Action<string> log)
    {
        float ratioPre  = (float)preHp / maxHp;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // ── pass 1: snapshot everything that currently encodes preHp ─────
        var candidates = CollectCandidates(preHp, ratioPre);
        log($"[LagBarProbe] pass 1: {candidates.Count} candidate(s) encoding pre-damage HP {preHp} ({sw.ElapsedMilliseconds} ms)."
            + (candidates.Count >= MaxCandidates ? " CAP HIT — coverage incomplete, results unreliable." : ""));
        if (candidates.Count == 0) return;

        // ── passes 2..N: keep only monotonically decaying values ─────────
        var last = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++) last[i] = preHp;

        var alive = new bool[candidates.Count];
        for (int i = 0; i < alive.Length; i++) alive[i] = true;

        for (int s = 0; s < Samples; s++)
        {
            Thread.Sleep(SampleMs);

            // The probe assumes one clean hit followed by a quiet second. If
            // the player's HP moves again mid-sample (another hit, a rally
            // heal, estus), every legitimate candidate now tracks the NEW hp
            // and the decay model is invalid — abort and wait for a clean hit.
            (int hpNowPlayer, _) = PlayerBody.ReadHp();
            if (hpNowPlayer != postHp)
            {
                log($"[LagBarProbe] aborted: player HP changed mid-probe ({postHp} → {hpNowPlayer}). " +
                    "Take one hit, then stand still (don't attack) for ~2 s to calibrate.");
                return;
            }

            int remaining = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!alive[i]) continue;
                (nint addr, LagBarKind kind) = candidates[i];
                if (!TryReadAsHp(addr, kind, maxHp, out float hpNow)) { alive[i] = false; continue; }

                // Must decay (or hold) — never rise above the previous sample,
                // and never fall below the post-damage floor.
                if (hpNow > last[i] + 1.5f || hpNow < postHp - 1.5f) { alive[i] = false; continue; }
                last[i] = hpNow;
                remaining++;
            }
            if (remaining == 0)
            {
                log("[LagBarProbe] all candidates eliminated — no lag-bar value found this hit. " +
                    "Re-trigger by taking another hit (the value may be UI-side and map-dependent).");
                return;
            }
        }

        // ── final filter: must have actually moved and ended near postHp ─
        var survivors = new List<(nint Addr, LagBarKind Kind, float Final)>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!alive[i]) continue;
            // Reject constants (never moved off preHp) — defense stats etc.
            if (last[i] > preHp - 1.5f) continue;
            if (Math.Abs(last[i] - postHp) > 2.0f) continue;
            survivors.Add((candidates[i].Addr, candidates[i].Kind, last[i]));
        }

        log($"[LagBarProbe] {survivors.Count} survivor(s) after {sw.ElapsedMilliseconds} ms:");
        nint chr = GamePointers.PlayerChr;
        LagBarResult? best = null;
        foreach ((nint addr, LagBarKind kind, float fin) in survivors)
        {
            int rel = -1;
            if (chr != 0)
            {
                long d = addr - chr;
                if (d >= 0 && d < 0x8000) rel = (int)d;
            }
            log($"[LagBarProbe]   0x{(ulong)addr:X} kind={kind} final={fin:0.#}" +
                (rel >= 0 ? $"  ← PlayerChr+0x{rel:X} (STABLE — report this offset!)" : "  (heap, session-local)"));

            var r = new LagBarResult(addr, kind, rel);
            // Prefer chr-relative (survives reloads), then ratio floats
            // (typical FE/HUD encoding), then anything.
            if (best == null
                || (rel >= 0 && best.ChrRelOffset < 0)
                || (rel >= 0 == best.ChrRelOffset >= 0 && kind == LagBarKind.Ratio && best.Kind != LagBarKind.Ratio))
                best = r;
        }

        if (best != null)
        {
            log($"[LagBarProbe] adopting 0x{(ulong)best.Address:X} ({best.Kind}" +
                (best.ChrRelOffset >= 0 ? $", PlayerChr+0x{best.ChrRelOffset:X}" : "") + ").");
            onFound(best);
        }
    }

    // ── pass 1 heap walk ──────────────────────────────────────────────────

    private static unsafe List<(nint Addr, LagBarKind Kind)> CollectCandidates(int preHp, float ratioPre)
    {
        var hits = new List<(nint, LagBarKind)>();
        byte[] buffer = new byte[ChunkSize];
        nint self = GetCurrentProcess();
        nint addr = 0x10000;
        float fPre = preHp;
        bool wantRatio = ratioPre > 0.0001f && ratioPre <= 1.0001f;

        while (hits.Count < MaxCandidates
            && VirtualQuery(addr, out MEMORY_BASIC_INFORMATION mbi, MbiSize) != 0)
        {
            nint regionEnd = mbi.BaseAddress + mbi.RegionSize;
            if (mbi.RegionSize <= 0) break;

            bool scannable = mbi.State == MEM_COMMIT
                && mbi.Type == MEM_PRIVATE
                && (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) == 0
                && (mbi.Protect & PAGE_READWRITE) != 0;

            if (scannable)
            {
                for (nint chunk = mbi.BaseAddress; chunk < regionEnd && hits.Count < MaxCandidates; chunk += ChunkSize)
                {
                    nint len = nint.Min(ChunkSize, regionEnd - chunk);
                    bool ok;
                    fixed (byte* b = buffer)
                        ok = ReadProcessMemory(self, chunk, b, len, out _);
                    if (!ok) continue;

                    fixed (byte* b = buffer)
                    {
                        // Skip phantom hits inside our own copy buffer (it is
                        // heap memory full of bytes copied from prior chunks).
                        nint bufLo = (nint)b, bufHi = (nint)(b + buffer.Length);
                        int n = (int)len / 4;
                        int*   ip = (int*)b;
                        float* fp = (float*)b;
                        for (int i = 0; i < n && hits.Count < MaxCandidates; i++)
                        {
                            nint hit = chunk + i * 4;
                            if (hit >= bufLo && hit < bufHi) continue;
                            if (ip[i] == preHp)
                                hits.Add((hit, LagBarKind.Int32));
                            else
                            {
                                float f = fp[i];
                                if (f > fPre - 0.6f && f < fPre + 0.6f)
                                    hits.Add((hit, LagBarKind.Float));
                                else if (wantRatio && f > ratioPre - 0.0005f && f < ratioPre + 0.0005f)
                                    hits.Add((hit, LagBarKind.Ratio));
                            }
                        }
                    }
                }
            }
            addr = regionEnd;
        }
        Array.Clear(buffer);   // no stale copies for later scans to find
        return hits;
    }

    // ── safe candidate re-read ────────────────────────────────────────────

    internal static unsafe bool TryReadAsHp(nint addr, LagBarKind kind, int maxHp, out float hp)
    {
        hp = 0f;
        uint raw;
        if (!ReadProcessMemory(GetCurrentProcess(), addr, (byte*)&raw, 4, out _)) return false;
        switch (kind)
        {
            case LagBarKind.Int32:
                hp = (int)raw;
                return true;
            case LagBarKind.Float:
                hp = BitConverter.Int32BitsToSingle((int)raw);
                return float.IsFinite(hp);   // NaN passes every comparison filter — reject here
            case LagBarKind.Ratio:
                float ratio = BitConverter.Int32BitsToSingle((int)raw);
                if (!float.IsFinite(ratio)) return false;
                hp = ratio * maxHp;
                return true;
            default: return false;
        }
    }

    // ── interop ───────────────────────────────────────────────────────────

    private const uint MEM_COMMIT     = 0x1000;
    private const uint MEM_PRIVATE    = 0x20000;
    private const uint PAGE_NOACCESS  = 0x001;
    private const uint PAGE_GUARD     = 0x100;
    private const uint PAGE_READWRITE = 0x04;

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private static readonly nint MbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

    [DllImport("kernel32.dll")]
    private static extern nint VirtualQuery(nint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, nint dwLength);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool ReadProcessMemory(nint hProcess, nint baseAddress, byte* buffer, nint size, out nint bytesRead);
}
