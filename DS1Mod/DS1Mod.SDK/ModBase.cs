using DS1Mod.Core;

namespace DS1Mod.SDK;

/// <summary>
/// Base class for DS1 game mods. Inherit this instead of implementing
/// <see cref="IGameMode"/> directly — all lifecycle and event methods are
/// virtual no-ops so you only override what you need.
///
/// Example:
///   public class DeathCounter : ModBase {
///       public override string Name => "Death Counter";
///       private int _deaths;
///       public override void OnPlayerDied() => Console.WriteLine(++_deaths);
///   }
/// </summary>
public abstract class ModBase : IGameMode
{
    public abstract string Name { get; }

    protected GameSession? Session { get; private set; }

    public virtual void OnAttached(GameSession session) => Session = session;
    public virtual void OnDetached()                    => Session = null;
    public virtual void OnBossKilled(BossKill kill)     { }
    public virtual void OnFogGateOpened(FogGate gate)   { }
    public virtual void OnPlayerMoved(float x, float y, float z, string mapId) { }
}
