using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Enemies;
using DS1MegaRando.Core.FogGate;
using DS1MegaRando.Core.Items;
using DS1MegaRando.Core.Settings;
using SoulsFormats;

namespace DS1MegaRando.Core.IO;

/// <summary>
/// Writes randomization results back to DSR game files using SoulsFormats.
/// </summary>
public class GameFileWriter
{
    private readonly GlobalSettings _settings;

    public GameFileWriter(GlobalSettings settings)
    {
        _settings = settings;
    }

    public void WriteAll(
        GameData gameData,
        FogGateResult? fogResult,
        ItemResult? itemResult,
        EnemyResult? enemyResult,
        AnnotationData? ann = null)
    {
        string outDir = string.IsNullOrEmpty(_settings.OutputDirectory)
            ? _settings.GameDirectory
            : _settings.OutputDirectory;

        Directory.CreateDirectory(outDir);

        if (itemResult != null)
            WriteItemParams(outDir, gameData, itemResult);

        if (fogResult != null || enemyResult != null)
            WriteMapFiles(outDir, gameData, fogResult, enemyResult, ann);
    }

    // ── Items ──────────────────────────────────────────────────────────────

    private void WriteItemParams(string outDir, GameData gameData, ItemResult itemResult)
    {
        if (gameData.ParamBnd == null) return;

        ApplyLotAssignments(gameData.ItemLotParam, itemResult);
        ApplyShopAssignments(gameData.ShopLineupParam, itemResult);
        ApplyStartingLoadout(gameData.CharaInitParam, itemResult);

        // Rewrite modified param bytes back into the BND entries
        RepackParamIntoBnd(gameData.ParamBnd, "ItemLotParam",    gameData.ItemLotParam);
        RepackParamIntoBnd(gameData.ParamBnd, "ShopLineupParam", gameData.ShopLineupParam);
        RepackParamIntoBnd(gameData.ParamBnd, "CharaInitParam",  gameData.CharaInitParam);

        string dest = Path.Combine(outDir, @"param\GameParam\GameParam.parambnd.dcx");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        gameData.ParamBnd.Write(dest, gameData.ParamBndCompression);
    }

    private static void ApplyLotAssignments(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;

        foreach (var (rowId, entry) in itemResult.LotAssignments)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;

            TrySetCell(row, "lotItemId01",       entry.ItemId);
            TrySetCell(row, "lotItemCategory01", entry.Category);
            // Zero out slots 2-8 so the lot is clean
            for (int i = 2; i <= 8; i++)
            {
                TrySetCell(row, $"lotItemId{i:D2}", 0);
                TrySetCell(row, $"lotItemCategory{i:D2}", 0);
            }
        }
    }

    private static void ApplyShopAssignments(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;

        foreach (var (rowId, newItemId) in itemResult.ShopAssignments)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;
            TrySetCell(row, "equipId", newItemId);
        }

        foreach (var (rowId, price) in itemResult.ShopPrices)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == rowId);
            if (row == null) continue;
            TrySetCell(row, "value", price);
        }
    }

    private static void ApplyStartingLoadout(PARAM? param, ItemResult itemResult)
    {
        if (param == null || param.AppliedParamdef == null) return;
        if (itemResult.StartingItems.Count == 0) return;

        // Rows 1000-1009 are the 10 player-selectable starting classes (Knight, Wanderer, etc.)
        for (int classIdx = 0; classIdx < 10; classIdx++)
        {
            var row = param.Rows.FirstOrDefault(r => r.ID == 1000 + classIdx);
            if (row == null) continue;

            // Replace right-hand weapon with the randomized starting weapon (if provided)
            if (classIdx < itemResult.StartingItems.Count)
                TrySetCell(row, "equip_Wep_Right", itemResult.StartingItems[classIdx]);
        }
    }

    // ── Maps ───────────────────────────────────────────────────────────────

    private void WriteMapFiles(
        string outDir,
        GameData gameData,
        FogGateResult? fogResult,
        EnemyResult? enemyResult,
        AnnotationData? ann)
    {
        // Apply fog gate changes to MSBs (adds trigger regions + player spawns)
        if (fogResult != null && ann != null)
        {
            string distEventDir = FogGateWriter.DefaultDistEventDir;
            new FogGateWriter(distEventDir).Write(outDir, gameData, fogResult, ann);
        }

        string msbOutDir = Path.Combine(outDir, "map", "MapStudio");
        Directory.CreateDirectory(msbOutDir);

        foreach (var (mapId, msb) in gameData.Maps)
        {
            if (enemyResult != null)
                ApplyEnemyChanges(msb, mapId, enemyResult);

            // Determine the output filename from the source path.
            // DSR uses .msb.dcx; UXM-unpacked installs use .msb.
            // Write back with the same extension so the game can load our changes.
            string sourcePath = gameData.MapSourcePaths.TryGetValue(mapId, out var sp) ? sp : "";
            string fileName = Path.GetFileName(sourcePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{mapId}.msb";

            // If output dir differs from game dir, keep the same filename.
            string destPath = Path.Combine(msbOutDir, fileName);
            msb.Write(destPath);
        }
    }

    private static void ApplyEnemyChanges(MSB1 msb, string mapId, EnemyResult enemyResult)
    {
        if (!enemyResult.Placements.TryGetValue(mapId, out var placements)) return;

        var placementByEntity = placements
            .GroupBy(p => p.EntityId)
            .ToDictionary(g => g.Key, g => g.First());

        // Collect model names already declared in this MSB
        var knownModels = msb.Models.Enemies.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var enemy in msb.Parts.Enemies)
        {
            if (!placementByEntity.TryGetValue(enemy.EntityID, out var placement)) continue;

            string newModel = placement.NewModelId;

            // Register the model in the MSB model list if not already present
            if (!knownModels.Contains(newModel))
            {
                msb.Models.Enemies.Add(new MSB1.Model.Enemy { Name = newModel });
                knownModels.Add(newModel);
            }

            enemy.ModelName  = newModel;
            enemy.NPCParamID = placement.NewNpcParam;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void RepackParamIntoBnd(BND3 bnd, string paramName, PARAM? param)
    {
        if (param == null) return;
        var entry = bnd.Files.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f.Name)
                .Equals(paramName, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return;
        entry.Bytes = param.Write();
    }

    private static void TrySetCell(PARAM.Row row, string name, object value)
    {
        var cell = row[name];
        if (cell != null) cell.Value = value;
    }
}
