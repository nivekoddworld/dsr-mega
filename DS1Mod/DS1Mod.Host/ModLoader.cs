using System.Runtime.InteropServices;

namespace DS1Mod.Host;

/// <summary>
/// Managed entry point called by the C++ injector.
/// </summary>
public static class ModLoader
{
    // Keeping this commented out to ensure no dependency issues during bridge testing
    // private static ModLifecycleManager? _manager;

    [UnmanagedCallersOnly(EntryPoint = "Initialize")]
    public static int Initialize(nint arg, int argSizeInBytes)
    {
        // If the log shows "Managed Initialize returned -> 0x00000000", the bridge is fixed.
        // You can then restore the logic.
        return 0;
    }
}