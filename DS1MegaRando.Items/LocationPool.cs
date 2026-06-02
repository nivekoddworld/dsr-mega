using DS1MegaRando.Annotations;
using DS1MegaRando.FogGate;
using DS1MegaRando.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Items;

public class ItemLocation
{
    public int              LotId       { get; set; }
    public string           Area        { get; set; } = "";
    public ItemDifficulty   Difficulty  { get; set; }
    public string           Description { get; set; } = "";
    public bool             IsShop      { get; set; }
    public bool             IsDLC       { get; set; }
    public bool             IsNPCDrop   { get; set; }
    public bool             IsKeySlot   { get; set; }
    public bool             IsStarting  { get; set; }
}

/// <summary>
/// Builds the pool of item locations from annotation data, filtered and weighted by settings.
/// Ported from locations_setup.py.
/// </summary>
public class LocationPool
{
    public List<ItemLocation> Build(
        ItemSettings settings,
        AnnotationData ann,
        FogGateResult? fogResult)
    {
        var locations = BuildBase(ann, fogResult);

        if (!settings.IncludeDLCItemLocations)
            locations.RemoveAll(l => l.IsDLC);

        return locations;
    }

    private List<ItemLocation> BuildBase(AnnotationData ann, FogGateResult? fogResult)
    {
        // Build from the LotLocations map in the annotation
        var locations = new List<ItemLocation>();

        foreach (var (lotId, areaName) in ann.LotLocations)
        {
            bool isDLC = areaName.StartsWith("dlc");
            var diff = InferDifficulty(areaName, fogResult);

            locations.Add(new ItemLocation
            {
                LotId       = lotId,
                Area        = areaName,
                Difficulty  = diff,
                Description = $"Lot {lotId} in {areaName}",
                IsDLC       = isDLC,
            });
        }

        return locations;
    }

    private ItemDifficulty InferDifficulty(string area, FogGateResult? fogResult)
    {
        if (fogResult == null || !fogResult.AreaRatios.TryGetValue(area, out var ratio))
        {
            // Fallback to a static classification based on vanilla area ordering
            return area switch
            {
                "firelink" or "burg_start" or "burg_upper" or "parish_church" or "depths" => ItemDifficulty.Easy,
                "sens" or "anorlondo_main" or "anorlondo_building" => ItemDifficulty.Medium,
                "newlondo_lower" or "totg_lower" or "dukes_prison" => ItemDifficulty.Hard,
                _ => ItemDifficulty.Medium,
            };
        }

        float healthRatio = ratio.Health;
        return healthRatio switch
        {
            < 1.3f => ItemDifficulty.Easy,
            < 2.0f => ItemDifficulty.Medium,
            < 2.8f => ItemDifficulty.Hard,
            _      => ItemDifficulty.VeryHard,
        };
    }
}
