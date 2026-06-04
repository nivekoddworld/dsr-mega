namespace DS1Mod.Core;

public interface IPatchContext
{
    /// <summary>Path to the UXM-extracted DSR game directory.</summary>
    string GameDir { get; }

    /// <summary>Path to the mods folder (<see cref="GameDir"/>/mods).</summary>
    string ModsDir { get; }

    /// <summary>
    /// Copies <paramref name="filePath"/> to <c>&lt;filePath&gt;.bak</c>
    /// if the backup does not already exist. Safe to call multiple times.
    /// </summary>
    void BackupFile(string filePath);

    /// <summary>
    /// Writes a line to the host's patch log (stdout in debug builds).
    /// </summary>
    void Log(string message);
}
