using System.Runtime.InteropServices;

namespace DS1Mod.Rendering;

/// <summary>
/// P/Invoke bindings to the three exports added to dinput8.dll by d3d_hook.cpp.
/// </summary>
internal static partial class NativeD3DHook
{
    // dinput8 is the injector DLL — already loaded in-process.
    private const string Dll = "dinput8";

    [LibraryImport(Dll)]
    internal static partial void DS1Mod_SetOnGuiCallback(nint callback);

    [LibraryImport(Dll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DS1Mod_IsImGuiReady();

    /// <summary>Returns the ImGuiContext* created by the C++ hook.</summary>
    [LibraryImport(Dll)]
    internal static partial nint DS1Mod_GetImGuiContext();
}
