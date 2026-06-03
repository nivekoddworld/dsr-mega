using DS1MegaRando.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Items;

/// <summary>
/// A single character's starting loadout. The slot values map directly onto the
/// CharaInitParam equip fields.
///   <see cref="Empty"/> (-1): explicitly unequip the slot (DS1's empty-slot value).
///   <see cref="Keep"/>  (-2): leave the slot's vanilla value untouched.
/// </summary>
public readonly record struct StartingLoadout(
    int RightHand, int LeftHand, int SubRightHand,
    int Helm, int Chest, int Gauntlets, int Legs,
    int Arrow = -2, int Bolt = -2)
{
    /// <summary>Sentinel for an empty equip slot (matches the CharaInitParam default).</summary>
    public const int Empty = -1;

    /// <summary>Sentinel meaning "don't write this slot — keep the class's vanilla value".</summary>
    public const int Keep = -2;
}

public class StartingLoadoutRandomizer
{
    /// <summary>Number of player-selectable classes in the character creator (CharaInitParam rows 3000-3009).</summary>
    public const int PlayerClassCount = 10;

    private static readonly int[] OneHandedWeapons =
    {
        // Daggers
        ItemIds.Dagger, ItemIds.ParryingDagger, ItemIds.GhostBlade,
        ItemIds.BanditsKnife, ItemIds.PriscillasDagger,
        // Straight Swords
        ItemIds.Shortsword, ItemIds.Longsword, ItemIds.Broadsword,
        ItemIds.BalderSideSword, ItemIds.Darksword, ItemIds.DrakeSwrd,
        ItemIds.SilverKnightSword, ItemIds.BarbedStraightSword,
        ItemIds.AstoraStraightSword, ItemIds.SunlightStraightSword, ItemIds.Weapon211000,
        // Curved Swords
        ItemIds.Scimitar, ItemIds.Falchion, ItemIds.Shotel,
        ItemIds.JaggedGhostBlade, ItemIds.CurvedSword405, ItemIds.CurvedSword406,
        ItemIds.PaintingGuardianSword, ItemIds.GoldTracer, ItemIds.DarkSilverTracer,
        // Katanas
        ItemIds.Uchigatana, ItemIds.Katana501, ItemIds.Iaito,
        // Thrusting Swords
        ItemIds.MailBreaker, ItemIds.Rapier, ItemIds.Estoc,
        ItemIds.VelkasRapier, ItemIds.RicardsRapier,
        // Axes
        ItemIds.HandAxe, ItemIds.BattleAxe,
        ItemIds.CrescentAxe, ItemIds.GargoyleTailAxe, ItemIds.GolemAxe, ItemIds.ButchersKnife,
        // Hammers
        ItemIds.Club, ItemIds.Mace, ItemIds.MorningStar, ItemIds.Pickaxe,
        ItemIds.Hammer804, ItemIds.ReinforcedClub,
        ItemIds.BlacksmithHammer, ItemIds.BlacksmithGiantHammer, ItemIds.DragonToothHammer,
        // Fists
        ItemIds.Caestus, ItemIds.Claw, ItemIds.Fist903, ItemIds.DarkHand,
        // Spears
        ItemIds.Spear, ItemIds.WingedSpear, ItemIds.ChannelersTrident,
        ItemIds.Partizan, ItemIds.Spear1004, ItemIds.SilverKnightSpear, ItemIds.DemonsSpear,
        // Whips
        ItemIds.Whip, ItemIds.NotchedWhip,
    };

    private static readonly int[] TwoHandedWeapons =
    {
        // Greatswords
        ItemIds.BastardSword, ItemIds.Claymore, ItemIds.ManSerpentGreatsword,
        ItemIds.Flamberge, ItemIds.Greatsword304, ItemIds.BlackKnightSword,
        ItemIds.BlackKnightGreatsword, ItemIds.Greatsword309,
        ItemIds.ArtorisasGreatsword, ItemIds.AbyssGreatsword, ItemIds.Greatsword314,
        // Ultra Greatswords
        ItemIds.Zweihander, ItemIds.Greatsword, ItemIds.StoneGreatsword,
        ItemIds.UltraGreatsword354, ItemIds.MoonlightGreatsword,
        // Curved Greatswords / Katanas
        ItemIds.Murakumo, ItemIds.CurvedGreatsword453,
        ItemIds.WashingPole, ItemIds.ChaosBlade,
        // Great Axes
        ItemIds.Greataxe, ItemIds.BlackKnightGreataxe, ItemIds.DemonsGreataxe, ItemIds.GreatAxe753,
        // Great Hammers
        ItemIds.GreatClub, ItemIds.GreatHammer851, ItemIds.GreatHammer852,
        ItemIds.GreatHammer854, ItemIds.LargeClub, ItemIds.GreatHammer856,
        ItemIds.Grant, ItemIds.SmoughsHammer,
        // Great Spears
        ItemIds.Pike, ItemIds.GreatSpear1051, ItemIds.GreatSpear1052, ItemIds.GreatSpear1054,
        // Halberds
        ItemIds.Halberd, ItemIds.BlackKnightHalberd, ItemIds.TitaniteCatchPole,
        ItemIds.GargoyleHalberd, ItemIds.Halberd1105,
        ItemIds.Lucerne, ItemIds.Scythe, ItemIds.GreatScythe, ItemIds.Scythe1151, ItemIds.LifehuntScythe,
    };

    private static readonly int[] Bows =
    {
        ItemIds.Shortbow, ItemIds.Longbow, ItemIds.CompositeBow,
        ItemIds.BlackBowOfPharis, ItemIds.Bow1204, ItemIds.Bow1205,
    };

    private static readonly int[] Greatbows =
    {
        ItemIds.Greatbow, ItemIds.Greatbow1251, ItemIds.Greatbow1252, ItemIds.GoughsGreatbow,
    };

    private static readonly int[] Crossbows =
    {
        ItemIds.LightCrossbow, ItemIds.HeavyCrossbow, ItemIds.SniperCrossbow,
        ItemIds.Avelyn, ItemIds.Crossbow1304, ItemIds.Crossbow1305,
        ItemIds.Crossbow1306, ItemIds.Crossbow1307, ItemIds.Crossbow1308,
    };

    private static readonly HashSet<int> BowIds = new()
    {
        ItemIds.Shortbow, ItemIds.Longbow, ItemIds.CompositeBow,
        ItemIds.BlackBowOfPharis, ItemIds.Bow1204, ItemIds.Bow1205,
        ItemIds.Greatbow, ItemIds.Greatbow1251, ItemIds.Greatbow1252, ItemIds.GoughsGreatbow,
    };

    private static readonly HashSet<int> CrossbowIds = new()
    {
        ItemIds.LightCrossbow, ItemIds.HeavyCrossbow, ItemIds.SniperCrossbow,
        ItemIds.Avelyn, ItemIds.Crossbow1304, ItemIds.Crossbow1305,
        ItemIds.Crossbow1306, ItemIds.Crossbow1307, ItemIds.Crossbow1308,
    };

    private static readonly int[] Shields =
    {
        ItemIds.EastWestShield, ItemIds.WoodenShield, ItemIds.LargeLeatherShield,
        ItemIds.SmallLeatherShield, ItemIds.TargetShield, ItemIds.BucklerShield,
        ItemIds.CrackedRoundShield, ItemIds.LeatherShield, ItemIds.PlankShield,
        ItemIds.HeaterShield, ItemIds.KnightShield, ItemIds.TowerKiteShield,
        ItemIds.GrassCrestShield, ItemIds.HollowSoldierShield, ItemIds.SpiderShield,
        ItemIds.SpikedShield, ItemIds.EagleShield, ItemIds.TowerShield,
    };

    // Base IDs of every full DS1 armor set (EquipParamProtector). Each set's four
    // pieces are at base+0 (helm), base+1000 (chest), base+2000 (gauntlets),
    // base+3000 (legs). Armor slots are rolled independently per class, so a class
    // can mix pieces from different sets.
    private static readonly int[] ArmorSetBases =
    {
        10000, 20000, 40000, 50000, 60000, 70000, 80000, 90000, 100000, 110000,
        120000, 130000, 140000, 150000, 160000, 170000, 180000, 190000, 200000, 210000,
        220000, 230000, 240000, 250000, 270000, 280000, 290000, 300000, 310000, 320000,
        340000, 350000, 360000, 370000, 390000, 400000, 410000, 420000, 440000, 450000,
        460000, 470000, 480000, 490000, 500000, 510000, 520000, 530000, 540000, 550000,
        560000, 570000, 580000, 590000, 600000, 610000, 620000, 630000, 640000, 650000,
        660000, 670000, 680000, 690000, 700000, 710000, 720000,
    };

    private const int ArmorHelmOffset  = 0;
    private const int ArmorChestOffset = 1000;
    private const int ArmorGauntOffset = 2000;
    private const int ArmorLegOffset   = 3000;


    /// <summary>
    /// Rolls an independent starting loadout for every player class. Weapons are rolled
    /// only when <see cref="ItemSettings.RandomizeStartingLoadout"/> is set; armor only
    /// when <see cref="ItemSettings.FashionSouls"/> is set; gift only when
    /// <see cref="ItemSettings.RandomizeStartingGift"/> is set. Slots for a group that
    /// isn't being randomized are left as <see cref="StartingLoadout.Keep"/> so the
    /// class's vanilla gear in that group is preserved.
    /// </summary>
    public List<StartingLoadout> RandomizePerClass(ItemSettings settings, Random rng)
    {
        var result = new List<StartingLoadout>(PlayerClassCount);
        for (int i = 0; i < PlayerClassCount; i++)
            result.Add(RollOne(settings.StartingLoadoutMode,
                randomizeWeapons: settings.RandomizeStartingLoadout,
                randomizeArmor:   settings.FashionSouls,
                rng));
        return result;
    }

    private static StartingLoadout RollOne(
        StartingLoadoutMode mode, bool randomizeWeapons, bool randomizeArmor, Random rng)
    {
        var (rightHand, leftHand, subRightHand, arrow, bolt) = randomizeWeapons
            ? RollWeapons(mode, rng)
            : (StartingLoadout.Keep, StartingLoadout.Keep, StartingLoadout.Keep, StartingLoadout.Keep, StartingLoadout.Keep);

        return new StartingLoadout(
            rightHand, leftHand, subRightHand,
            Helm:      randomizeArmor ? RollArmorPiece(ArmorHelmOffset,  rng) : StartingLoadout.Keep,
            Chest:     randomizeArmor ? RollArmorPiece(ArmorChestOffset, rng) : StartingLoadout.Keep,
            Gauntlets: randomizeArmor ? RollArmorPiece(ArmorGauntOffset, rng) : StartingLoadout.Keep,
            Legs:      randomizeArmor ? RollArmorPiece(ArmorLegOffset,   rng) : StartingLoadout.Keep,
            Arrow:     arrow,
            Bolt:      bolt);
    }

    private static (int right, int left, int subRight, int arrow, int bolt) RollWeapons(StartingLoadoutMode mode, Random rng)
    {
        switch (mode)
        {
            case StartingLoadoutMode.ShieldAnd1H:
                // Keep the off-hand slot so caster classes retain their catalyst/flame.
                return (Pick(OneHandedWeapons, rng), Pick(Shields, rng), StartingLoadout.Keep, StartingLoadout.Keep, StartingLoadout.Keep);

            case StartingLoadoutMode.ShieldAnd1HAnd2H:
                // The 2H weapon goes in the right off-hand slot so the player actually
                // receives it (the old flat-list code dropped it entirely).
                return (Pick(OneHandedWeapons, rng), Pick(Shields, rng), Pick(TwoHandedWeapons, rng), StartingLoadout.Keep, StartingLoadout.Keep);

            case StartingLoadoutMode.CombinedPool:
            {
                var combined = OneHandedWeapons.Concat(TwoHandedWeapons).Concat(Bows).Concat(Greatbows).Concat(Crossbows).ToArray();
                int weapon = combined[rng.Next(combined.Length)];
                int shield = rng.Next(2) == 0 ? Pick(Shields, rng) : StartingLoadout.Empty;
                int arrow = BowIds.Contains(weapon) ? ItemIds.StandardArrow : StartingLoadout.Keep;
                int bolt  = CrossbowIds.Contains(weapon) ? ItemIds.StandardBolt : StartingLoadout.Keep;
                return (weapon, shield, StartingLoadout.Keep, arrow, bolt);
            }

            default:
                return (ItemIds.Longsword, ItemIds.WoodenShield, StartingLoadout.Keep, StartingLoadout.Keep, StartingLoadout.Keep);
        }
    }

    private static int RollArmorPiece(int slotOffset, Random rng)
        => ArmorSetBases[rng.Next(ArmorSetBases.Length)] + slotOffset;

    private static int Pick(int[] pool, Random rng) => pool[rng.Next(pool.Length)];
}
