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

    [DllImport("kernel32.dll")]
    private static extern nint VirtualQuery(nint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, nint dwLength);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
