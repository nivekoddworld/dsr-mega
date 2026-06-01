using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Settings;
using DS1MegaRando.Data.Items;

namespace DS1MegaRando.Core.Items;

public class StartingLoadoutRandomizer
{
    private static readonly int[] OneHandedWeapons =
    {
        ItemIds.Longsword, ItemIds.Shortsword, ItemIds.BroadswordId, ItemIds.MailBreaker,
        ItemIds.Dagger, ItemIds.HandAxe, ItemIds.BattleAxe, ItemIds.Club, ItemIds.Mace,
        ItemIds.Reinforced_Club, ItemIds.Spear, ItemIds.Estoc, ItemIds.Falchion,
        ItemIds.Scimitar, ItemIds.Uchigatana, ItemIds.Whip, ItemIds.ClawWeapon,
    };

    private static readonly int[] TwoHandedWeapons =
    {
        ItemIds.BattleAxe, ItemIds.ButcherKnife, ItemIds.Club, ItemIds.Mace, ItemIds.Spear,
    };

    private static readonly int[] Shields =
    {
        ItemIds.WoodenShield, ItemIds.LargeLeatherShield, ItemIds.SmallLeatherShield,
        ItemIds.RoundShield, ItemIds.KiteSheild, ItemIds.HeaterShield, ItemIds.BucklerShield,
        ItemIds.GrassCrestShield, ItemIds.TowerKiteShield,
    };

    public List<int> Randomize(ItemSettings settings, GameData gameData, Random rng)
    {
        return settings.StartingLoadoutMode switch
        {
            StartingLoadoutMode.ShieldAnd1H => new List<int>
            {
                OneHandedWeapons[rng.Next(OneHandedWeapons.Length)],
                Shields[rng.Next(Shields.Length)],
            },
            StartingLoadoutMode.ShieldAnd1HAnd2H => new List<int>
            {
                OneHandedWeapons[rng.Next(OneHandedWeapons.Length)],
                TwoHandedWeapons[rng.Next(TwoHandedWeapons.Length)],
                Shields[rng.Next(Shields.Length)],
            },
            StartingLoadoutMode.CombinedPool => new List<int>
            {
                OneHandedWeapons.Concat(TwoHandedWeapons).ToArray()[
                    rng.Next(OneHandedWeapons.Length + TwoHandedWeapons.Length)],
                rng.Next(2) == 0 ? Shields[rng.Next(Shields.Length)] : 0,
            },
            _ => new List<int> { ItemIds.Longsword, ItemIds.WoodenShield },
        };
    }
}
