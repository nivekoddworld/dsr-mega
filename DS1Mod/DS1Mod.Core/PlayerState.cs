namespace DS1Mod.Core;

public sealed record PlayerState(
    float X, float Y, float Z,
    string MapId);

public sealed class PlayerStateReader
{
    private readonly GameProcess _proc;
    private string _lastMapId = "";

    internal PlayerStateReader(GameProcess proc) => _proc = proc;

    public PlayerState? Read()
    {
        // Chain: [base + WorldChrManPtr] → WorldChrMan
        //        WorldChrMan + WCM_PlayerOff → player character base
        //        player base + Player_PosOff → float[3] x,y,z
        nint wcm = _proc.Reader.Read<nint>(_proc.ModuleBase + (nint)Offsets.WorldChrManPtr);
        if (wcm == 0) return null;

        nint playerBase = _proc.Reader.Read<nint>(wcm + Offsets.WCM_PlayerOff);
        if (playerBase == 0) return null;

        nint posAddr = playerBase + Offsets.Player_PosOff;
        float x = _proc.Reader.Read<float>(posAddr);
        float y = _proc.Reader.Read<float>(posAddr + 4);
        float z = _proc.Reader.Read<float>(posAddr + 8);

        // Map ID is derived from the last-used bonfire; we cache and fall back
        // to the previous value if the read returns zero (between warps).
        nint gdm = _proc.Reader.Read<nint>(_proc.ModuleBase + (nint)Offsets.GameDataManPtr);
        if (gdm != 0)
        {
            int areaId = _proc.Reader.Read<int>(gdm + Offsets.GDM_AreaOff);
            if (areaId != 0)
                _lastMapId = BonfireAreaToMapId(areaId);
        }

        return new PlayerState(x, y, z, _lastMapId);
    }

    private static string BonfireAreaToMapId(int areaId)
    {
        // Bonfire area IDs correspond to map IDs by their leading digits.
        // e.g. 101xxxx → m10_01_00_00, 121xxxx → m12_01_00_00
        int major = areaId / 1_000_000;
        int minor = (areaId / 100_000) % 10;
        return $"m{major:D2}_{minor:D2}_00_00";
    }
}
