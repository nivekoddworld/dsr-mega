namespace DS1Mod.Core;

/// <summary>
/// DSR 1.03.1 memory offsets. Verify against JohrnaJohrna/RemasterCETable
/// after any game update.
/// </summary>
internal static class Offsets
{
    // ── EventFlagMan ──────────────────────────────────────────────────────
    // [base + EventFlagManPtr] → EventFlagMan*
    // EventFlagMan + 0x18 → uint[] flag bit array
    // Flag N: word[N >> 5] bit (31 - (N & 31))  (big-endian within each word)
    public const long EventFlagManPtr = 0x1D2F10C0;

    // ── WorldChrMan / player character ────────────────────────────────────
    // [base + WorldChrManPtr] → WorldChrMan*
    // WorldChrMan + WCM_PlayerOff → ChrIns* (player)
    // ChrIns + Player_PosOff     → float x, y, z
    // ChrIns + Player_HpOff      → int32 current HP
    // ChrIns + Player_MaxHpOff   → int32 max HP
    // ChrIns + Player_StaminaOff → float current stamina
    public const long WorldChrManPtr      = 0x1D2FE018;
    public const int  WCM_PlayerOff       = 0x80;
    public const int  Player_PosOff       = 0x70;
    public const int  Player_HpOff        = 0xD8;
    public const int  Player_MaxHpOff     = 0xDC;
    public const int  Player_StaminaOff   = 0xF0;
    public const int  Player_MaxStaminaOff = 0xF4;

    // ── GameDataMan / player save data ────────────────────────────────────
    // [base + GameDataManPtr] → GameDataMan*
    // GameDataMan + GDM_PlayerDataOff → PlayerGameData*
    // PlayerGameData + PGD_SoulsOff      → int32 held souls
    // PlayerGameData + PGD_SoulLevelOff  → int32 soul level
    // PlayerGameData + PGD_AreaOff       → int32 last bonfire area ID
    //
    // ⚠ VERIFY these if level / souls reads return 0.
    public const long GameDataManPtr     = 0x1D2F7158;
    public const int  GDM_PlayerDataOff  = 0x08;
    public const int  PGD_SoulsOff       = 0x40;
    public const int  PGD_SoulLevelOff   = 0x68;
    public const int  PGD_AreaOff        = 0xA4;
}
