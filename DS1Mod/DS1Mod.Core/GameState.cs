using System.Text;
using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// Reports whether DSR is in a state where game memory is safe and meaningful
/// to poll. Returns <c>false</c> at the main menu, on loading screens, and
/// during warps / quit-outs — the windows in which the manager structures are
/// null or being torn down, and in which polling would either read garbage or
/// fault the process.
/// </summary>
public static class GameState
{
    /// <summary>
    /// True only when the world, the player character, and the event-flag
    /// manager are all live. Every read goes through <see cref="GameMemory"/>,
    /// which validates the page first, so this check can never itself fault.
    /// </summary>
    public static bool IsInGame()
    {
        // WorldChrMan is allocated once a save is loaded; null at the menu and
        // for most of a loading screen.
        nint wcm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.WorldChrManPtr);
        if (wcm == 0) return false;

        // The player ChrIns slot stays null until the map has finished
        // streaming in — this is what distinguishes "loaded" from "loading".
        nint player = GameMemory.Read<nint>(wcm + Offsets.WCM_PlayerOff);
        if (player == 0) return false;

        // EventFlagMan backs the boss / fog-gate hooks; gate on it too so those
        // polls never run before its flag array exists.
        nint efm = GameMemory.Read<nint>(GameMemory.ModuleBase + Offsets.EventFlagManPtr);
        if (efm == 0) return false;

        return true;
    }

    /// <summary>
    /// Human-readable dump of the manager pointer chain for diagnosing why
    /// IsInGame() is (or isn't) true. Shows the module base, each manager's
    /// static slot, whether that slot is mapped, and the pointer read from it.
    /// </summary>
    public static string Describe()
    {
        nint b = GameMemory.ModuleBase;
        var sb = new StringBuilder();
        sb.Append($"ModuleBase=0x{(ulong)b:X}");
        AppendSlot(sb, "WorldChrMan",  b + Offsets.WorldChrManPtr);
        AppendSlot(sb, "GameDataMan",  b + Offsets.GameDataManPtr);
        AppendSlot(sb, "EventFlagMan", b + Offsets.EventFlagManPtr);
        return sb.ToString();
    }

    private static void AppendSlot(StringBuilder sb, string label, nint slotAddr)
    {
        bool mapped = GameMemory.CanRead(slotAddr, nint.Size);
        nint val = GameMemory.Read<nint>(slotAddr);
        sb.Append($" | {label}@0x{(ulong)slotAddr:X} mapped={mapped} ->0x{(ulong)val:X}");
    }
}
