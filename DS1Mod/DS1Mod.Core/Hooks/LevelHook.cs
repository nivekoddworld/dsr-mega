using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

internal sealed class LevelHook
{
    private int _lastLevel = -1;

    public event Action<int>? PlayerLeveledUp;

    public void Poll()
    {
        nint pgd = GamePointers.PlayerGameData;
        if (pgd == 0) return;

        int level = GameMemory.Read<int>(pgd + Offsets.PGD_SoulLevelOff);
        if (level <= 0) return;

        if (_lastLevel >= 0 && level > _lastLevel)
            PlayerLeveledUp?.Invoke(level);

        _lastLevel = level;
    }
}
