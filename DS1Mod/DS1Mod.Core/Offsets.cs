namespace DS1Mod.Core;

/// <summary>
/// DSR memory layout. Pointer bases are found at runtime by AOB scan (see
/// <see cref="GamePointers"/>) rather than hardcoded RVAs, so they survive the
/// image layout/version drift that broke the old static offsets.
///
/// Patterns and chain offsets are from JKAnderson/DSR-Gadget (DSROffsets.cs).
/// Each AOB matches a `mov reg, [rip+disp32]`: 3-byte opcode, 4-byte
/// displacement at +3, instruction length 7 → the referenced static address is
/// `hit + 7 + int32@(hit+3)`, and that address holds the manager pointer.
/// </summary>
internal static class Offsets
{
    // ── AOB patterns ('?' = wildcard byte) ────────────────────────────────
    public const string WorldChrBaseAOB =
        "48 8B 05 ? ? ? ? 48 8B 48 68 48 85 C9 0F 84 ? ? ? ? 48 39 5E 10 0F 84 ? ? ? ? 48";
    public const string ChrClassBaseAOB =
        "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
    public const string EventFlagsAOB =
        "48 8B 0D ? ? ? ? 99 33 C2 45 33 C0 2B C2 8D 50 F6";

    public const int RipDispAt   = 3;   // displacement starts here
    public const int RipInstrLen = 7;   // length of the mov instruction

    // ── WorldChrMan → player character (ChrData1) ─────────────────────────
    // WorldChrMan = *(worldChrTarget); player = *(WorldChrMan + 0x68)
    public const int WCM_PlayerChrOff   = 0x68;
    public const int Chr_HpOff          = 0x3D8;   // int32
    public const int Chr_MaxHpOff       = 0x3DC;   // int32
    public const int Chr_StaminaOff     = 0x3E8;   // float
    public const int Chr_MaxStaminaOff  = 0x3EC;   // float
    public const int Chr_MapDataOff     = 0x48;    // ChrData1 → ChrMapData

    // ── ChrMapData → ChrPosData → world position ──────────────────────────
    public const int ChrMap_PosDataOff  = 0x28;
    public const int Pos_XOff           = 0x10;    // float
    public const int Pos_YOff           = 0x14;    // float
    public const int Pos_ZOff           = 0x18;    // float

    // ── GameDataMan → PlayerGameData (ChrData2) ───────────────────────────
    // GameDataMan = *(chrClassTarget); PlayerGameData = *(GameDataMan + 0x10)
    public const int GDM_PlayerDataOff  = 0x10;
    public const int PGD_SoulLevelOff   = 0x90;    // int32
    public const int PGD_SoulsOff       = 0x94;    // int32
}
