namespace DS1Mod.Core;

public sealed record PlayerStats(
    int   CurrentHp,
    int   MaxHp,
    float CurrentStamina,
    float MaxStamina)
{
    public float HpFraction      => MaxHp      > 0 ? (float)CurrentHp / MaxHp           : 0f;
    public float StaminaFraction => MaxStamina > 0 ? CurrentStamina   / MaxStamina       : 0f;
}

public sealed class PlayerStatsReader
{
    private readonly GameProcess _proc;

    internal PlayerStatsReader(GameProcess proc) => _proc = proc;

    public PlayerStats? Read()
    {
        nint wcm = _proc.Reader.Read<nint>(_proc.ModuleBase + (nint)Offsets.WorldChrManPtr);
        if (wcm == 0) return null;

        nint playerBase = _proc.Reader.Read<nint>(wcm + Offsets.WCM_PlayerOff);
        if (playerBase == 0) return null;

        int   hp    = _proc.Reader.Read<int>  (playerBase + Offsets.Player_HpOff);
        int   maxHp = _proc.Reader.Read<int>  (playerBase + Offsets.Player_MaxHpOff);
        float st    = _proc.Reader.Read<float>(playerBase + Offsets.Player_StaminaOff);
        float maxSt = _proc.Reader.Read<float>(playerBase + Offsets.Player_MaxStaminaOff);

        // Sanity-check: spurious reads during load screens can produce garbage values.
        if (maxHp <= 0 || maxHp > 99_999) return null;

        return new PlayerStats(
            Math.Clamp(hp,    0, maxHp),
            maxHp,
            Math.Clamp(st,    0f, maxSt),
            Math.Max(maxSt,   0f));
    }
}
