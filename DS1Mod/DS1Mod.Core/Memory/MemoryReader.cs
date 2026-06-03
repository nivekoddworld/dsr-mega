using System.Runtime.InteropServices;

namespace DS1Mod.Core.Memory;

internal sealed class MemoryReader
{
    private readonly nint _processHandle;

    public MemoryReader(nint processHandle)
    {
        _processHandle = processHandle;
    }

    public unsafe T Read<T>(nint address) where T : unmanaged
    {
        T value = default;
        ReadProcessMemory(_processHandle, address, &value, (nuint)sizeof(T), out _);
        return value;
    }

    /// <summary>
    /// Resolves a pointer chain: reads a pointer at (base + staticOffset),
    /// then dereferences each successive offset.
    /// Returns the final address (not the value at it).
    /// Returns 0 if any dereference lands on a null/invalid pointer.
    /// </summary>
    public nint ResolveChain(nint moduleBase, long staticOffset, params int[] offsets)
    {
        nint addr = moduleBase + (nint)staticOffset;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool ReadProcessMemory(
        nint hProcess, nint lpBaseAddress,
        void* lpBuffer, nuint nSize, out nuint lpNumberOfBytesRead);
}
