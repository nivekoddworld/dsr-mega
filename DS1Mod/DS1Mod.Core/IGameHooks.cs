namespace DS1Mod.Core;

public interface IGameHooks
{
    event Action<BossKill> BossKilled;
    event Action<FogGate>  FogGateEntered;
    event Action           PlayerDied;
    event Action<int>      PlayerLeveledUp;   // arg = new soul level
}
