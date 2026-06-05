using DS1MegaRando.Data.Enemies;
using SoulsFormats;

namespace DS1MegaRando.Enemies;

/// <summary>
/// Patches EMEVD event scripts for boss slots that received a new model.
/// Each boss may have intro events that call ForceAnimationPlayback or
/// WarpCharacter with hardcoded animation IDs from the original model.
/// Stripping those instructions makes any replacement model spawn cleanly
/// at its MSB position using its own AI defaults.
/// </summary>
public static class BossEmevdPatcher
{
    /// <summary>
    /// Applies EMEVD patches for all bosses in <paramref name="placements"/>
    /// whose model changed from the vanilla model.
    /// </summary>
    public static void PatchAll(
        IEnumerable<EnemyPlacement> placements,
        string gameDir,
        string outDir)
    {
        // Build a map of entityId → placement for quick lookup.
        // Use GroupBy().First() so a duplicate entity ID (same boss in two map
        // files) doesn't throw — the first placement wins.
        var byEntity = placements
            .GroupBy(p => p.EntityId)
            .ToDictionary(g => g.Key, g => g.First());

        // Group patched bosses by their map
        var bossesToPatch = BossIds.All
            .Where(b => b.EmevdPatches != null && b.EmevdPatches.Count > 0)
            .Where(b => byEntity.TryGetValue(b.EntityId, out var p) &&
                        !string.Equals(p.OldModelId, p.NewModelId, StringComparison.OrdinalIgnoreCase))
            .GroupBy(b => b.MapId);

        foreach (var group in bossesToPatch)
        {
            string mapId = group.Key;
            PatchMapEmevd(mapId, group, gameDir, outDir);
        }
    }

    private static void PatchMapEmevd(
        string mapId,
        IEnumerable<BossDef> bosses,
        string gameDir,
        string outDir)
    {
        string fileName = $"{mapId}.emevd.dcx";

        // Prefer already-written outDir version (fog gate writer may have touched it)
        string outPath = Path.Combine(outDir, "event", fileName);
        string srcPath = File.Exists(outPath)
            ? outPath
            : Path.Combine(gameDir, "event", fileName);

        if (!File.Exists(srcPath)) return;

        EMEVD evd;
        try { evd = EMEVD.Read(srcPath); }
        catch { return; }

        bool changed = false;

        foreach (var boss in bosses)
        {
            if (boss.EmevdPatches == null) continue;

            foreach (var patch in boss.EmevdPatches)
            {
                var evt = evd.Events.FirstOrDefault(e => e.ID == patch.EventId);
                if (evt == null) continue;

                int before = evt.Instructions.Count;
                evt.Instructions.RemoveAll(instr =>
                    patch.Remove.Any(r => instr.Bank == r.Bank && instr.ID == r.InstrId));
                int after = evt.Instructions.Count;

                if (after < before) changed = true;
            }
        }

        if (!changed) return;

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        try { evd.Write(outPath); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"BossEmevdPatcher: failed to write {outPath}: {ex.Message}");
        }
    }
}
