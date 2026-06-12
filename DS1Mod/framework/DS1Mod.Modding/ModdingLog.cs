namespace DS1Mod.Modding;

/// <summary>
/// Verbosity switch for the modding framework's internal diagnostics.
/// Patch-phase plumbing (DCX open, BND load, param row writes) is noisy and
/// only useful when debugging the framework itself — it is gated behind
/// <see cref="Verbose"/>, default off. Mod-facing results (e.g. "ring
/// defined") still log unconditionally via IPatchContext.Log.
/// </summary>
public static class ModdingLog
{
    /// <summary>Set true to see per-file patch plumbing in the console.</summary>
    public static bool Verbose { get; set; }

    internal static void Debug(string message)
    {
        if (Verbose) Console.WriteLine(message);
    }
}
