using SoulsFormats;
using System.Text.RegularExpressions;

namespace DS1MegaRando.Core;

/// <summary>
/// Merges enemy-specific visual-effect entries from all map SFX bundles into
/// FRPG_SfxBnd_CommonEffects.ffxbnd.dcx, which is loaded in every area.
///
/// In DSR, enemy particle effects (hit sparks, magic glows, smoke trails, etc.)
/// are stored inside per-map SFX bundles.  When the enemy randomizer places a
/// boss in a foreign map the effects aren't present in that map's bundle, so
/// the boss appears without any VFX.  Copying every enemy-range effect entry
/// (IDs 10001–20000) into the CommonEffects bundle makes them globally available.
///
/// This mirrors what the Dark Souls Enemy Randomizer does in ffx_data.py
/// (AddEverythingToCommon, useDCX=True branch).
/// </summary>
public static class BossSfxMerger
{
    // Map SFX bundles to scan (matches the list used by the reference randomizer)
    private static readonly string[] MapBundleNames =
    [
        "m10", "m10_00", "m10_01", "m10_02",
        "m11", "m11_00",
        "m12", "m12_00", "m12_01",
        "m13", "m13_00", "m13_01", "m13_02",
        "m14", "m14_00", "m14_01",
        "m15", "m15_00", "m15_01",
        "m16", "m16_00",
        "m17", "m17_00",
        "m18", "m18_00", "m18_01",
    ];

    // Enemy effect files have IDs 10001–20000 encoded in their internal path names:
    //   Effect data:    …f0010001.ffx  …  f0020000.ffx
    //   Effect models:  …s10000.flver  …  s20000.flver
    //   Effect textures:…s10000.tpf    …  s20000.tpf
    private static readonly Regex _ffxRe   = new(@"f00([1-9]\d{4})\.ffx$",   RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _flverRe = new(@"s([1-9]\d{4})\.flver$",   RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _tpfRe   = new(@"s([1-9]\d{4})\.tpf$",    RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void MergeEnemyEffectsIntoCommon(string gameDir, string outDir)
    {
        string sfxDir = Path.Combine(gameDir, "sfx");
        if (!Directory.Exists(sfxDir))
        {
            Log("sfx directory not found — skipping VFX merge.");
            return;
        }

        const string commonName = "FRPG_SfxBnd_CommonEffects.ffxbnd.dcx";
        string commonSrc = Path.Combine(sfxDir, commonName);
        if (!File.Exists(commonSrc))
        {
            Log("CommonEffects bundle not found — skipping VFX merge.");
            return;
        }

        // ── 1. Collect all enemy-effect entries from every map bundle ──────────
        // Key = internal name (full path string), Value = the BinderFile to copy.
        var toAdd = new Dictionary<string, BinderFile>(StringComparer.OrdinalIgnoreCase);

        foreach (string bundleName in MapBundleNames)
        {
            string path = Path.Combine(sfxDir, $"FRPG_SfxBnd_{bundleName}.ffxbnd.dcx");
            if (!File.Exists(path)) continue;

            BND3 bnd;
            try { bnd = BND3.Read(path); }
            catch { continue; }

            foreach (var entry in bnd.Files)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                if (!IsEnemyEffect(entry.Name)) continue;
                toAdd.TryAdd(entry.Name, entry);
            }
        }

        if (toAdd.Count == 0) return;
        Log($"Collected {toAdd.Count} enemy-effect entries from map SFX bundles.");

        // ── 2. Open CommonEffects (prefer already-written outDir copy) ─────────
        string outSfxDir = Path.Combine(outDir, "sfx");
        string destPath  = Path.Combine(outSfxDir, commonName);
        string readPath  = File.Exists(destPath) ? destPath : commonSrc;

        BND3 common;
        try { common = BND3.Read(readPath); }
        catch (Exception ex)
        {
            Log($"Failed to read CommonEffects bundle: {ex.Message}");
            return;
        }

        // ── 3. Add missing entries, maintaining the ID range scheme ───────────
        // IDs < 100000 = effect (.ffx) entries
        // IDs 100000–199999 = texture (.tpf) entries
        // IDs ≥ 200000 = model (.flver) entries
        int nextFfx  = common.Files.Where(f => f.ID < 100000)
                                   .Select(f => f.ID).DefaultIfEmpty(0).Max() + 1;
        int nextTpf  = common.Files.Where(f => f.ID >= 100000 && f.ID < 200000)
                                   .Select(f => f.ID).DefaultIfEmpty(99999).Max() + 1;
        int nextMdl  = common.Files.Where(f => f.ID >= 200000)
                                   .Select(f => f.ID).DefaultIfEmpty(199999).Max() + 1;

        var existing = common.Files
            .Select(f => f.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int added = 0;
        foreach (var (name, src) in toAdd)
        {
            if (existing.Contains(name)) continue;

            int newId = src.ID < 100000 ? nextFfx++
                      : src.ID < 200000 ? nextTpf++
                      :                   nextMdl++;

            common.Files.Add(new BinderFile(src.Flags, newId, name, src.Bytes));
            existing.Add(name);
            added++;
        }

        if (added == 0) return;
        Log($"Added {added} new enemy-effect entries to CommonEffects.");

        // ── 4. Write out ───────────────────────────────────────────────────────
        Directory.CreateDirectory(outSfxDir);
        try
        {
            common.Write(destPath);
            Log($"CommonEffects bundle written to {destPath}");
        }
        catch (Exception ex)
        {
            Log($"Failed to write CommonEffects bundle: {ex.Message}");
        }
    }

    private static bool IsEnemyEffect(string name)
    {
        Match m;

        m = _ffxRe.Match(name);
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

    private static void Log(string msg) =>
        System.Diagnostics.Debug.WriteLine($"[BossSfxMerger] {msg}");
}
