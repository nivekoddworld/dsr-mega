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
        // Guard-page / null check — anything below 64 KB is invalid on Windows.
        if ((ulong)address < 0x10000) return default;
        return *(T*)address;
    }

    public static unsafe void Write<T>(nint address, T value) where T : unmanaged
    {
        if ((ulong)address < 0x10000) return;
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

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
