using DS1Mod.Modding;
using SoulsFormats;
using System.Text.RegularExpressions;

namespace DS1Mod.EnemyRandomizer.Core;

/// <summary>
/// Merges enemy-specific effect bundles (sfx\FRPG_SfxBnd_m*.ffxbnd.dcx)
/// into the globally-loaded CommonEffects bundle so replaced bosses have their VFX.
/// </summary>
public static class BossSfxMerger
{
    private static readonly Regex _ffxRe = new(@"f00([1-9]\d{4})\.ffx$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _flverRe = new(@"s([1-9]\d{4})\.flver$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _tpfRe = new(@"s([1-9]\d{4})\.tpf$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsEnemyEffect(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var m = _ffxRe.Match(name);
        if (m.Success && int.TryParse(m.Groups[1].Value, out int id) && id <= 20000)
            return true;

        m = _flverRe.Match(name);
        if (m.Success && int.TryParse(m.Groups[1].Value, out id) && id <= 20000)
            return true;

        m = _tpfRe.Match(name);
        if (m.Success && int.TryParse(m.Groups[1].Value, out id) && id <= 20000)
            return true;

        return false;
    }
    /// <summary>
    /// Merge ALL enemy effects from all map bundles into CommonEffects.
    /// This ensures any enemy can be placed anywhere without missing VFX.
    /// Matches the old randomizer's approach.
    /// </summary>
    public static void MergeAllEnemyEffects(string gameDir, Action<string>? log = null)
    {
        try
        {
            var sfxDir = Path.Combine(gameDir, "sfx");
            if (!Directory.Exists(sfxDir))
            {
                log?.Invoke("[BossSfxMerger] SFX directory not found, skipping VFX merge");
                return;
            }

            var commonEffectsPath = Path.Combine(sfxDir, "FRPG_SfxBnd_CommonEffects.ffxbnd.dcx");
            if (!File.Exists(commonEffectsPath))
            {
                log?.Invoke("[BossSfxMerger] CommonEffects bundle not found, skipping VFX merge");
                return;
            }

            // Collect all enemy effects from all map bundles
            var toAdd = new Dictionary<string, BinderFile>(StringComparer.OrdinalIgnoreCase);
            int mapBundlesProcessed = 0;
            int effectsCollected = 0;

            foreach (var mapSfxFile in Directory.GetFiles(sfxDir, "FRPG_SfxBnd_m*.ffxbnd.dcx"))
            {
                try
                {
                    byte[] data = DCX.Decompress(mapSfxFile, out DCX.Type dcxType);
                    BND3 bnd = BND3.Read(data);
                    mapBundlesProcessed++;

                    foreach (var file in bnd.Files)
                    {
                        if (IsEnemyEffect(file.Name))
                        {
                            if (toAdd.TryAdd(file.Name, file))
                                effectsCollected++;
                        }
                    }
                }
                catch { continue; }
            }

            if (toAdd.Count == 0)
            {
                log?.Invoke("[BossSfxMerger] No enemy effects found to merge");
                return;
            }

            log?.Invoke($"[BossSfxMerger] Collected {effectsCollected} enemy effects from {mapBundlesProcessed} map bundles");

            // Merge into CommonEffects
            byte[] commonData = DCX.Decompress(commonEffectsPath, out DCX.Type commonDcxType);
            BND3 common = BND3.Read(commonData);

            int added = 0;
            foreach (var (name, file) in toAdd)
            {
                if (!common.Files.Any(f => f.Name == name))
                {
                    common.Files.Add(file);
                    added++;
                }
            }

            if (added > 0)
            {
                log?.Invoke($"[BossSfxMerger] Added {added} new effects to CommonEffects");
                byte[] newData = common.Write();
                DCX.Compress(newData, commonDcxType, commonEffectsPath);
                log?.Invoke("[BossSfxMerger] VFX merge complete");
            }
            else
            {
                log?.Invoke("[BossSfxMerger] All effects already in CommonEffects");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"[BossSfxMerger] Error during VFX merge: {ex.Message}");
        }
    }

    private readonly static Action<string>? _log;

    public static void MergeEnemyEffects(string gameDir, List<EnemyPlacement> bossPlacements, Action<string>? log = null)
    {
        try
        {
            // Find all map-specific SFX bundles that have enemy effects
            var sfxDir = Path.Combine(gameDir, "sfx");
            if (!Directory.Exists(sfxDir))
            {
                log?.Invoke("[BossSfxMerger] SFX directory not found, skipping VFX merge");
                return;
            }

            var commonEffectsPath = Path.Combine(sfxDir, "FRPG_SfxBnd_CommonEffects.ffxbnd.dcx");
            if (!File.Exists(commonEffectsPath))
            {
                log?.Invoke("[BossSfxMerger] CommonEffects bundle not found, skipping VFX merge");
                return;
            }

            // Collect all SFX IDs from boss placements
            var requiredSfxIds = new HashSet<int>();
            var bossesByMap = bossPlacements.GroupBy(p => p.MapId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var mapGroup in bossesByMap)
            {
                string mapId = mapGroup.Key;
                var mapSfxPath = Path.Combine(sfxDir, $"FRPG_SfxBnd_{mapId}.ffxbnd.dcx");

                if (!File.Exists(mapSfxPath)) continue;

                try
                {
                    byte[] mapSfxData = DCX.Decompress(mapSfxPath, out DCX.Type dcxType);
                    BND3 mapSfx = BND3.Read(mapSfxData);

                    // Collect all FFX IDs from this map's bundle
                    foreach (var file in mapSfx.Files)
                    {
                        if (file.Name.EndsWith(".ffx", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract ID from filename (format: ####.ffx)
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                            if (int.TryParse(nameWithoutExt, out int ffxId))
                            {
                                requiredSfxIds.Add(ffxId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[BossSfxMerger] Error reading {mapId} SFX: {ex.Message}");
                }
            }

            if (requiredSfxIds.Count == 0)
            {
                log?.Invoke("[BossSfxMerger] No enemy SFX IDs found to merge");
                return;
            }

            // Merge effects into CommonEffects
            MergeEffectsIntoCommon(commonEffectsPath, gameDir, sfxDir, requiredSfxIds, log);
        }
        catch (Exception ex)
        {
            log?.Invoke($"[BossSfxMerger] Fatal error: {ex.Message}");
        }
    }

    private static void MergeEffectsIntoCommon(
        string commonPath,
        string gameDir,
        string sfxDir,
        HashSet<int> sfxIds,
        Action<string>? log)
    {
        try
        {
            byte[] commonData = DCX.Decompress(commonPath, out DCX.Type dcxType);
            BND3 common = BND3.Read(commonData);

            var existingIds = new HashSet<int>();
            foreach (var file in common.Files)
            {
                if (file.Name.EndsWith(".ffx", StringComparison.OrdinalIgnoreCase))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                    if (int.TryParse(nameWithoutExt, out int ffxId))
                        existingIds.Add(ffxId);
                }
            }

            int mergedCount = 0;

            // Find and copy missing SFX from map bundles
            foreach (var mapFile in Directory.GetFiles(sfxDir, "FRPG_SfxBnd_m*.ffxbnd.dcx"))
            {
                try
                {
                    byte[] mapData = DCX.Decompress(mapFile, out _);
                    BND3 mapSfx = BND3.Read(mapData);

                    foreach (var file in mapSfx.Files)
                    {
                        if (file.Name.EndsWith(".ffx", StringComparison.OrdinalIgnoreCase))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                            if (int.TryParse(nameWithoutExt, out int ffxId) && sfxIds.Contains(ffxId))
                            {
                                if (!existingIds.Contains(ffxId))
                                {
                                    common.Files.Add(new BinderFile
                                    {
                                        Name = file.Name,
                                        ID = file.ID,
                                        Bytes = file.Bytes
                                    });
                                    mergedCount++;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Skip broken map bundles
                }
            }

            if (mergedCount > 0)
            {
                // Recompress and write back
                byte[] recompressed = DCX.Compress(common.Write(), dcxType);
                File.WriteAllBytes(commonPath, recompressed);
                log?.Invoke($"[BossSfxMerger] Merged {mergedCount} enemy effects into CommonEffects");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"[BossSfxMerger] Error merging into CommonEffects: {ex.Message}");
        }
    }
}
