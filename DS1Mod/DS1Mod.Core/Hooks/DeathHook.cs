using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

internal sealed class DeathHook
{
    private bool _wasDead = false;

    public event Action? PlayerDied;

    public void Poll()
    {
        nint playerBase = GetPlayerBase();
        if (playerBase == 0)
        {
            _wasDead = false;
            return;
        }

        int hp = GameMemory.Read<int>(playerBase + Offsets.Player_HpOff);
        bool isDead = hp <= 0;

        if (isDead && !_wasDead)
            PlayerDied?.Invoke();

        _wasDead = isDead;
    }

    private static nint GetPlayerBase()
    {
        nint wcm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.WorldChrManPtr);
        if (wcm == 0) return 0;
        return GameMemory.Read<nint>(wcm + Offsets.WCM_PlayerOff);
    }
}
