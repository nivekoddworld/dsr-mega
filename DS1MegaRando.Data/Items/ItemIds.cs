namespace DS1MegaRando.Data.Items;

/// <summary>
/// Common DS1R item IDs for weapons, armor, rings, and consumables.
/// These are EquipParamWeapon / item-lot row IDs (the values used by item lots and
/// the CharaInitParam equip slots) — NOT the WP_A_xxxx model IDs. A weapon's row ID
/// and its visual model ID differ (e.g. Uchigatana is row 500000 but model WP_A_0402).
/// Category offsets: Weapons 100000–1601000 (daggers 1xxxxx, straight swords 2xxxxx,
/// greatswords 3xxxxx, curved swords 4xxxxx, katanas 5xxxxx, thrusting 6xxxxx,
/// axes 7xxxxx, hammers 8xxxxx, fists 9xxxxx, spears 10xxxxx, halberds 11xxxxx,
/// whips 16xxxxx), Shields 1400000–1510000, Armor 1200000+, Rings 2000000+,
/// Items/Consumables 3000000+, Keys 2000–2999.
/// </summary>
public static class ItemIds
{
    // ── One-handable weapons (valid right-hand equip row IDs) ──────────────
    public const int Dagger           = 100000;
    public const int BanditsKnife     = 103000;
    public const int ParryingDagger   = 104000;
    public const int Shortsword       = 200000;
    public const int Longsword        = 201000;
    public const int Broadsword       = 202000;
    public const int BalderSideSword  = 204000;
    public const int Scimitar         = 400000;
    public const int Falchion         = 401000;
    public const int Shotel           = 402000;
    public const int PaintingGuardianSword = 450000;
    public const int Uchigatana       = 500000;
    public const int Iaito            = 502000;
    public const int WashingPole      = 504000;
    public const int MailBreaker      = 600000;
    public const int Rapier           = 601000;
    public const int Estoc            = 602000;
    public const int HandAxe          = 700000;
    public const int BattleAxe        = 701000;
    public const int Club             = 800000;
    public const int Mace             = 801000;
    public const int MorningStar      = 802000;
    public const int Pickaxe          = 803000;
    public const int ReinforcedClub   = 809000;
    public const int Caestus          = 901000;
    public const int Claw             = 902000;
    public const int Spear            = 1000000;
    public const int WingedSpear      = 1001000;
    public const int Partizan         = 1003000;
    public const int Whip             = 1600000;

    // ── Heavier / typically two-handed weapons ────────────────────────────
    public const int BastardSword         = 300000;
    public const int Claymore             = 301000;
    public const int ManSerpentGreatsword = 302000;
    public const int Flamberge            = 303000;
    public const int Zweihander           = 350000;
    public const int Greatsword           = 351000;
    public const int StoneGreatsword      = 352000;
    public const int Murakumo             = 451000;
    public const int Greataxe             = 750000;
    public const int DemonsGreataxe       = 752000;
    public const int GreatClub            = 850000;
    public const int LargeClub            = 855000;
    public const int SmoughsHammer        = 858000;
    public const int Pike                 = 1050000;
    public const int Halberd              = 1100000;
    public const int Lucerne              = 1106000;
    public const int Scythe               = 1107000;
    public const int GreatScythe          = 1150000;
    public const int Twinblade            = 1200000;
    public const int StoneTwinblade       = 1201000;

    // ── Catalysts / flames ────────────────────────────────────────────────
    public const int Pyromancy_Flame  = 1330000;

    // ── Shields (valid left-hand equip row IDs) ───────────────────────────
    public const int EastWestShield      = 1400000;
    public const int WoodenShield        = 1401000;
    public const int LargeLeatherShield  = 1402000;
    public const int SmallLeatherShield  = 1403000;
    public const int TargetShield        = 1404000;
    public const int BucklerShield       = 1405000;
    public const int CrackedRoundShield  = 1406000;
    public const int LeatherShield       = 1408000;
    public const int PlankShield         = 1409000;
    public const int HeaterShield        = 1450000;
    public const int KnightShield        = 1451000;
    public const int TowerKiteShield     = 1452000;
    public const int GrassCrestShield    = 1453000;
    public const int HollowSoldierShield = 1454000;
    public const int SpiderShield        = 1462000;
    public const int SpikedShield        = 1470000;
    public const int EagleShield         = 1500000;
    public const int TowerShield         = 1501000;

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
