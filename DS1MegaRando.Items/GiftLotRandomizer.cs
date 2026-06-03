using SoulsFormats;

namespace DS1MegaRando.Items;

/// <summary>
/// Randomizes the items given by the character-creation starting-gift choices.
///
/// Gift lot row IDs confirmed from DarkSoulsItemRandomizer-master/items_setup.py
/// (the PTDE Python randomizer in this repo), cross-checked against DSR-Gadget:
///
///   1010 → Tiny Being's Ring  (ring 111)
///   2060 → Firekeeper Soul    (goods 390)
///   2070 → Pendant            (goods 376)
///   4073 → Master Key         (goods 2100)
///   6371 → Twin Humanities ×2 (goods 501)
///
/// Lot 1082 (Black Firebombs ×5) is intentionally excluded: it is also the lot
/// used by ApplyStartingEstus to deliver a guaranteed Estus Flask when fog gate
/// randomization is active, so modifying it here would break that guarantee.
///
/// Humanity (single), Binoculars, and Old Witch's Ring are delivered
/// engine-side and cannot be changed through ItemLotParam.
/// </summary>
public class GiftLotRandomizer
{
    private static readonly HashSet<int> GiftLotIds = new()
    {
        1010, // Tiny Being's Ring
        // 1082 excluded — reserved for Estus Flask guarantee (ApplyStartingEstus)
        2060, // Firekeeper Soul
        2070, // Pendant
        4073, // Master Key
        6371, // Twin Humanities ×2
    };

    public event EventHandler<string>? Log;

    public Dictionary<int, (int ItemId, int Category, int Count)> Randomize(
        PARAM? itemLotParam,
        List<PoolItem> itemPool,
        Random rng)
    {
        var result = new Dictionary<int, (int, int, int)>();

        if (itemLotParam == null)
        {
            Log?.Invoke(this, "GiftLot: ItemLotParam is null — skipping");
            return result;
        }
        if (itemLotParam.AppliedParamdef == null)
        {
            Log?.Invoke(this, "GiftLot: ItemLotParam has no paramdef applied — skipping");
            return result;
        }

        Log?.Invoke(this, $"GiftLot: targeting {GiftLotIds.Count} known gift lot row IDs...");

        var giftable = itemPool
            .Where(i => !i.IsKey && !i.IsBoss)
            .ToList();

        if (giftable.Count == 0)
        {
            Log?.Invoke(this, "GiftLot: item pool is empty — skipping");
            return result;
        }

        foreach (var row in itemLotParam.Rows)
        {
            if (!GiftLotIds.Contains((int)row.ID))
                continue;

            var pick = giftable[rng.Next(giftable.Count)];
            result[(int)row.ID] = (pick.ItemId, pick.LotCategory, Math.Max(1, pick.Count));
            Log?.Invoke(this, $"GiftLot: row {row.ID} → item {pick.ItemId} cat {pick.LotCategory} ×{pick.Count}");
        }

        Log?.Invoke(this, $"GiftLot: {result.Count}/{GiftLotIds.Count} gift lot(s) found and replaced.");
        return result;
    }
}
