namespace DS1MegaRando.Data.Items;

/// <summary>
/// Progression-critical key items. IDs are DS1R ItemLotParam item_id values and are
/// kept in sync with the key items defined in the annotation data (ds1-fog.yaml),
/// which drives key-item placement. Matching the two ensures these items are excluded
/// from the general item pool so they are never double-placed.
/// </summary>
public static class KeyItemList
{
    public static readonly IReadOnlyList<KeyItemDef> All = new[]
    {
        // Lord Souls
        new KeyItemDef(2500, "Lord Soul (Nito)"),
        new KeyItemDef(2501, "Lord Soul (Bed of Chaos)"),
        new KeyItemDef(2502, "Bequeathed Lord Soul Shard (Four Kings)"),
        new KeyItemDef(2503, "Bequeathed Lord Soul Shard (Seath)"),

        // Endgame progression
        new KeyItemDef(2510, "Lordvessel"),
        new KeyItemDef(2520, "Broken Pendant"),

        // Keys
        new KeyItemDef(2001, "Basement Key"),
        new KeyItemDef(2002, "Crest of Artorias"),
        new KeyItemDef(2003, "Cage Key"),
        new KeyItemDef(2005, "Archive Tower Giant Door Key"),
        new KeyItemDef(2006, "Archive Tower Giant Cell Key"),
        new KeyItemDef(2007, "Blighttown Key"),
        new KeyItemDef(2008, "Key to New Londo Ruins"),
        new KeyItemDef(2009, "Annex Key"),
        new KeyItemDef(2013, "Key to the Seal"),
        new KeyItemDef(2014, "Key to Depths"),
        new KeyItemDef(2016, "Undead Asylum F2 West Key"),
        new KeyItemDef(2019, "Watchtower Basement Key"),
        new KeyItemDef(2020, "Archive Prison Extra Key"),
        new KeyItemDef(2021, "Residence Key"),
        new KeyItemDef(2022, "Crest Key"),

        // Special progression goods
        new KeyItemDef(118,  "Purple Coward's Crystal"),
        new KeyItemDef(384,  "Peculiar Doll"),
        new KeyItemDef(3500, "Sorcery: Cast Light"),

        // Covenant / progression rings
        new KeyItemDef(138,  "Covenant of Artorias"),
        new KeyItemDef(139,  "Orange Charred Ring"),
        new KeyItemDef(149,  "Darkmoon Seance Ring"),
    };

    private static readonly IReadOnlyDictionary<int, KeyItemDef> _byId =
        All.ToDictionary(k => k.ItemId);

    public static KeyItemDef? ById(int itemId) =>
        _byId.TryGetValue(itemId, out var k) ? k : null;

    public static bool IsKeyItem(int itemId) => _byId.ContainsKey(itemId);
}

public record KeyItemDef(int ItemId, string Name);
