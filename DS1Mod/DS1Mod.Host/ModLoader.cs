using System.Runtime.InteropServices;
using DS1Mod.Core.Memory;

namespace DS1Mod.Host;

/// <summary>
/// Managed entry point called by the C++ injector via hostfxr
/// load_assembly_and_get_function_pointer.
///
/// Signature must exactly match the component_entry_point_fn typedef:
///   typedef int32_t (HOSTFXR_CALLTYPE *component_entry_point_fn)(void *arg, int32_t arg_size_in_bytes);
/// </summary>
public static class ModLoader
{
    private static ModLifecycleManager? _manager;


    [UnmanagedCallersOnly]
    public static int Initialize(nint arg, int argSizeInBytes)
    {
        try
        {
            GameMemory.Initialize();

            string gameDir = argSizeInBytes > 0
                ? Marshal.PtrToStringUni(arg, argSizeInBytes / 2)?.TrimEnd('\0') ?? ""
                : "";

            string modsDir = Path.Combine(gameDir, "mods");

            _manager = new ModLifecycleManager();
            _manager.LoadMods(gameDir, modsDir);

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DS1Mod.Host] Initialize failed: {ex}");
            return -1;
        }
    }
}