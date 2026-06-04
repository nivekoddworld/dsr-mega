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
    /// region are all live. Every read goes through validated memory access,
    /// so this check can never itself fault.
    /// </summary>
    public static bool IsInGame()
    {
        // WorldChrMan + player ChrIns distinguish "loaded" from menu/loading;
        // the event-flag block must exist before the boss/fog polls run.
        return GamePointers.PlayerChr != 0 && GamePointers.EventFlagBlock != 0;
    }

    /// <summary>
    /// Human-readable dump of the resolved pointers for diagnosing why
    /// IsInGame() is (or isn't) true.
    /// </summary>
    public static string Describe() => GamePointers.Describe();
}
