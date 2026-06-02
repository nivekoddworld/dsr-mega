using DS1MegaRando.Core.IO;
using SoulsFormats;

namespace DS1MegaRando.Core.Items;

/// <summary>
/// Randomizes the items given by the character-creation starting-gift choices.
///
/// The gift-delivery lots are identified by their fixed row IDs in <c>ItemLotParam</c>:
///   1000 = Divine Blessing, 1010 = Black Firebomb, 1020 = Twin Humanities,
///   1030 = Binoculars, 1040 = Pendant, 1050 = Master Key,
///   1060 = Tiny Being's Ring, 1070 = Old Witch's Ring.
/// Replacing each lot's item changes what the player receives for that gift
/// choice; the menu labels in the character-creation screen are engine-side
/// and remain unchanged.
/// </summary>
public class GiftLotRandomizer
{
    // The eight non-None starting-gift lot row IDs in DS1's ItemLotParam.
    private static readonly HashSet<int> GiftLotIds = new()
    {
        1000, 1010, 1020, 1030, 1040, 1050, 1060, 1070,
    };

    /// <summary>
    /// Returns a dictionary of <c>lotRowId → (itemId, category, count)</c>
    /// to write back into <c>ItemLotParam</c>.  The items are drawn from
    /// <paramref name="itemPool"/> — the same pool the main item randomizer
    /// uses — so their IDs and categories are always valid for the current
    /// game version.
    /// </summary>
    public Dictionary<int, (int ItemId, int Category, int Count)> Randomize(
        PARAM? itemLotParam,
        List<PoolItem> itemPool,
        Random rng)
    {
        var result = new Dictionary<int, (int, int, int)>();
        if (itemLotParam?.AppliedParamdef == null) return result;
        if (itemPool.Count == 0) return result;

        // Build a pool of non-key, non-boss items to use as gift replacements.
        // Key items and boss souls are excluded — receiving a key item as a gift
        // would bypass progression; boss souls are confusing out of context.
        var giftableItems = itemPool
            .Where(i => !i.IsKey && !i.IsBoss)
            .ToList();

        if (giftableItems.Count == 0) return result;

        foreach (var row in itemLotParam.Rows)
        {
            if (!GiftLotIds.Contains((int)row.ID)) continue;

            var pick = giftableItems[rng.Next(giftableItems.Count)];
            result[(int)row.ID] = (pick.ItemId, pick.LotCategory, Math.Max(1, pick.Count));
        }

        return result;
    }
}
