using DS1MegaRando.Data.Items;
using SoulsFormats;

namespace DS1MegaRando.Items;

/// <summary>
/// Randomizes the items given by the character-creation starting-gift choices.
///
/// Each gift now delivers a random item drawn from the full game catalog
/// (<see cref="GiftItemCatalog"/>) — every real weapon, shield, armor piece, ring,
/// spell and consumable — excluding developer/debug entries and progression-critical
/// keys (so a gift can never break key-item logic).
///
/// Gift lot row IDs confirmed from DarkSoulsItemRandomizer-master/items_setup.py
/// (the PTDE Python randomizer in this repo), cross-checked against DSR-Gadget:
///
///   1010 → Tiny Being's Ring
///   1082 → Black Firebombs ×5
///   2060 → Firekeeper Soul
///   2070 → Pendant
///   4073 → Master Key
///   6371 → Twin Humanities ×2
///
/// (The Estus Flask is no longer delivered through lot 1082 — it is granted via
/// CharaInitParam — so lot 1082 is once again a normal randomizable gift lot.)
/// </summary>
public class GiftLotRandomizer
{
    /// <summary>Raw ItemLotParam category for goods/consumables.</summary>
    private const int GoodsCategory = 0x40000000;

    private static readonly HashSet<int> GiftLotIds = new()
    {
        1010, // Tiny Being's Ring
        1082, // Black Firebombs ×5
        2060, // Firekeeper Soul
        2070, // Pendant
        4073, // Master Key
        6371, // Twin Humanities ×2
    };

    public event EventHandler<string>? Log;

    public Dictionary<int, (int ItemId, int Category, int Count)> Randomize(
        PARAM? itemLotParam,
        Random rng)
    {
        var result = new Dictionary<int, (int, int, int)>();

        if (itemLotParam?.AppliedParamdef == null)
        {
            Log?.Invoke(this, "GiftLot: ItemLotParam unavailable — skipping");
            return result;
        }

        var catalog = GiftItemCatalog.Items;
        if (catalog.Length == 0)
        {
            Log?.Invoke(this, "GiftLot: catalog is empty — skipping");
            return result;
        }

        Log?.Invoke(this, $"GiftLot: picking from full catalog of {catalog.Length} items...");

        foreach (var row in itemLotParam.Rows)
        {
            if (!GiftLotIds.Contains((int)row.ID))
                continue;

            var (itemId, category) = catalog[rng.Next(catalog.Length)];
            // Consumable goods are handed out in a small stack; equipment is a single item.
            int count = category == GoodsCategory ? rng.Next(2, 6) : 1;

            result[(int)row.ID] = (itemId, category, count);
            Log?.Invoke(this, $"GiftLot: row {row.ID} → item {itemId} cat 0x{category:X8} ×{count}");
        }

        Log?.Invoke(this, $"GiftLot: {result.Count}/{GiftLotIds.Count} gift lot(s) replaced.");
        return result;
    }
}
