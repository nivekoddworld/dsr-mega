using DS1MegaRando.Annotations;
using DS1MegaRando.FogGate;
using DS1MegaRando.Graph;
using DS1MegaRando.IO;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Items;

public class ItemRandomizer
{
    public event EventHandler<string>? Log;

    public ItemResult Randomize(
        ItemSettings settings,
        AnnotationData ann,
        GameData gameData,
        FogGateResult? fogResult,
        WorldGraph? graph,
        Random rng)
    {
        Emit("Building location pool...");
        var locationPool = new LocationPool().Build(settings, ann, fogResult);

        Emit("Building item pool...");
        var itemPool = new ItemPool().Build(settings, gameData);

        Emit("Placing key items...");
        var keyAssignments = new KeyItemPlacer().Place(
            settings, ann, locationPool, itemPool, rng, fogResult, graph);

        Emit("Placing remaining items...");
        var allAssignments = new ItemPlacement().PlaceAll(
            settings, locationPool, itemPool, keyAssignments, rng);

        Dictionary<int, int> shopItems  = new();
        Dictionary<int, int> shopPrices = new();
        if (settings.RandomizeShops)
        {
            Emit("Randomizing shops...");
            (shopItems, shopPrices) = new ShopRandomizer().Randomize(settings, gameData, allAssignments, rng);
        }

        List<StartingLoadout> startingLoadouts = new();
        if (settings.RandomizeStartingLoadout || settings.FashionSouls)
        {
            Emit("Randomizing starting loadout...");
            startingLoadouts = new StartingLoadoutRandomizer().RandomizePerClass(settings, rng);
        }


        var keyPlacements = BuildKeyItemSpoiler(keyAssignments, ann, locationPool);
        Emit($"Item randomization complete. {allAssignments.Count} lots assigned.");

        var giftLots = new Dictionary<int, (int, int, int)>();
        if (settings.RandomizeStartingGift)
        {
            Emit("Randomizing starting gifts...");
            var gr = new GiftLotRandomizer();
            gr.Log += (_, m) => Emit(m);
            giftLots = gr.Randomize(gameData.ItemLotParam, rng);
        }

        return new ItemResult
        {
            LotAssignments        = allAssignments,
            ShopAssignments       = shopItems,
            ShopPrices            = shopPrices,
            StartingLoadouts      = startingLoadouts,
            KeyItemPlacements     = keyPlacements,
            // Auto-enable stat adjustment whenever the loadout itself is randomized
            // so players can always wield their starting weapon.
            AdjustStatsForWeapons = settings.AdjustStatsForWeapons || settings.RandomizeStartingLoadout,
            GiftLotAssignments    = giftLots,
        };
    }

    private static List<(string, string, string)> BuildKeyItemSpoiler(
        Dictionary<int, (int ItemId, int Category)> keyAssignments,
        AnnotationData ann,
        List<ItemLocation> locations)
    {
        var result = new List<(string, string, string)>();
        var locByLot = locations.ToDictionary(l => l.LotId);

        foreach (var (lotId, entry) in keyAssignments)
        {
            int itemId = entry.ItemId;
            var ki = ann.KeyItems.FirstOrDefault(k =>
                k.ID != null && ParseKeyItemId(k.ID) == itemId);
            if (ki == null) continue;

            string area = locByLot.TryGetValue(lotId, out var loc) ? loc.Area : "unknown";
            result.Add((ki.Name, area, $"Lot {lotId}"));
        }

        return result;
    }

    private static int ParseKeyItemId(string id)
    {
        // Format: "category:itemId" e.g. "3:2510" or "2:138"
        var parts = id.Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], out int v) ? v : 0;
    }

    private void Emit(string msg) => Log?.Invoke(this, msg);
}
