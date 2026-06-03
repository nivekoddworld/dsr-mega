namespace DS1Mod.Core;

/// <summary>
/// Reads and writes DSR event flags (the boolean state machine that tracks
/// every in-game event: boss kills, item pickups, NPC deaths, fog gates, etc.)
///
/// Event flags are stored as bits in a contiguous array of 32-bit words.
/// Flag N lives at: flagArray[N >> 5] bit (31 - (N &amp; 31))
/// (big-endian bit order within each word — leftmost bit is the highest index)
/// </summary>
public sealed class EventFlags
{
    private readonly GameProcess _proc;

    internal EventFlags(GameProcess proc) => _proc = proc;

    public bool Get(int flagId)
    {
        nint arrayBase = GetFlagArrayBase();
        if (arrayBase == 0) return false;

        int  wordIndex = flagId >> 5;
        int  bitIndex  = 31 - (flagId & 31);
        nint wordAddr  = arrayBase + wordIndex * 4;
        uint word      = _proc.Reader.Read<uint>(wordAddr);
        return (word & (1u << bitIndex)) != 0;
    }

    public void Set(int flagId, bool value)
    {
        nint arrayBase = GetFlagArrayBase();
        if (arrayBase == 0) return;

        int  wordIndex = flagId >> 5;
        int  bitIndex  = 31 - (flagId & 31);
        nint wordAddr  = arrayBase + wordIndex * 4;
        uint word      = _proc.Reader.Read<uint>(wordAddr);

        if (value)
            word |=  (1u << bitIndex);
        else
            word &= ~(1u << bitIndex);

        _proc.Writer.Write(wordAddr, word);
    }

    private nint GetFlagArrayBase()
    {
        // Chain: [base + EventFlagManPtr] → EventFlagMan struct
        // EventFlagMan + 0x18 → pointer to the flag bit array
        nint manAddr = _proc.Reader.Read<nint>(_proc.ModuleBase + (nint)Offsets.EventFlagManPtr);
        if (manAddr == 0) return 0;
        return _proc.Reader.Read<nint>(manAddr + 0x18);
    }
}
