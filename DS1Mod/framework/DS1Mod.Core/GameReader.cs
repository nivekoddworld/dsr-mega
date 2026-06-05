using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

public sealed class GameReader : IGameReader
{
    public bool GetEventFlag(int flagId) => EventFlags.Get(flagId);

    public PlayerState? GetPlayerState()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return null;

        DsrVersion.Ensure();
        nint mapData = GameMemory.Read<nint>(chr + Offsets.Chr_MapDataOff + DsrVersion.ChrData1Boost1);
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

        DsrVersion.Ensure();
        int b2 = DsrVersion.ChrData1Boost2;
        int hp    = GameMemory.Read<int>(chr + Offsets.Chr_HpOff         + b2);
        int maxHp = GameMemory.Read<int>(chr + Offsets.Chr_MaxHpOff      + b2);
        // Stamina is stored as int32 (like HP), not float — reading it as float
        // yields denormals that print as 0 but keep a correct ratio.
        int st    = GameMemory.Read<int>(chr + Offsets.Chr_StaminaOff    + b2);
        int maxSt = GameMemory.Read<int>(chr + Offsets.Chr_MaxStaminaOff + b2);

        if (maxHp <= 0 || maxHp > 99_999) return null;

        return new PlayerStats(
            Math.Clamp(hp, 0, maxHp),
            maxHp,
            Math.Clamp(st, 0, Math.Max(maxSt, 0)),
            Math.Max(maxSt, 0));
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

    public int GetCurrentAnimation() => GamePointers.CurrentAnimation;
}
