namespace DS1Mod.Core;

/// <summary>
/// Optional interface for mods that need to modify game files on disk before
/// any map is loaded.
///
/// Implement this alongside <see cref="IGameMod"/>:
///   public class MyMod : ModBase, IGamePatcher { ... }
///
/// <see cref="Patch"/> is called once at startup, before <see cref="IGameMod.OnLoad"/>,
/// while the game is still on the title screen.  DSR loads map files lazily
/// (only when the player enters an area), so patched files will always be
/// used for the current session.
///
/// Typical uses: injecting SetEventFlag instructions into EMEVD scripts,
/// editing params, swapping models, etc.
/// </summary>
public interface IGamePatcher
{
    void Patch(IPatchContext ctx);
}
