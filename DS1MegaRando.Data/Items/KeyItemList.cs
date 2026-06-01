namespace DS1MegaRando.Data.Items;

/// <summary>
/// Progression-critical key items. IDs match DS1R ItemLotParam item_id values.
/// </summary>
public static class KeyItemList
{
    public static readonly IReadOnlyList<KeyItemDef> All = new[]
    {
        // Bells / progression gates
        new KeyItemDef(2000, "Bell of Awakening (Undead Parish)"),
        new KeyItemDef(2001, "Bell of Awakening (Quelaag's Domain)"),

        // Lord Souls
        new KeyItemDef(2005, "Bequeathed Lord Soul Shard (Seath)"),
        new KeyItemDef(2006, "Bequeathed Lord Soul Shard (Four Kings)"),
        new KeyItemDef(2007, "Lord Soul (Bed of Chaos)"),
        new KeyItemDef(2008, "Lord Soul (Gravelord Nito)"),

        // Keys
        new KeyItemDef(2010, "Basement Key"),
        new KeyItemDef(2011, "Blighttown Key"),
        new KeyItemDef(2012, "Key to New Londo Ruins"),
        new KeyItemDef(2013, "Annex Key"),
        new KeyItemDef(2014, "Dungeon Cell Key"),
        new KeyItemDef(2015, "Big Pilgrim's Key"),
        new KeyItemDef(2016, "Undead Asylum F2 East Key"),
        new KeyItemDef(2017, "Key to the Seal"),
        new KeyItemDef(2018, "Key to Depths"),
        new KeyItemDef(2019, "Residence Key"),
        new KeyItemDef(2020, "Crest of Artorias"),
        new KeyItemDef(2021, "Cage Key"),
        new KeyItemDef(2022, "Archive Tower Cell Key"),
        new KeyItemDef(2023, "Archive Tower Giant Door Key"),
        new KeyItemDef(2024, "Archive Tower Giant Cell Key"),
        new KeyItemDef(2025, "Watchtower Basement Key"),
        new KeyItemDef(2026, "Mystery Key"),
        new KeyItemDef(2027, "Sewer Chamber Key"),
        new KeyItemDef(2028, "Undead Asylum F2 West Key"),
        new KeyItemDef(2029, "Peculiar Doll"),

        // Rite / special progression
        new KeyItemDef(2030, "Lordvessel"),
        new KeyItemDef(2031, "Covenant of Artorias"),
        new KeyItemDef(2032, "Broken Pendant"),

        // Embers (required for some NPC interactions)
        new KeyItemDef(2040, "Divine Ember"),
        new KeyItemDef(2041, "Large Divine Ember"),
        new KeyItemDef(2042, "Dark Ember"),
        new KeyItemDef(2043, "Large Ember"),
        new KeyItemDef(2044, "Very Large Ember"),
        new KeyItemDef(2045, "Large Magic Ember"),
        new KeyItemDef(2046, "Enchanted Ember"),
        new KeyItemDef(2047, "Crystal Ember"),
        new KeyItemDef(2048, "Large Flame Ember"),
        new KeyItemDef(2049, "Chaos Flame Ember"),
    };

    private static readonly IReadOnlyDictionary<int, KeyItemDef> _byId =
        All.ToDictionary(k => k.ItemId);

    public static KeyItemDef? ById(int itemId) =>
        _byId.TryGetValue(itemId, out var k) ? k : null;

    public static bool IsKeyItem(int itemId) => _byId.ContainsKey(itemId);
}

public record KeyItemDef(int ItemId, string Name);
