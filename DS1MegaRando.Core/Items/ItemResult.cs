namespace DS1MegaRando.Core.Items;

public class ItemResult
{
    /// <summary>ItemLotParam row ID → (new item ID, lotItemCategory value).</summary>
    public Dictionary<int, (int ItemId, int Category)> LotAssignments { get; set; } = new();

    /// <summary>ShopLineupParam row ID → new item ID.</summary>
    public Dictionary<int, int> ShopAssignments { get; set; } = new();

    /// <summary>ShopLineupParam row ID → new price.</summary>
    public Dictionary<int, int> ShopPrices { get; set; } = new();

    /// <summary>Per-class starting weapon loadouts (one entry per character-creator class).</summary>
    public List<StartingLoadout> StartingLoadouts { get; set; } = new();

    /// <summary>When true, the writer boosts each class's base stats to meet their randomized weapon requirements.</summary>
    public bool AdjustStatsForWeapons { get; set; } = false;

    /// <summary>Gift lot row ID → (itemId, lotCategory, count) replacements for the character-creation gift choices.</summary>
    public Dictionary<int, (int ItemId, int Category, int Count)> GiftLotAssignments { get; set; } = new();


    /// <summary>Human-readable key item placements for spoiler log.</summary>
    public List<(string ItemName, string AreaName, string LocationDesc)> KeyItemPlacements { get; set; } = new();
}
