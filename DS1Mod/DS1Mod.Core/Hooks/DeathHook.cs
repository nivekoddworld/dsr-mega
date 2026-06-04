using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

internal sealed class DeathHook
{
    private bool _wasDead = false;

    public event Action? PlayerDied;

    public void Poll()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0)
        {
            _wasDead = false;
            return;
        }

        int hp = GameMemory.Read<int>(chr + Offsets.Chr_HpOff);
        bool isDead = hp <= 0;

        if (isDead && !_wasDead)
            PlayerDied?.Invoke();

        _wasDead = isDead;
    }
}
