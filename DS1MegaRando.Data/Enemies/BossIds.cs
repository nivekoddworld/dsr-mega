namespace DS1MegaRando.Data.Enemies;

/// <summary>
/// Boss entity IDs per map used to identify the boss trigger entity in MSB files.
/// Entity IDs are the ThinkParamId / entity_id values in MSB NPC entries.
/// </summary>
public static class BossIds
{
    public record BossDef(string MapId, int EntityId, string ModelId, string Name);

    public static readonly IReadOnlyList<BossDef> All = new[]
    {
        new BossDef("m18_01_00_00", 1801800, "c2800", "Asylum Demon"),
        new BossDef("m10_01_00_00", 1010800, "c3080", "Bell Gargoyles"),
        new BossDef("m10_00_00_00", 1000800, "c3200", "Gaping Dragon"),
        new BossDef("m10_02_00_00", 1002990, "c2900", "Taurus Demon"),
        new BossDef("m10_02_00_00", 1002800, "c2910", "Capra Demon"),
        new BossDef("m14_00_00_00", 1400800, "c3210", "Chaos Witch Quelaag"),
        new BossDef("m11_00_00_00", 1100800, "c3100", "Moonlight Butterfly"),
        new BossDef("m12_00_00_01", 1200800, "c3120", "Sif the Great Wolf"),
        new BossDef("m14_01_00_00", 1410800, "c3220", "Ceaseless Discharge"),
        new BossDef("m14_01_00_00", 1410850, "c2920", "Demon Firesage"),
        new BossDef("m14_01_00_00", 1410900, "c2930", "Centipede Demon"),
        new BossDef("m14_01_00_00", 1410980, "c3230", "Bed of Chaos"),
        new BossDef("m13_01_00_00", 1310800, "c3240", "Gravelord Nito"),
        new BossDef("m16_00_00_00", 1600800, "c3250", "Four Kings"),
        new BossDef("m15_01_00_00", 1500800, "c3280", "Iron Golem"),
        new BossDef("m15_01_00_00", 1500900, "c3300", "Ornstein"),
        new BossDef("m15_01_00_00", 1500901, "c3301", "Smough"),
        new BossDef("m15_01_00_00", 1500980, "c3320", "Dark Sun Gwyndolin"),
        new BossDef("m17_00_00_00", 1700800, "c3260", "Seath the Scaleless"),
        new BossDef("m18_00_00_00", 1800800, "c3360", "Gwyn, Lord of Cinder"),
        new BossDef("m12_01_00_00", 1210800, "c3380", "Knight Artorias"),
        new BossDef("m12_01_00_00", 1210850, "c3390", "Black Dragon Kalameet"),
        new BossDef("m12_01_00_00", 1210900, "c3400", "Manus, Father of the Abyss"),
    };
}
