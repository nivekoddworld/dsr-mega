namespace DS1Mod.Core;

public interface IGameHooks
{
    event Action<BossKill> BossKilled;
    event Action<FogGate>  FogGateEntered;
    event Action           PlayerDied;
    event Action<int>      PlayerLeveledUp;   // arg = new soul level
    event Action<int>      ItemUsed;          // arg = goodsId

    /// <summary>
    /// Fires when any loaded non-player character loses HP. Discovery is lazy
    /// (RTTI walk + periodic background heap scan) and only runs while at
    /// least one handler is subscribed.
    /// </summary>
    event Action<EnemyDamage> EnemyDamaged;

    /// <summary>
    /// Register an item to watch. The <paramref name="triggerFlagId"/> must be
    /// wired in your patcher via <c>GamePatch.DefineItemTrigger</c>.
    /// </summary>
    void RegisterItemUsed(int goodsId, int triggerFlagId);
}
