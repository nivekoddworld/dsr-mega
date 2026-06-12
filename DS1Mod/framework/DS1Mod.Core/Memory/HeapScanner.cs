using System.Runtime.InteropServices;

namespace DS1Mod.Core.Memory;

/// <summary>
/// Scans the process's private heap memory. Game objects (ChrIns, managers,
/// the inventory block) live in MEM_PRIVATE allocations that
/// <see cref="GameMemory.Scan"/> (module-only) cannot see.
///
/// Regions are copied with ReadProcessMemory (on our own process) before
/// scanning. Direct dereferencing would race the game: a region observed as
/// committed by VirtualQuery can be decommitted before we read it, and the
/// resulting AccessViolation is a corrupted-state exception that kills DSR.
/// RPM just returns false on unmapped pages.
/// </summary>
public static class HeapScanner
{
    private const int ChunkSize = 1 << 20;   // 1 MB copy granularity

    [ThreadStatic] private static byte[]? _buffer;

    /// <summary>
    /// Find every 8-aligned qword in committed private RW memory equal to
    /// <paramref name="value"/>. The classic use is vptr scanning: pass a
    /// vftable address and the hits are the live instances of that class.
    /// </summary>
    public static List<nint> FindQwordValue(ulong value, int max = 4096)
        => FindQwordValues(new[] { value }, max);

    /// <summary>
    /// Single-pass variant matching any of <paramref name="values"/> — one
    /// heap walk regardless of how many vftables are being looked for.
    /// </summary>
    public static unsafe List<nint> FindQwordValues(ulong[] values, int max = 4096)
    {
        var hits = new List<nint>();
        byte[] buffer = _buffer ??= new byte[ChunkSize];
        nint self = GetCurrentProcess();
        nint addr = 0x10000;

        while (hits.Count < max
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
                for (nint chunk = mbi.BaseAddress; chunk < regionEnd && hits.Count < max; chunk += ChunkSize)
                {
                    nint len = nint.Min(ChunkSize, regionEnd - chunk);
                    bool ok;
                    fixed (byte* b = buffer)
                        ok = ReadProcessMemory(self, chunk, b, len, out _);
                    if (!ok) continue;   // region vanished — skip, never fault

                    fixed (byte* b = buffer)
                    {
                        // Our own copy buffer lives in this same scannable
                        // heap; bytes copied during previous chunks would be
                        // found as phantom hits. Skip anything that resolves
                        // into the buffer itself.
                        nint bufLo = (nint)b, bufHi = (nint)(b + buffer.Length);
                        ulong* p   = (ulong*)b;
                        ulong* end = (ulong*)(b + len - 7);
                        for (; p < end && hits.Count < max; p++)
                        {
                            ulong v = *p;
                            for (int i = 0; i < values.Length; i++)
                            {
                                if (v == values[i])
                                {
                                    nint hit = chunk + (nint)((byte*)p - b);
                                    if (hit < bufLo || hit >= bufHi)
                                        hits.Add(hit);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            addr = regionEnd;
        }

        // Don't leave copied game bytes (vftable values!) in the buffer —
        // the NEXT scan would find them as phantom instances. The skip above
        // only protects against the buffer's current address; after a GC
        // moves/abandons it, stale copies elsewhere would still match.
        Array.Clear(buffer);
        return hits;
    }

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
