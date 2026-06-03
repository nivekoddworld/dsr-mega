using System.Diagnostics;
using System.Runtime.InteropServices;
using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// Represents an open handle to a running DarkSoulsRemastered.exe process.
/// </summary>
public sealed class GameProcess : IDisposable
{
    private const string ProcessName = "DarkSoulsRemastered";

    public nint Handle      { get; }
    public nint ModuleBase  { get; }
    public int  ProcessId   { get; }

    internal MemoryReader Reader { get; }
    internal MemoryWriter Writer { get; }

    private bool _disposed;

    private GameProcess(nint handle, nint moduleBase, int pid)
    {
        Handle     = handle;
        ModuleBase = moduleBase;
        ProcessId  = pid;
        Reader     = new MemoryReader(handle);
        Writer     = new MemoryWriter(handle);
    }

    /// <summary>
    /// Tries to attach to a running DSR process.
    /// Returns null if the process is not found or cannot be opened.
    /// </summary>
    public static GameProcess? TryAttach()
    {
        var procs = Process.GetProcessesByName(ProcessName);
        if (procs.Length == 0) return null;

        var proc = procs[0];
        foreach (var p in procs) if (p != proc) p.Dispose();

        const uint PROCESS_VM_READ      = 0x0010;
        const uint PROCESS_VM_WRITE     = 0x0020;
        const uint PROCESS_VM_OPERATION = 0x0008;
        const uint PROCESS_QUERY_INFO   = 0x0400;

        nint handle = OpenProcess(
            PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFO,
            false, proc.Id);

        if (handle == 0) return null;

        nint moduleBase = proc.MainModule?.BaseAddress ?? 0;
        return new GameProcess(handle, moduleBase, proc.Id);
    }

    public bool IsAlive =>
        !_disposed && WaitForSingleObject(Handle, 0) == 0x00000102; // WAIT_TIMEOUT

    public void Dispose()
    {
        if (!_disposed)
        {
            CloseHandle(Handle);
            _disposed = true;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(nint handle, uint ms);
}
