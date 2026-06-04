using System.Runtime.InteropServices;
using DS1Mod.Core;
using ImGuiNET;

namespace DS1Mod.Rendering;

/// <summary>
/// Bridges the C++ Present hook and the managed <see cref="IGuiMod"/> mods.
///
/// Call <see cref="Initialize"/> once after mods have been loaded.
/// The renderer registers a managed delegate with the C++ hook so that
/// every frame's <c>OnImGuiFrame</c> is driven from Present.
///
/// Thread safety: <c>OnImGuiFrame</c> executes on the game's render thread.
/// The mod list is set once during init and never mutated afterwards.
/// </summary>
public sealed class ImGuiRenderer : IDisposable
{
    // Delegate kept alive for the lifetime of the renderer.
    // The GC must not collect it while the C++ hook holds a raw function pointer.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnGuiDelegate();

    private readonly IReadOnlyList<IGuiMod> _mods;
    private readonly OnGuiDelegate          _delegate;
    private          bool                   _contextSynced;
    private          bool                   _disposed;

    public ImGuiRenderer(IReadOnlyList<IGuiMod> mods)
    {
        _mods     = mods;
        _delegate = OnImGuiFrame;

        nint fnPtr = Marshal.GetFunctionPointerForDelegate(_delegate);
        NativeD3DHook.DS1Mod_SetOnGuiCallback(fnPtr);

        Console.WriteLine($"[DS1Mod.Rendering] Registered OnGui callback for {_mods.Count} mod(s).");
    }

    /// <summary>
    /// Called every frame by the C++ Present hook, on the render thread.
    /// </summary>
    private void OnImGuiFrame()
    {
        if (_disposed) return;

        // On the very first call the C++ side has just finished initialising
        // ImGui and the D3D11 backend.  Sync the managed ImGui context so
        // ImGui.NET's calls operate on the same instance.
        if (!_contextSynced)
        {
            if (!NativeD3DHook.DS1Mod_IsImGuiReady()) return;

            nint ctx = NativeD3DHook.DS1Mod_GetImGuiContext();
            if (ctx == nint.Zero) return;

            ImGui.SetCurrentContext(ctx);
            _contextSynced = true;
        }

        foreach (var mod in _mods)
        {
            try { mod.OnGui(); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DS1Mod.Rendering] OnGui exception in {mod.GetType().Name}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        NativeD3DHook.DS1Mod_SetOnGuiCallback(nint.Zero);
    }
}
