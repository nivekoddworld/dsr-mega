using DS1MegaRando.IO;
using DS1MegaRando.Settings;
using DS1MegaRando.Data.Items;
using SoulsFormats;

namespace DS1MegaRando.Items;

/// <summary>
/// Collects and filters the pool of items to be randomized.
/// </summary>
public class ItemPool
{
    // ItemLotParam rows that should never be moved (quest progression, scripted events)
    private static readonly HashSet<int> NeverRandomize = new()
    {
        10010, // Estus Flask lot — always given at start via Oscar (vanilla path)
        // Lot 11000 (Lordvessel) is gated by RandomizeLordvessel — handled in Build() below
    };

    // DS1R Lord Soul / Bequeathed Lord Soul Shard goods IDs (Nito, Bed of Chaos,
    // Four Kings, Seath). Previously listed as 2005-2008, which are actually keys.
    private static readonly HashSet<int> LordSoulItemIds = new() { 2500, 2501, 2502, 2503 };
    private const int LordvesselLotId  = 11000;

    public List<PoolItem> Build(ItemSettings settings, GameData gameData)
    {
        var items = new List<PoolItem>();

        if (gameData.ItemLotParam?.AppliedParamdef == null) return items;

        foreach (var row in gameData.ItemLotParam.Rows)
        {
            int lotId = (int)row.ID;
            if (NeverRandomize.Contains(lotId)) continue;
            // Lordvessel lot: only randomize when setting is enabled
            if (lotId == LordvesselLotId && !settings.RandomizeLordvessel) continue;

            int itemId   = GetCell<int>(row, "lotItemId01");
            int category = GetCell<int>(row, "lotItemCategory01");
            if (itemId == 0) continue;

            int count = GetCell<int>(row, "lotItemNum01");
            bool isKey     = KeyItemList.IsKeyItem(itemId);
            bool isUseless = ItemIds.UselessItems.Contains(itemId);
            bool isBoss    = IsBossSoul(itemId);

            if (settings.ExcludeUselessItems && isUseless) continue;
            // Lord Souls: keep vanilla when setting is disabled
            if (!settings.RandomizeLordSouls && LordSoulItemIds.Contains(itemId)) continue;
            // ReplaceWithConsumable: remove boss souls from pool; their lots get regular random items
            if (settings.BossSoulHandling == BossSoulHandling.ReplaceWithConsumable && isBoss) continue;

            items.Add(new PoolItem
            {
                ItemId      = itemId,
                LotCategory = category,
                SourceLot   = lotId,
                IsKey       = isKey,
                IsBoss      = isBoss,
                Count       = count,
            });
        }

        return items;
    }

    private static T GetCell<T>(PARAM.Row row, string name)
    {
        try { return (T)Convert.ChangeType(row[name]?.Value ?? default(T)!, typeof(T)); }
        catch { return default!; }
    }

    private static bool IsBossSoul(int itemId) => itemId is
        ItemIds.SoulOfQuelaag or ItemIds.SoulOfSif or ItemIds.SoulOfGwyn or
        ItemIds.CoreOfIronGolem or ItemIds.SoulOfOrnstein or ItemIds.SoulOfMoonlightButterfly or
        ItemIds.SoulOfSmough or ItemIds.SoulOfPriscilla or ItemIds.SoulOfGwyndolin or
        ItemIds.SoulOfArtorias or ItemIds.SoulOfManus;
}

public class PoolItem
{
    public int  ItemId      { get; set; }
    public int  LotCategory { get; set; }
    public int  SourceLot   { get; set; }
    public bool IsKey       { get; set; }
    public bool IsBoss      { get; set; }
    public int  Count       { get; set; }
}
