using System.Runtime.InteropServices;

namespace DS1Mod.Core.Memory;

internal sealed class MemoryWriter
{
    private readonly nint _processHandle;

    public MemoryWriter(nint processHandle)
    {
        _processHandle = processHandle;
    }

    public unsafe bool Write<T>(nint address, T value) where T : unmanaged
    {
        return WriteProcessMemory(_processHandle, address, &value, (nuint)sizeof(T), out _);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool WriteProcessMemory(
        nint hProcess, nint lpBaseAddress,
        void* lpBuffer, nuint nSize, out nuint lpNumberOfBytesWritten);
}
