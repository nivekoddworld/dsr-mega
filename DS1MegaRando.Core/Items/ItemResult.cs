namespace DS1MegaRando.Core.Items;

public class ItemResult
{
    /// <summary>ItemLotParam row ID → (new item ID, lotItemCategory value).</summary>
    public Dictionary<int, (int ItemId, int Category)> LotAssignments { get; set; } = new();

    /// <summary>ShopLineupParam row ID → new item ID.</summary>
    public Dictionary<int, int> ShopAssignments { get; set; } = new();

    /// <summary>ShopLineupParam row ID → new price.</summary>
    public Dictionary<int, int> ShopPrices { get; set; } = new();

    /// <summary>Starting weapon/shield item IDs for the player character.</summary>
    public List<int> StartingItems { get; set; } = new();

    /// <summary>Human-readable key item placements for spoiler log.</summary>
    public List<(string ItemName, string AreaName, string LocationDesc)> KeyItemPlacements { get; set; } = new();
}
