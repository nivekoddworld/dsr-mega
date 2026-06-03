namespace DS1Mod.Core;

/// <summary>
/// DSR 1.03.1 (Steam) static pointer offsets from the module base address.
///
/// To verify / update these: open DarkSoulsRemastered.exe in Cheat Engine,
/// use the RemasterCETable (JohrnaJohrna/RemasterCETable on GitHub), and
/// follow the pointer chains for each manager.
///
/// All values are for the x64 build. Offsets are from module base unless
/// noted as relative to a dereferenced pointer in the chain description.
/// </summary>
internal static class Offsets
{
    // ── Event flag manager ────────────────────────────────────────────────
    // Chain: [base + EventFlagManPtr] → EventFlagMan*
    // EventFlagMan has an array of 4-byte flag words; flag N is at
    //   word[N >> 5] bit (N & 31)  (big-endian bit order within the word).
    public const long EventFlagManPtr = 0x1D2F10C0; // static ptr in .data

    // ── WorldChrMan (player + NPC character manager) ──────────────────────
    // Chain: [base + WorldChrManPtr] → WorldChrMan*
    //        WorldChrMan* + 0x80 → PlayerGameData*
    //        PlayerGameData* + 0x70 → position XYZ (3× float)
    public const long WorldChrManPtr  = 0x1D2FE018;
    public const int  WCM_PlayerOff   = 0x80;
    public const int  Player_PosOff   = 0x70;  // float x, y, z

    // ── GameDataMan (current area / map ID) ──────────────────────────────
    // Chain: [base + GameDataManPtr] → GameDataMan*
    //        GameDataMan* + 0xA4 → last bonfire area (int32)
    public const long GameDataManPtr  = 0x1D2F10C0; // shares region with EventFlagMan
    public const int  GDM_AreaOff     = 0xA4;

    // ── Note ──────────────────────────────────────────────────────────────
    // These offsets target DSR 1.03.1. If FromSoftware updates the exe the
    // static pointers will shift. Pattern-scan alternatives exist but the
    // static-offset approach is simpler and has been stable since 1.03.
}
