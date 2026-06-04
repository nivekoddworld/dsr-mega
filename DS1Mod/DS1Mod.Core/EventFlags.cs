using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// Reads/writes DSR event flags. The flag's location is not a flat bit array —
/// the 8-digit ID decomposes into group / area / section / number, each
/// contributing to a byte offset into the flag region, with the bit selected
/// MSB-first within its 32-bit word. Algorithm and tables from
/// JKAnderson/DSR-Gadget.
/// </summary>
public static class EventFlags
{
    public static bool Get(int flagId)
    {
        nint block = GamePointers.EventFlagBlock;
        if (block == 0) return false;
        if (!TryGetOffset(flagId, out int offset, out uint mask)) return false;
        return (GameMemory.Read<uint>(block + offset) & mask) != 0;
    }

    public static void Set(int flagId, bool value)
    {
        nint block = GamePointers.EventFlagBlock;
        if (block == 0) return;
        if (!TryGetOffset(flagId, out int offset, out uint mask)) return;

        nint addr = block + offset;
        uint word = GameMemory.Read<uint>(addr);
        word = value ? (word | mask) : (word & ~mask);
        GameMemory.Write(addr, word);
    }

    private static readonly Dictionary<char, int> Groups = new()
    {
        ['0'] = 0x00000, ['1'] = 0x00500, ['5'] = 0x05F00, ['6'] = 0x0B900, ['7'] = 0x11300,
    };

    private static readonly Dictionary<string, int> Areas = new()
    {
        ["000"] = 0,  ["100"] = 1,  ["101"] = 2,  ["102"] = 3,  ["110"] = 4,  ["120"] = 5,
        ["121"] = 6,  ["130"] = 7,  ["131"] = 8,  ["132"] = 9,  ["140"] = 10, ["141"] = 11,
        ["150"] = 12, ["151"] = 13, ["160"] = 14, ["170"] = 15, ["180"] = 16, ["181"] = 17,
    };

    private static bool TryGetOffset(int id, out int offset, out uint mask)
    {
        offset = 0;
        mask   = 0;
        if (id < 0) return false;

        string s = id.ToString("D8");
        if (s.Length != 8) return false;   // IDs are at most 8 digits

        if (!Groups.TryGetValue(s[0], out int group)) return false;
        if (!Areas.TryGetValue(s.Substring(1, 3), out int area)) return false;

        int section = s[4] - '0';
        int number  = int.Parse(s.Substring(5, 3));

        offset = group + area * 0x500 + section * 128 + (number - number % 32) / 8;
        mask   = 0x80000000u >> (number % 32);
        return true;
    }
}
