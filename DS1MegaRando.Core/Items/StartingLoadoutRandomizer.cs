using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Core.Items;

/// <summary>
/// A single character's starting weapon loadout. The slot values map directly
/// onto the CharaInitParam equip fields. <see cref="Empty"/> (-1) is the value
/// DS1 uses for an unequipped slot.
/// </summary>
public readonly record struct StartingLoadout(int RightHand, int LeftHand, int SubRightHand)
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

    /// <summary>
    /// Rolls an independent starting loadout for every player class. Each class in
    /// the character creator gets its own random gear instead of one shared roll
    /// being copied onto all of them.
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
        switch (mode)
        {
            case StartingLoadoutMode.ShieldAnd1H:
                return new StartingLoadout(
                    RightHand:    Pick(OneHandedWeapons, rng),
                    LeftHand:     Pick(Shields, rng),
                    SubRightHand: StartingLoadout.Empty);

            case StartingLoadoutMode.ShieldAnd1HAnd2H:
                // The 2H weapon goes in the right off-hand slot so the player
                // actually receives it (the old flat-list code dropped it entirely).
                return new StartingLoadout(
                    RightHand:    Pick(OneHandedWeapons, rng),
                    LeftHand:     Pick(Shields, rng),
                    SubRightHand: Pick(TwoHandedWeapons, rng));

            case StartingLoadoutMode.CombinedPool:
            {
                // Weighted toward 1H simply because that pool is larger.
                var combined = OneHandedWeapons.Concat(TwoHandedWeapons).ToArray();
                int weapon = combined[rng.Next(combined.Length)];
                int shield = rng.Next(2) == 0 ? Pick(Shields, rng) : StartingLoadout.Empty;
                return new StartingLoadout(weapon, shield, StartingLoadout.Empty);
            }

            default:
                return new StartingLoadout(ItemIds.Longsword, ItemIds.WoodenShield, StartingLoadout.Empty);
        }
    }

    private static int Pick(int[] pool, Random rng) => pool[rng.Next(pool.Length)];
}
