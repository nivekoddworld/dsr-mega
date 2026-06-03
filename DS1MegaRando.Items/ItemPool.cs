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
        10010, // Estus Flask lot — always given at start via Oscar
        1082,  // Estus Flask lot used by the asylum corpse (ApplyStartingEstus / GuaranteeStartingEstus)
        // Lot 11000 (Lordvessel) is gated by RandomizeLordvessel — handled in Build() below
    };

    // Lord Soul item IDs in DS1R ItemLotParam (must match KeyItemList values)
    private static readonly HashSet<int> LordSoulItemIds = new() { 2005, 2006, 2007, 2008 };
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
        ItemIds.SoulOfSif or ItemIds.SoulOfGwyn or ItemIds.CoreOfIronGolem or
        ItemIds.SoulOfQuelaag or ItemIds.SoulOfOrnstein or ItemIds.SoulOfSmough or
        ItemIds.SoulOfNito or ItemIds.SoulOfBedOfChaos or ItemIds.SoulOfFourKings or
        ItemIds.SoulOfSeath or ItemIds.SoulOfArtorias or ItemIds.SoulOfManus;
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
