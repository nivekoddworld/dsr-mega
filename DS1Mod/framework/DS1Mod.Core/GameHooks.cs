using DS1Mod.Core.Hooks;

namespace DS1Mod.Core;

public sealed class GameHooks : IGameHooks
{
    internal readonly BossHook    Boss     = new();
    internal readonly FogGateHook FogGate  = new();
    internal readonly DeathHook   Death    = new();
    internal readonly LevelHook   Level    = new();

    public event Action<BossKill>? BossKilled
    {
        add    => Boss.BossKilled    += value;
        remove => Boss.BossKilled    -= value;
    }

    public event Action<FogGate>? FogGateEntered
    {
        add    => FogGate.FogGateEntered += value;
        remove => FogGate.FogGateEntered -= value;
    }

    public event Action? PlayerDied
    {
        add    => Death.PlayerDied += value;
        remove => Death.PlayerDied -= value;
    }

    public event Action<int>? PlayerLeveledUp
    {
        add    => Level.PlayerLeveledUp += value;
        remove => Level.PlayerLeveledUp -= value;
    }

    internal void PollAll()
    {
        Boss.Poll();
        FogGate.Poll();
        Death.Poll();
        Level.Poll();
    }
}
