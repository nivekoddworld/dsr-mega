namespace DS1MegaRando.Core.Settings;

public enum ItemDifficultyMode { Easy, Medium, Hard }
public enum KeyItemMode { Vanilla, Shuffled, RaceMode, SpeedrunMode }
public enum BossSoulHandling { Shuffle, ReplaceWithConsumable, AllowTranspose }
public enum ShopPriceMode { Vanilla, Scaled, Random, Free }
public enum StartingLoadoutMode { ShieldAnd1H, ShieldAnd1HAnd2H, CombinedPool }

public class ItemSettings
{
    public bool EnableItemRandomizer { get; set; } = true;
    public ItemDifficultyMode Difficulty { get; set; } = ItemDifficultyMode.Medium;
    public KeyItemMode KeyItemMode { get; set; } = KeyItemMode.Shuffled;
    public bool RandomizeLordvessel { get; set; } = false;
    public bool RandomizeLordSouls { get; set; } = false;
    public BossSoulHandling BossSoulHandling { get; set; } = BossSoulHandling.Shuffle;
    public bool IncludeDLCItemLocations { get; set; } = true;
    public bool RandomizeShops { get; set; } = true;
    public ShopPriceMode ShopPriceMode { get; set; } = ShopPriceMode.Scaled;
    public int ShopPriceRandomMin { get; set; } = 50;
    public int ShopPriceRandomMax { get; set; } = 20000;
    public bool RandomizeStartingLoadout { get; set; } = true;
    public StartingLoadoutMode StartingLoadoutMode { get; set; } = StartingLoadoutMode.ShieldAnd1H;
    /// <summary>Randomize each starting class's armor (helm/chest/gauntlets/legs) per piece.</summary>
    public bool FashionSouls { get; set; } = false;
    /// <summary>Give each starting class a random bonus consumable gift (placed in their first free item slot).</summary>
    public bool RandomizeStartingGift { get; set; } = false;
    /// <summary>Boost a class's base STR/DEX/INT/FTH (and soul level) to meet the minimum requirements of their randomized starting weapons.</summary>
    public bool AdjustStatsForWeapons { get; set; } = false;
    public bool RandomizeNPCDropArmor { get; set; } = false;
    public bool ProtectNPCQuestItems { get; set; } = true;
    public bool EnsureUpgradeMaterialAccess { get; set; } = true;
    public bool AllowKeyItemsInShops { get; set; } = false;
    public bool ExcludeUselessItems { get; set; } = false;
    public bool GuaranteeRepairBox { get; set; } = true;
    public bool SpoilerLogItemPlacements { get; set; } = true;
}
