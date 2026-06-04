using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

public sealed class GameReader : IGameReader
{
    public bool GetEventFlag(int flagId) => EventFlags.Get(flagId);

    public PlayerState? GetPlayerState()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return null;

        nint mapData = GameMemory.Read<nint>(chr + Offsets.Chr_MapDataOff);
        if (mapData == 0) return new PlayerState(0f, 0f, 0f, "");

        nint posData = GameMemory.Read<nint>(mapData + Offsets.ChrMap_PosDataOff);
        if (posData == 0) return new PlayerState(0f, 0f, 0f, "");

        float x = GameMemory.Read<float>(posData + Offsets.Pos_XOff);
        float y = GameMemory.Read<float>(posData + Offsets.Pos_YOff);
        float z = GameMemory.Read<float>(posData + Offsets.Pos_ZOff);
        return new PlayerState(x, y, z, "");
    }

    public PlayerStats? GetPlayerStats()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return null;

        int   hp    = GameMemory.Read<int>  (chr + Offsets.Chr_HpOff);
        int   maxHp = GameMemory.Read<int>  (chr + Offsets.Chr_MaxHpOff);
        float st    = GameMemory.Read<float>(chr + Offsets.Chr_StaminaOff);
        float maxSt = GameMemory.Read<float>(chr + Offsets.Chr_MaxStaminaOff);

        if (maxHp <= 0 || maxHp > 99_999) return null;

        return new PlayerStats(
            Math.Clamp(hp, 0, maxHp),
            maxHp,
            Math.Clamp(st, 0f, maxSt),
            Math.Max(maxSt, 0f));
    }

    public int GetSoulLevel()
    {
        nint pgd = GamePointers.PlayerGameData;
        return pgd == 0 ? 0 : GameMemory.Read<int>(pgd + Offsets.PGD_SoulLevelOff);
    }

    public int GetSouls()
    {
        nint pgd = GamePointers.PlayerGameData;
        return pgd == 0 ? 0 : GameMemory.Read<int>(pgd + Offsets.PGD_SoulsOff);
    }
}
