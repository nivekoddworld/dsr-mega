using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Event-flag helpers, encoding the rule that cost real debugging time: a map
/// only allocates *some* flag sections, and writing to an unallocated section
/// silently does nothing.
///
/// Map flag layout (8 digits): <c>1 AAA S NNN</c> = group / area / section /
/// number. e.g. <c>11815700</c> = area 181, section 5, number 700. m18_01 only
/// allocates section 0 (11810xxx) and section 5 (11815xxx).
/// </summary>
public static class Flags
{
    /// <summary>The section digit (the 5th of an 8-digit map flag), or -1 if not 8 digits.</summary>
    public static int Section(int flagId)
    {
        string s = flagId.ToString();
        return s.Length == 8 ? s[4] - '0' : -1;
    }

    /// <summary>The area part (digits 2-4) of an 8-digit map flag, or -1.</summary>
    public static int Area(int flagId)
    {
        string s = flagId.ToString();
        return s.Length == 8 ? int.Parse(s.Substring(1, 3)) : -1;
    }

    /// <summary>
    /// The set of (area, section) pairs a map's EMEVD actually references via
    /// Set/IF Event Flag — i.e. the flag sections you can safely write to. Use it
    /// to validate a flag id you intend to broadcast/watch.
    /// </summary>
    public static HashSet<(int area, int section)> AllocatedSections(EMEVD mapEmevd)
    {
        var set = new HashSet<(int, int)>();
        foreach (EMEVD.Event ev in mapEmevd.Events)
            foreach (EMEVD.Instruction ins in ev.Instructions)
            {
                // Set Event Flag (2003:2) and IF Event Flag (3:0) both carry a flag id.
                int flag = -1;
                if (ins.Bank == 2003 && ins.ID == 2) flag = TryInt(ins, 0, EMEVD.Instruction.ArgType.Int32, EMEVD.Instruction.ArgType.Byte);
                else if (ins.Bank == 3 && ins.ID == 0) flag = TryInt(ins, 3,
                    EMEVD.Instruction.ArgType.SByte, EMEVD.Instruction.ArgType.Byte, EMEVD.Instruction.ArgType.Byte, EMEVD.Instruction.ArgType.Int32);
                if (flag <= 0) continue;
                int a = Area(flag), s = Section(flag);
                if (a >= 0 && s >= 0) set.Add((a, s));
            }
        return set;
    }

    /// <summary>True if <paramref name="flagId"/>'s (area, section) is one the map uses.</summary>
    public static bool IsSectionAllocated(EMEVD mapEmevd, int flagId) =>
        AllocatedSections(mapEmevd).Contains((Area(flagId), Section(flagId)));

    private static int TryInt(EMEVD.Instruction i, int idx, params EMEVD.Instruction.ArgType[] layout)
    {
        try { return Convert.ToInt32(i.UnpackArgs(layout)[idx]); } catch { return -1; }
    }
}
