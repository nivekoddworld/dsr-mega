using DS1MegaRando.Core.IO;
using SoulsFormats;

namespace DS1MegaRando.Core.Items;

/// <summary>
/// Randomizes the items given by the character-creation starting-gift choices.
///
/// In DS1R the gift selection screen is hardcoded in the game engine; each gift
/// choice sets an event flag in the 50000000–50000090 range, and the matching
/// <c>ItemLotParam</c> row (identified by its <c>getItemFlagId</c> field) delivers
/// the item when the character first spawns.  Replacing the lot's item ID changes
/// what the player actually receives for that gift choice while the menu labels
/// (e.g. "Pendant", "Binoculars") remain unchanged.
/// </summary>
public class GiftLotRandomizer
{
    // Event flags set by the character-creation gift selection.
    // Every ItemLotParam row whose getItemFlagId matches one of these values is
    // a gift-delivery lot.  The range 50000000–50000082 covers all nine vanilla
    // gift options; 50000090 is the Lordvessel (given by Gwynevere, not a gift)
    // and is deliberately excluded.
    private static readonly HashSet<int> GiftFlags = new()
    {
        50000000, 50000010, 50000020, 50000030, 50000040,
        50000050, 50000060, 50000070, 50000080, 50000081, 50000082,
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
            int flag = GetCell(row, "getItemFlagId");
            if (!GiftFlags.Contains(flag)) continue;

            var pick = giftableItems[rng.Next(giftableItems.Count)];
            result[(int)row.ID] = (pick.ItemId, pick.LotCategory, Math.Max(1, pick.Count));
        }

        return result;
    }

    private static int GetCell(PARAM.Row row, string name)
    {
        try { return Convert.ToInt32(row[name]?.Value ?? 0); }
        catch { return 0; }
    }
}
