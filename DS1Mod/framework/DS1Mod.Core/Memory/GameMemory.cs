using System.Runtime.InteropServices;

namespace DS1Mod.Core.Memory;

/// <summary>
/// Direct in-process memory access. Mods run inside DSR's process via the
/// managed host, so we dereference pointers directly rather than using
/// ReadProcessMemory.  Call Initialize() once at startup before any reads.
/// </summary>
public static class GameMemory
{
    public static nint ModuleBase { get; private set; }

    /// <summary>
    /// Capture the base address of DarkSoulsRemastered.exe.
    /// Must be called from the managed entry point before any Hook or Reader
    /// is constructed.
    /// </summary>
    public static void Initialize()
    {
        // GetModuleHandle(null) returns the base of the calling process exe.
        ModuleBase = GetModuleHandle(null);
    }

    public static unsafe T Read<T>(nint address) where T : unmanaged
    {
        // Validate the page before dereferencing. During loading screens and
        // quit-outs the game's manager structures are torn down, leaving stale
        // pointers that look valid (>= 64 KB) but point at unmapped memory.
        // Dereferencing those raises an AccessViolationException — a Corrupted
        // State Exception the pump's try/catch cannot swallow, so it would kill
        // the whole process. VirtualQuery turns that into a graceful default.
        if (!IsReadable(address, sizeof(T))) return default;
        return *(T*)address;
    }

    public static unsafe void Write<T>(nint address, T value) where T : unmanaged
    {
        if (!IsWritable(address, sizeof(T))) return;
        *(T*)address = value;
    }

    /// <summary>
    /// Resolve a static-base → deref → (+offset → deref)* pointer chain.
    /// Returns the final address (not the value at it). Returns 0 on any null
    /// intermediate pointer.
    /// </summary>
    public static nint Resolve(long staticOffset, params int[] offsets)
    {
        nint addr = ModuleBase + (nint)staticOffset;
        addr = Read<nint>(addr);
        if (addr == 0) return 0;

        foreach (int off in offsets)
        {
            addr += off;
            addr = Read<nint>(addr);
            if (addr == 0) return 0;
        }
        return addr;
    }

    // ── page validation ───────────────────────────────────────────────────

    /// <summary>Public probe used for diagnostics — is this address readable?</summary>
    public static bool CanRead(nint address, int size) => IsReadable(address, size);

    /// <summary>True if [address, address+size) is committed and readable.</summary>
    private static bool IsReadable(nint address, int size)
        => ValidateRange(address, size, PageReadMask);

    /// <summary>True if [address, address+size) is committed and writable.</summary>
    private static bool IsWritable(nint address, int size)
        => ValidateRange(address, size, PageWriteMask);

    private static bool ValidateRange(nint address, int size, uint allowedProtect)
    {
        // Reject null / guard-page region without paying for a syscall.
        if ((ulong)address < 0x10000 || size <= 0) return false;

        if (VirtualQuery(address, out MEMORY_BASIC_INFORMATION mbi, MbiSize) == 0)
            return false;

        if (mbi.State != MEM_COMMIT) return false;
        // PAGE_GUARD / PAGE_NOACCESS are never safe to touch.
        if ((mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) != 0) return false;
        if ((mbi.Protect & allowedProtect) == 0) return false;

        // The whole read/write must fit inside this single committed region.
        ulong regionEnd = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;
        return (ulong)address + (ulong)size <= regionEnd;
    }

    private const uint MEM_COMMIT    = 0x1000;
    private const uint PAGE_NOACCESS = 0x001;
    private const uint PAGE_GUARD    = 0x100;

    // Page protections that permit reads / writes respectively.
    private const uint PageReadMask  = 0x02 | 0x04 | 0x08 | 0x20 | 0x40 | 0x80; // RO, RW, WC, XR, XRW, XWC
    private const uint PageWriteMask = 0x04 | 0x08 | 0x40 | 0x80;               // RW, WC, XRW, XWC

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public nint  BaseAddress;
        public nint  AllocationBase;
        public uint  AllocationProtect;
        public nint  RegionSize;
        public uint  State;
        public uint  Protect;
        public uint  Type;
    }

    private static readonly nint MbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

    // ── AOB (signature) scanning ───────────────────────────────────────────

    /// <summary>
    /// Scans the main module for a byte pattern ("48 8B 05 ? ? ? ?", '?' =
    /// wildcard). Returns the absolute address of the first match, or 0.
    /// Walks committed, readable regions only so it never faults.
    /// </summary>
    public static nint Scan(string pattern)
    {
        (byte[] bytes, bool[] match) = ParsePattern(pattern);
        if (bytes.Length == 0) return 0;

        GetModuleBounds(out nint start, out nint size);
        if (start == 0 || size == 0) return 0;

        nint end  = start + size;
        nint addr = start;
        while (addr < end)
        {
            if (VirtualQuery(addr, out MEMORY_BASIC_INFORMATION mbi, MbiSize) == 0) break;
            nint regionEnd = mbi.BaseAddress + mbi.RegionSize;
            if (mbi.RegionSize <= 0) break;

            bool readable = mbi.State == MEM_COMMIT
                && (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) == 0
                && (mbi.Protect & PageReadMask) != 0;

            if (readable)
            {
                nint scanEnd = regionEnd < end ? regionEnd : end;
                nint hit = ScanRegion(addr, scanEnd, bytes, match);
                if (hit != 0) return hit;
            }

            addr = regionEnd;
        }
        return 0;
    }

    /// <summary>
    /// Like <see cref="Scan"/> but collects every match in the main module,
    /// up to <paramref name="max"/> hits.
    /// </summary>
    public static List<nint> ScanAll(string pattern, int max = 16)
    {
        var results = new List<nint>();
        (byte[] bytes, bool[] match) = ParsePattern(pattern);
        if (bytes.Length == 0) return results;

        GetModuleBounds(out nint start, out nint size);
        if (start == 0 || size == 0) return results;

        nint end  = start + size;
        nint addr = start;
        while (addr < end && results.Count < max)
        {
            if (VirtualQuery(addr, out MEMORY_BASIC_INFORMATION mbi, MbiSize) == 0) break;
            nint regionEnd = mbi.BaseAddress + mbi.RegionSize;
            if (mbi.RegionSize <= 0) break;

            bool readable = mbi.State == MEM_COMMIT
                && (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) == 0
                && (mbi.Protect & PageReadMask) != 0;

            if (readable)
            {
                nint scanEnd = regionEnd < end ? regionEnd : end;
                nint from = addr;
                while (results.Count < max)
                {
                    nint hit = ScanRegion(from, scanEnd, bytes, match);
                    if (hit == 0) break;
                    results.Add(hit);
                    from = hit + 1;
                }
            }

            addr = regionEnd;
        }
        return results;
    }

    private static unsafe nint ScanRegion(nint start, nint end, byte[] pat, bool[] match)
    {
        long len = (long)end - (long)start;
        int plen = pat.Length;
        byte* p = (byte*)start;
        for (long i = 0; i + plen <= len; i++)
        {
            int j = 0;
            for (; j < plen; j++)
                if (match[j] && p[i + j] != pat[j]) break;
            if (j == plen) return start + (nint)i;
        }
        return 0;
    }

    private static (byte[], bool[]) ParsePattern(string pattern)
    {
        string[] tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bytes = new byte[tokens.Length];
        var match = new bool[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] is "?" or "??")
            {
                match[i] = false;
            }
            else
            {
                bytes[i] = Convert.ToByte(tokens[i], 16);
                match[i] = true;
            }
        }
        return (bytes, match);
    }

    private static void GetModuleBounds(out nint baseAddr, out nint size)
    {
        baseAddr = ModuleBase;
        size = 0;
        if (baseAddr == 0) return;
        if (GetModuleInformation(GetCurrentProcess(), baseAddr, out MODULEINFO mi, (uint)Marshal.SizeOf<MODULEINFO>()))
            size = (nint)mi.SizeOfImage;
    }

    /// <summary>SizeOfImage of the main module (used for version detection), or 0.</summary>
    public static int ModuleSize
    {
        get { GetModuleBounds(out _, out nint size); return (int)size; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MODULEINFO
    {
        public nint lpBaseOfDll;
        public uint SizeOfImage;
        public nint EntryPoint;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetModuleInformation(nint hProcess, nint hModule, out MODULEINFO lpmodinfo, uint cb);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern nint VirtualQuery(nint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, nint dwLength);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
