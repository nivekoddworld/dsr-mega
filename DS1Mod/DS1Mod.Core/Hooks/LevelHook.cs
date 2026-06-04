using DS1Mod.Core.Memory;

namespace DS1Mod.Core.Hooks;

internal sealed class LevelHook
{
    private int _lastLevel = -1;

    public event Action<int>? PlayerLeveledUp;

    public void Poll()
    {
        int level = ReadSoulLevel();
        if (level <= 0) return;

        if (_lastLevel >= 0 && level > _lastLevel)
            PlayerLeveledUp?.Invoke(level);

        _lastLevel = level;
    }

    private static int ReadSoulLevel()
    {
        nint gdm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.GameDataManPtr);
        if (gdm == 0) return 0;
        nint playerData = GameMemory.Read<nint>(gdm + Offsets.GDM_PlayerDataOff);
        if (playerData == 0) return 0;
        return GameMemory.Read<int>(playerData + Offsets.PGD_SoulLevelOff);
    }
}
