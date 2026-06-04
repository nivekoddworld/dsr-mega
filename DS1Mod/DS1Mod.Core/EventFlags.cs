using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

public static class EventFlags
{
    public static bool Get(int flagId)
    {
        nint arrayBase = GetArrayBase();
        if (arrayBase == 0) return false;
        uint word = GameMemory.Read<uint>(arrayBase + (flagId >> 5) * 4);
        return (word & (1u << (31 - (flagId & 31)))) != 0;
    }

    public static void Set(int flagId, bool value)
    {
        nint arrayBase = GetArrayBase();
        if (arrayBase == 0) return;
        nint wordAddr = arrayBase + (flagId >> 5) * 4;
        uint word = GameMemory.Read<uint>(wordAddr);
        int  bit  = 31 - (flagId & 31);
        if (value) word |=  (1u << bit);
        else       word &= ~(1u << bit);
        GameMemory.Write(wordAddr, word);
    }

    private static nint GetArrayBase()
    {
        nint man = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.EventFlagManPtr);
        if (man == 0) return 0;
        return GameMemory.Read<nint>(man + 0x18);
    }
}
