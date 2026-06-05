namespace DS1Mod.Core;

/// <summary>
/// Optional interface for mods that want to render an ImGui overlay.
/// Implement alongside <see cref="IGameMod"/> (or inherit <see cref="DS1Mod.SDK.ModBase"/>
/// and also implement this interface).
///
/// <c>OnGui()</c> is called every frame from the D3D11 Present hook, on the
/// render thread.  Use <c>ImGui.*</c> calls (from the ImGui.NET package) freely —
/// DS1Mod.Rendering ensures the shared ImGui context is current before the call.
///
/// <example>
/// <code>
/// public class MyMod : ModBase, IGuiMod
/// {
///     public void OnGui()
///     {
///         if (ImGui.Begin("My Overlay"))
///             ImGui.Text("Hello from DSR!");
///         ImGui.End();
///     }
/// }
/// </code>
/// </example>
/// </summary>
public interface IGuiMod
{
    /// <summary>Called every frame on the render thread. Issue ImGui draw calls here.</summary>
    void OnGui();
}
