namespace DS1MegaRando.Data.Areas;

public record MapSpec(string MapId, string InternalName, DS1Area Area);

public static class MapSpecs
{
    public static readonly IReadOnlyList<MapSpec> All = new[]
    {
        new MapSpec("m10_00_00_00", "depths",       DS1Area.Depths),
        new MapSpec("m10_01_00_00", "parish",       DS1Area.UndeadParish),
        new MapSpec("m10_02_00_00", "firelink",     DS1Area.Firelink),
        new MapSpec("m11_00_00_00", "paintedworld", DS1Area.PaintedWorld),
        new MapSpec("m12_00_00_00", "darkroot0",    DS1Area.DarkrootGardenOld),
        new MapSpec("m12_00_00_01", "darkroot",     DS1Area.DarkrootGarden),
        new MapSpec("m12_01_00_00", "dlc",          DS1Area.Oolacile),
        new MapSpec("m13_00_00_00", "catacombs",    DS1Area.Catacombs),
        new MapSpec("m13_01_00_00", "totg",         DS1Area.TombOfTheGiants),
        new MapSpec("m13_02_00_00", "greathollow",  DS1Area.GreatHollow),
        new MapSpec("m14_00_00_00", "blighttown",   DS1Area.Blighttown),
        new MapSpec("m14_01_00_00", "demonruins",   DS1Area.DemonRuins),
        new MapSpec("m15_00_00_00", "sens",         DS1Area.SensFortress),
        new MapSpec("m15_01_00_00", "anorlondo",    DS1Area.AnorLondo),
        new MapSpec("m16_00_00_00", "newlondo",     DS1Area.NewLondo),
        new MapSpec("m17_00_00_00", "dukes",        DS1Area.DukesArchives),
        new MapSpec("m18_00_00_00", "kiln",         DS1Area.KilnOfTheFirstFlame),
        new MapSpec("m18_01_00_00", "asylum",       DS1Area.UndeadAsylum),
    };

    private static readonly IReadOnlyDictionary<string, MapSpec> _byMapId =
        All.ToDictionary(m => m.MapId);
    private static readonly IReadOnlyDictionary<DS1Area, MapSpec> _byArea =
        All.ToDictionary(m => m.Area);

    public static MapSpec? ByMapId(string mapId) =>
        _byMapId.TryGetValue(mapId, out var s) ? s : null;

    public static MapSpec? ByArea(DS1Area area) =>
        _byArea.TryGetValue(area, out var s) ? s : null;
}
