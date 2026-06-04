using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

public sealed class GameReader : IGameReader
{
    public bool GetEventFlag(int flagId) => EventFlags.Get(flagId);

    public PlayerState? GetPlayerState()
    {
        nint wcm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.WorldChrManPtr);
        if (wcm == 0) return null;

        nint playerBase = GameMemory.Read<nint>(wcm + Offsets.WCM_PlayerOff);
        if (playerBase == 0) return null;

        nint posAddr = playerBase + Offsets.Player_PosOff;
        float x = GameMemory.Read<float>(posAddr);
        float y = GameMemory.Read<float>(posAddr + 4);
        float z = GameMemory.Read<float>(posAddr + 8);

        string mapId = ReadMapId();
        return new PlayerState(x, y, z, mapId);
    }

    public PlayerStats? GetPlayerStats()
    {
        nint wcm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.WorldChrManPtr);
        if (wcm == 0) return null;

        nint playerBase = GameMemory.Read<nint>(wcm + Offsets.WCM_PlayerOff);
        if (playerBase == 0) return null;

        int   hp    = GameMemory.Read<int>  (playerBase + Offsets.Player_HpOff);
        int   maxHp = GameMemory.Read<int>  (playerBase + Offsets.Player_MaxHpOff);
        float st    = GameMemory.Read<float>(playerBase + Offsets.Player_StaminaOff);
        float maxSt = GameMemory.Read<float>(playerBase + Offsets.Player_MaxStaminaOff);

        if (maxHp <= 0 || maxHp > 99_999) return null;

        return new PlayerStats(
            Math.Clamp(hp, 0, maxHp),
            maxHp,
            Math.Clamp(st, 0f, maxSt),
            Math.Max(maxSt, 0f));
    }

    public int GetSoulLevel()
    {
        nint gdm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.GameDataManPtr);
        if (gdm == 0) return 0;
        nint playerData = GameMemory.Read<nint>(gdm + Offsets.GDM_PlayerDataOff);
        if (playerData == 0) return 0;
        return GameMemory.Read<int>(playerData + Offsets.PGD_SoulLevelOff);
    }

    public int GetSouls()
    {
        nint gdm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.GameDataManPtr);
        if (gdm == 0) return 0;
        nint playerData = GameMemory.Read<nint>(gdm + Offsets.GDM_PlayerDataOff);
        if (playerData == 0) return 0;
        return GameMemory.Read<int>(playerData + Offsets.PGD_SoulsOff);
    }

    private static string ReadMapId()
    {
        nint gdm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.GameDataManPtr);
        if (gdm == 0) return "";
        nint playerData = GameMemory.Read<nint>(gdm + Offsets.GDM_PlayerDataOff);
        if (playerData == 0) return "";
        int areaId = GameMemory.Read<int>(playerData + Offsets.PGD_AreaOff);
        if (areaId == 0) return "";
        int major = areaId / 1_000_000;
        int minor = (areaId / 100_000) % 10;
        return $"m{major:D2}_{minor:D2}_00_00";
    }
}
