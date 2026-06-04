using DS1MegaRando.IO;
using DS1MegaRando.Settings;
using DS1MegaRando.Data.Items;
using SoulsFormats;

namespace DS1MegaRando.Items;

public class ShopRandomizer
{
    // Shop rows that should never be randomized (Feeding Frampt, covenant offerings, etc.)
    private static readonly HashSet<int> NeverRandomize = new() { 99000, 99001, 99002 };

    private static readonly IReadOnlyList<int> BossSoulIds = new[]
    {
        ItemIds.SoulOfQuelaag, ItemIds.SoulOfSif, ItemIds.SoulOfGwyn,
        ItemIds.CoreOfIronGolem, ItemIds.SoulOfOrnstein, ItemIds.SoulOfMoonlightButterfly,
        ItemIds.SoulOfSmough, ItemIds.SoulOfPriscilla, ItemIds.SoulOfGwyndolin,
        ItemIds.SoulOfArtorias, ItemIds.SoulOfManus,
    };

    public (Dictionary<int, int> Items, Dictionary<int, int> Prices) Randomize(
        ItemSettings settings,
        GameData gameData,
        Dictionary<int, (int ItemId, int Category)> itemAssignments,
        Random rng)
    {
        var itemChanges  = new Dictionary<int, int>();
        var priceChanges = new Dictionary<int, int>();

        if (gameData.ShopLineupParam?.AppliedParamdef == null)
            return (itemChanges, priceChanges);

        var shopRows = gameData.ShopLineupParam.Rows
            .Where(r => !NeverRandomize.Contains((int)r.ID))
            .ToList();

        // Collect all shop item IDs to shuffle
        var shopItemIds = shopRows
            .Select(r => GetCell<int>(r, "equipId"))
            .Where(id => id > 0)
            .ToList();

        // Inject key items into shop pool when enabled
        if (settings.AllowKeyItemsInShops)
            shopItemIds.AddRange(KeyItemList.All.Select(k => k.ItemId));

        // Inject boss souls into shop pool for AllowTranspose mode
        if (settings.BossSoulHandling == BossSoulHandling.AllowTranspose)
            shopItemIds.AddRange(BossSoulIds);

        Shuffle(shopItemIds, rng);

        for (int i = 0; i < shopRows.Count && i < shopItemIds.Count; i++)
        {
            int rowId   = (int)shopRows[i].ID;
            int itemId  = shopItemIds[i];
            int curPrice = GetCell<int>(shopRows[i], "value");

            itemChanges[rowId] = itemId;
            priceChanges[rowId] = settings.ShopPriceMode switch
            {
                ShopPriceMode.Free   => 0,
                ShopPriceMode.Random => rng.Next(settings.ShopPriceRandomMin, settings.ShopPriceRandomMax + 1),
                ShopPriceMode.Scaled => ScalePrice(curPrice, itemId),
                _                    => curPrice, // Vanilla
            };
        }

        return (itemChanges, priceChanges);
    }

    private static T GetCell<T>(PARAM.Row row, string name)
    {
        try { return (T)Convert.ChangeType(row[name]?.Value ?? default(T)!, typeof(T)); }
        catch { return default!; }
    }

    private static int ScalePrice(int basePrice, int itemId)
    {
        if (KeyItemList.IsKeyItem(itemId)) return Math.Max(basePrice, 5000);
        if (itemId >= 20000 && itemId < 30000) return Math.Max(basePrice, 8000); // Rings
        return basePrice;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
