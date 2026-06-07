namespace DS1Mod.Core;

public interface IPatchContext
{
    /// <summary>Path to the UXM-extracted DSR game directory.</summary>
    string GameDir { get; }

    /// <summary>Path to the mods folder (<see cref="GameDir"/>/mods).</summary>
    string ModsDir { get; }

    /// <summary>
    /// Shared bonfire ESD editor that all mods can modify during the patch phase.
    /// The framework accumulates all changes and writes the result to disk.
    /// Returns null if the bonfire ESD could not be loaded.
    /// Type: DS1Mod.Modding.EsdEditor (reference avoided due to circular dependency).
    /// </summary>
    object? GetBonfireEsd() => null;

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

    /// <summary>
    /// Request a contiguous block of IDs for this mod in the given ID space.
    /// Returns a guaranteed-unique start ID; subsequent IDs are Start+1, Start+2, etc.
    /// Allocation is persistent (same mod always gets same IDs across runs) to
    /// maintain save-game compatibility. Default implementation throws so existing
    /// custom implementations get early feedback.
    /// </summary>
    /// <param name="space">ID space name (e.g. "EquipParamGoods", "EventFlags_m18_01")</param>
    /// <param name="count">Number of consecutive IDs to allocate (≥1)</param>
    /// <returns>Start ID for this mod's allocation</returns>
    int AllocateIds(string space, int count)
        => throw new NotImplementedException(
            $"IPatchContext.AllocateIds not implemented. " +
            $"Upgrade to the host that supports ID allocation.");

    /// <summary>Request a single ID in the given space.</summary>
    int AllocateId(string space) => AllocateIds(space, 1);

    /// <summary>
    /// Allocates the next available bonfire menu slot index. Vanilla items occupy
    /// slots 0–4; custom items start at slot 5 and increment with each call.
    /// </summary>
    int AllocateBonfireSlot()
        => throw new NotImplementedException(
            $"IPatchContext.AllocateBonfireSlot not implemented. " +
            $"Upgrade to the host that supports bonfire menu editing.");
}
