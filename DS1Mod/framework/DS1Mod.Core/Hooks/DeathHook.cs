using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

internal sealed class DeathHook
{
    private bool _wasAlive = false;

    public event Action? PlayerDied;

    public void Poll()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0)
        {
            _wasAlive = false;
            return;
        }

        DsrVersion.Ensure();
        int hp = GameMemory.Read<int>(chr + Offsets.Chr_HpOff + DsrVersion.ChrData1Boost2);

        if (hp > 0)
        {
            _wasAlive = true;
            return;
        }

        // HP has dropped to 0 — only count it as a death if we previously saw
        // the player alive, so we never fire on a half-initialized load frame.
        if (_wasAlive)
        {
            PlayerDied?.Invoke();
            _wasAlive = false;
        }
    }
}
