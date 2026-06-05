using DS1Mod.Core;

namespace DS1Mod.SDK;

/// <summary>
/// Base class for DS1 mods. Inherit this instead of implementing
/// <see cref="IGameMod"/> directly — all lifecycle methods are virtual
/// no-ops so you only override what you need.
///
/// To patch game files before any map loads, also implement
/// <see cref="IGamePatcher"/> and override <see cref="Patch"/>:
///
///   public class MyMod : ModBase, IGamePatcher {
///       public override string Name    => "My Mod";
///       public override string Version => "1.0.0";
///       public override string Author  => "YourName";
///
///       public void Patch(IPatchContext ctx) {
///           // modify files in ctx.GameDir here
///       }
///
///       public override void OnLoad(IModContext ctx) {
///           ctx.Hooks.BossKilled += kill => Console.WriteLine($"Killed {kill.BossName}!");
///       }
///   }
/// </summary>
public abstract class ModBase : IGameMod
{
    public abstract string Name    { get; }
    public abstract string Version { get; }
    public abstract string Author  { get; }

    public virtual void OnLoad  (IModContext ctx) { }
    public virtual void OnUnload()                { }
    public virtual void OnTick  ()                { }
}
