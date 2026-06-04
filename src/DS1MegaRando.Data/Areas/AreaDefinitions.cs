namespace DS1MegaRando.Data.Areas;

public enum DS1Area
{
    UndeadAsylum,
    Firelink,
    UndeadBurg,
    UndeadParish,
    Depths,
    LowerUndeadBurg,
    DarkrootGarden,
    DarkrootGardenOld,
    DarkrootBasin,
    DarkrootForest,
    Oolacile,
    OolacileSanctuary,
    RoyalWood,
    ChasmOfTheAbyss,
    Catacombs,
    TombOfTheGiants,
    GreatHollow,
    AshLake,
    Blighttown,
    QuelaagsDomain,
    DemonRuins,
    LostIzalith,
    SensFortress,
    AnorLondo,
    PaintedWorld,
    NewLondo,
    ValleyOfDrakes,
    DukesArchives,
    CrystalCave,
    KilnOfTheFirstFlame,
}

public static class AreaInfo
{
    public static readonly IReadOnlyDictionary<DS1Area, string> DisplayName = new Dictionary<DS1Area, string>
    {
        [DS1Area.UndeadAsylum]      = "Undead Asylum",
        [DS1Area.Firelink]          = "Firelink Shrine",
        [DS1Area.UndeadBurg]        = "Undead Burg",
        [DS1Area.UndeadParish]      = "Undead Parish",
        [DS1Area.Depths]            = "The Depths",
        [DS1Area.LowerUndeadBurg]   = "Lower Undead Burg",
        [DS1Area.DarkrootGarden]    = "Darkroot Garden",
        [DS1Area.DarkrootBasin]     = "Darkroot Basin",
        [DS1Area.DarkrootForest]    = "Darkroot Forest",
        [DS1Area.Oolacile]          = "Oolacile Township",
        [DS1Area.OolacileSanctuary] = "Oolacile Sanctuary",
        [DS1Area.RoyalWood]         = "Royal Wood",
        [DS1Area.ChasmOfTheAbyss]   = "Chasm of the Abyss",
        [DS1Area.Catacombs]         = "The Catacombs",
        [DS1Area.TombOfTheGiants]   = "Tomb of the Giants",
        [DS1Area.GreatHollow]       = "The Great Hollow",
        [DS1Area.AshLake]           = "Ash Lake",
        [DS1Area.Blighttown]        = "Blighttown",
        [DS1Area.QuelaagsDomain]    = "Quelaag's Domain",
        [DS1Area.DemonRuins]        = "Demon Ruins",
        [DS1Area.LostIzalith]       = "Lost Izalith",
        [DS1Area.SensFortress]      = "Sen's Fortress",
        [DS1Area.AnorLondo]         = "Anor Londo",
        [DS1Area.PaintedWorld]      = "Painted World of Ariamis",
        [DS1Area.NewLondo]          = "New Londo Ruins",
        [DS1Area.ValleyOfDrakes]    = "Valley of Drakes",
        [DS1Area.DukesArchives]     = "Duke's Archives",
        [DS1Area.CrystalCave]       = "Crystal Cave",
        [DS1Area.KilnOfTheFirstFlame] = "Kiln of the First Flame",
    };

    public static readonly IReadOnlyCollection<DS1Area> DLCAreas = new[]
    {
        DS1Area.Oolacile, DS1Area.OolacileSanctuary, DS1Area.RoyalWood, DS1Area.ChasmOfTheAbyss
    };

    public static bool IsDLC(DS1Area area) => DLCAreas.Contains(area);
}
