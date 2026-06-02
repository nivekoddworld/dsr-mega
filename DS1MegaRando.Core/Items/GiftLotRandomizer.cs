using SoulsFormats;

namespace DS1MegaRando.Core.Items;

/// <summary>
/// Randomizes the items given by the character-creation starting-gift choices.
///
/// Seven of the nine non-None gift options are delivered through <c>ItemLotParam</c>
/// rows keyed by a specific <c>getItemFlagId</c>.  The other two (Binoculars,
/// Old Witch's Ring) have no valid DSR lot rows and remain vanilla.
///
/// Confirmed flag → vanilla gift mapping (verified against DSR-Gadget item IDs):
///   50000010 → Tiny Being's Ring      (ring 111)
///   50000082 → Black Firebombs ×5    (goods 201 legacy / ~297 DSR)
///   50000090 → Mysterious Humanity   (goods 500)
///   50001060 → Firekeeper Soul       (goods 390)
///   50001070 → Pendant               (goods 376)
///   50004066 → Master Key            (goods 2100)
///   50006371 → Twin Humanities ×2    (goods 501)
/// </summary>
public class GiftLotRandomizer
{
    private static readonly HashSet<int> GiftFlags = new()
    {
        50000010, // Tiny Being's Ring
        50000082, // Black Firebombs ×5
        50000090, // Mysterious Humanity
        50001060, // Firekeeper Soul
        50001070, // Pendant
        50004066, // Master Key
        50006371, // Twin Humanities ×2
    };

    /// <summary>
    /// Scans <paramref name="itemLotParam"/> for the known gift-delivery rows and
    /// replaces each one's item with a random pick from <paramref name="itemPool"/>
    /// (excluding key items and boss souls).
    /// Returns a <c>lotRowId → (itemId, lotCategory, count)</c> map ready to be
    /// written back by <see cref="IO.GameFileWriter"/>.
    /// </summary>
    public Dictionary<int, (int ItemId, int Category, int Count)> Randomize(
        PARAM? itemLotParam,
        List<PoolItem> itemPool,
        Random rng)
    {
        var result = new Dictionary<int, (int, int, int)>();
        if (itemLotParam?.AppliedParamdef == null) return result;

        var giftable = itemPool
            .Where(i => !i.IsKey && !i.IsBoss)
            .ToList();

        if (giftable.Count == 0) return result;

        foreach (var row in itemLotParam.Rows)
        {
            int flag = GetCell(row, "getItemFlagId");
            if (!GiftFlags.Contains(flag))
                continue;

            var pick = giftable[rng.Next(giftable.Count)];
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
