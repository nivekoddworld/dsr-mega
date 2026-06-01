namespace DS1MegaRando.Data.Items;

/// <summary>
/// Common DS1R item IDs for weapons, armor, rings, and consumables.
/// Category offsets: Weapons 0–999999, Shields 1000000–1199999,
/// Armor 1200000+, Rings 2000000+, Items/Consumables 3000000+, Keys 2000–2999.
/// </summary>
public static class ItemIds
{
    // ── Starting weapons (weapon category < 1000000) ──────────────────────
    public const int Longsword        = 201000;
    public const int Shortsword       = 202000;
    public const int BroadswordId     = 100000;
    public const int MailBreaker      = 103000;
    public const int StraightSwordHilt= 108000;
    public const int Dagger           = 200000;
    public const int GhostBlade       = 203000;
    public const int HandAxe          = 300000;
    public const int ButcherKnife     = 302000;
    public const int ClawWeapon       = 303000;
    public const int Whip             = 800000;
    public const int Spear            = 700000;
    public const int BattleAxe        = 301000;
    public const int Club             = 500000;
    public const int Mace             = 501000;
    public const int Reinforced_Club  = 502000;
    public const int Estoc            = 105000;
    public const int Falchion         = 400000;
    public const int Scimitar         = 401000;
    public const int Uchigatana       = 451000;
    public const int Pyromancy_Flame  = 1300000;

    // ── Starting shields ──────────────────────────────────────────────────
    public const int WoodenShield     = 1000000;
    public const int LargeLeatherShield = 1001000;
    public const int SmallLeatherShield = 1002000;
    public const int RoundShield      = 1003000;
    public const int KiteSheild       = 1010000;
    public const int HeaterShield     = 1011000;
    public const int SpikedShield     = 1012000;
    public const int EagleShield      = 1020000;
    public const int TowerKiteShield  = 1450000;
    public const int BucklerShield    = 1005000;
    public const int CrystalRingShield= 1060000;
    public const int GrassCrestShield = 1030000;

    // ── Upgrade materials (consumable range 3000000+) ─────────────────────
    public const int TitaniteShard    = 3000000;
    public const int LargeTitaniteShard = 3000100;
    public const int ChunkTitanite    = 3000200;
    public const int TitaniteSlab     = 3000300;
    public const int SmoothSilkyStone = 3000600;
    public const int TwininklingTitanite = 3000800;
    public const int DemonTitanite    = 3000900;
    public const int DragonScale      = 3001000;

    // ── Estus flask / misc ────────────────────────────────────────────────
    public const int EstusFlask       = 3000020;
    public const int FirebombItem     = 3010000;
    public const int BlackFirebomb    = 3010100;
    public const int LloydsTalisman   = 3040000;
    public const int RepairBox        = 3001100;

    // ── Rings ─────────────────────────────────────────────────────────────
    public const int HavelRing        = 20000;
    public const int RingOfFavor      = 20030;
    public const int DarkwoodGrainRing = 20010;
    public const int CovetousGoldSerpentRing = 20060;
    public const int CovetousSilverSerpentRing = 20050;

    // ── Boss souls ────────────────────────────────────────────────────────
    public const int SoulOfSif        = 3010200;
    public const int SoulOfGwyn       = 3010300;
    public const int CoreOfIronGolem  = 3020000;
    public const int SoulOfQuelaag    = 3020100;
    public const int SoulOfOrnstein   = 3020200;
    public const int SoulOfSmough     = 3020300;
    public const int SoulOfNito       = 3020400;
    public const int SoulOfBedOfChaos = 3020500;
    public const int SoulOfFourKings  = 3020600;
    public const int SoulOfSeath      = 3020700;
    public const int SoulOfArtorias   = 3020800;
    public const int SoulOfManus      = 3020900;

    // Items considered "useless" for exclude-useless-items setting
    public static readonly IReadOnlySet<int> UselessItems = new HashSet<int>
    {
        3050000, // Dried Fingers
        3050100, // Cracked Red Eye Orb
        3050200, // Dragon Eye (not truly useless but rarely needed)
    };
}
