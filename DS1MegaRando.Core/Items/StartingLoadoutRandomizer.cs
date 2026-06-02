using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Core.Items;

/// <summary>
/// A single character's starting loadout. The slot values map directly onto the
/// CharaInitParam equip fields. <see cref="Empty"/> (-1) is the value DS1 uses for
/// an unequipped slot.
/// </summary>
public readonly record struct StartingLoadout(
    int RightHand, int LeftHand, int SubRightHand,
    int Helm, int Chest, int Gauntlets, int Legs)
{
    /// <summary>Sentinel for an empty equip slot (matches the CharaInitParam default).</summary>
    public const int Empty = -1;
}

public class StartingLoadoutRandomizer
{
    /// <summary>Number of player-selectable classes in the character creator (CharaInitParam rows 3000-3009).</summary>
    public const int PlayerClassCount = 10;

    private static readonly int[] OneHandedWeapons =
    {
        ItemIds.Dagger, ItemIds.BanditsKnife, ItemIds.Shortsword, ItemIds.Longsword,
        ItemIds.Broadsword, ItemIds.BalderSideSword, ItemIds.Scimitar, ItemIds.Falchion,
        ItemIds.Shotel, ItemIds.Uchigatana, ItemIds.Iaito, ItemIds.MailBreaker,
        ItemIds.Rapier, ItemIds.Estoc, ItemIds.HandAxe, ItemIds.BattleAxe, ItemIds.Club,
        ItemIds.Mace, ItemIds.MorningStar, ItemIds.ReinforcedClub, ItemIds.Caestus,
        ItemIds.Claw, ItemIds.Spear, ItemIds.WingedSpear, ItemIds.Whip,
    };

    private static readonly int[] TwoHandedWeapons =
    {
        ItemIds.BastardSword, ItemIds.Claymore, ItemIds.ManSerpentGreatsword,
        ItemIds.Flamberge, ItemIds.Zweihander, ItemIds.Greatsword, ItemIds.Murakumo,
        ItemIds.Greataxe, ItemIds.GreatClub, ItemIds.LargeClub, ItemIds.Pike,
        ItemIds.Halberd, ItemIds.Lucerne, ItemIds.Scythe, ItemIds.GreatScythe,
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
    /// Rolls an independent starting loadout (weapons + armor) for every player class.
    /// Each class in the character creator gets its own random gear instead of one
    /// shared roll being copied onto all ten rows.
    /// </summary>
    public List<StartingLoadout> RandomizePerClass(ItemSettings settings, Random rng)
    {
        var result = new List<StartingLoadout>(PlayerClassCount);
        for (int i = 0; i < PlayerClassCount; i++)
            result.Add(RollOne(settings.StartingLoadoutMode, rng));
        return result;
    }

    private static StartingLoadout RollOne(StartingLoadoutMode mode, Random rng)
    {
        var (rightHand, leftHand, subRightHand) = RollWeapons(mode, rng);
        return new StartingLoadout(
            rightHand, leftHand, subRightHand,
            Helm:      RollArmorPiece(ArmorHelmOffset,  rng),
            Chest:     RollArmorPiece(ArmorChestOffset, rng),
            Gauntlets: RollArmorPiece(ArmorGauntOffset, rng),
            Legs:      RollArmorPiece(ArmorLegOffset,   rng));
    }

    private static (int right, int left, int subRight) RollWeapons(StartingLoadoutMode mode, Random rng)
    {
        switch (mode)
        {
            case StartingLoadoutMode.ShieldAnd1H:
                return (Pick(OneHandedWeapons, rng), Pick(Shields, rng), StartingLoadout.Empty);

            case StartingLoadoutMode.ShieldAnd1HAnd2H:
                // The 2H weapon goes in the right off-hand slot so the player actually
                // receives it (the old flat-list code dropped it entirely).
                return (Pick(OneHandedWeapons, rng), Pick(Shields, rng), Pick(TwoHandedWeapons, rng));

            case StartingLoadoutMode.CombinedPool:
            {
                // Weighted toward 1H simply because that pool is larger.
                var combined = OneHandedWeapons.Concat(TwoHandedWeapons).ToArray();
                int weapon = combined[rng.Next(combined.Length)];
                int shield = rng.Next(2) == 0 ? Pick(Shields, rng) : StartingLoadout.Empty;
                return (weapon, shield, StartingLoadout.Empty);
            }

            default:
                return (ItemIds.Longsword, ItemIds.WoodenShield, StartingLoadout.Empty);
        }
    }

    private static int RollArmorPiece(int slotOffset, Random rng)
        => ArmorSetBases[rng.Next(ArmorSetBases.Length)] + slotOffset;

    private static int Pick(int[] pool, Random rng) => pool[rng.Next(pool.Length)];
}
