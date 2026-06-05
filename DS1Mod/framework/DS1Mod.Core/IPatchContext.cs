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

    /// <summary>
    /// Records that this patcher wrote to <paramref name="selector"/> inside
    /// <paramref name="filePath"/> (e.g. selector = "PARAM:EquipParamGoods:8000").
    /// The host uses these records to detect when two mods write the same target
    /// and logs a conflict warning. Default implementation is a no-op so existing
    /// custom <see cref="IPatchContext"/> implementations don't break.
    /// </summary>
    void RecordEdit(string filePath, string selector) { }
}
