using DS1Mod.Core.Hooks;

namespace DS1Mod.Core;

public sealed class GameHooks : IGameHooks
{
    internal readonly BossHook     Boss     = new();
    internal readonly FogGateHook  FogGate  = new();
    internal readonly DeathHook    Death    = new();
    internal readonly LevelHook    Level    = new();
    internal readonly ItemUsedHook Items    = new();

    public event Action<BossKill>? BossKilled
    {
        add    => Boss.BossKilled        += value;
        remove => Boss.BossKilled        -= value;
    }

    public event Action<FogGate>? FogGateEntered
    {
        add    => FogGate.FogGateEntered += value;
        remove => FogGate.FogGateEntered -= value;
    }

    public event Action? PlayerDied
    {
        add    => Death.PlayerDied       += value;
        remove => Death.PlayerDied       -= value;
    }

    public event Action<int>? PlayerLeveledUp
    {
        add    => Level.PlayerLeveledUp  += value;
        remove => Level.PlayerLeveledUp  -= value;
    }

    /// <summary>
    /// Fires with the goods ID when a registered item is used.
    /// Register items via <see cref="RegisterItemUsed"/>.
    /// </summary>
    public event Action<int>? ItemUsed
    {
        add    => Items.ItemUsed += value;
        remove => Items.ItemUsed -= value;
    }

    /// <summary>
    /// Register an item to watch. <paramref name="triggerFlagId"/> must be the
    /// same flag passed to <c>GamePatch.DefineItemTrigger</c> in your patcher.
    /// </summary>
    public void RegisterItemUsed(int goodsId, int triggerFlagId)
        => Items.Register(goodsId, triggerFlagId);

    internal void PollAll()
    {
        Boss.Poll();
        FogGate.Poll();
        Death.Poll();
        Level.Poll();
        Items.Poll();
    }
}
