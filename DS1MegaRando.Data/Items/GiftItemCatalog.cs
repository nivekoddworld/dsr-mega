namespace DS1MegaRando.Data.Items;

/// <summary>
/// Full catalog of giftable DS1R items (every real weapon, shield, armor piece,
/// ring, spell, and consumable good), excluding: upgrade variants, ammunition,
/// the Estus Flask, character-appearance/hair entries, developer/debug/cut items,
/// and progression-critical keys / Lord Souls / Lordvessel / Crest of Artorias /
/// Broken Pendant (so a random gift can never break key-item logic).
/// Generated from the authoritative DarkSoulsItemRandomizer item-name table.
///
/// Category is the raw ItemLotParam lotItemCategory value:
///   weapon = 0x00000000, armor = 0x10000000, ring = 0x20000000, goods = 0x40000000.
/// </summary>
public static class GiftItemCatalog
{
    public static readonly (int Id, int Category)[] Items =
    {
        // ── Weapons & shields (198) ──
        (100000, 0x00000000),  // Dagger
        (101000, 0x00000000),  // Parrying Dagger
        (102000, 0x00000000),  // Ghost Blade
        (103000, 0x00000000),  // Bandit's Knife
        (104000, 0x00000000),  // Priscilla's Dagger
        (200000, 0x00000000),  // Shortsword
        (201000, 0x00000000),  // Longsword
        (202000, 0x00000000),  // Broadsword
        (203000, 0x00000000),  // Broken Straight Sword
        (204000, 0x00000000),  // Balder Side Sword
        (205000, 0x00000000),  // Crystal Straight Sword
        (206000, 0x00000000),  // Sunlight Straight Sword
        (207000, 0x00000000),  // Barbed Straight Sword
        (208000, 0x00000000),  // Silv. Knight Str. Sword
        (209000, 0x00000000),  // Astora's Straight Sword
        (210000, 0x00000000),  // Darksword
        (211000, 0x00000000),  // Drake Sword
        (212000, 0x00000000),  // Straight Sword Hilt
        (300000, 0x00000000),  // Bastard Sword
        (301000, 0x00000000),  // Claymore
        (302000, 0x00000000),  // Man-serpent Greatsword
        (303000, 0x00000000),  // Flamberge
        (304000, 0x00000000),  // Crystal Greatsword
        (306000, 0x00000000),  // Stone Greatsword
        (307000, 0x00000000),  // Greatsword of Artorias
        (308000, 0x00000000),  // Great Lord Greatsword
        (309000, 0x00000000),  // Moonlight Greatsword
        (310000, 0x00000000),  // Black Knight Sword
        (311000, 0x00000000),  // Greatsword of Artorias
        (312000, 0x00000000),  // Greatsword of Artorias
        (314000, 0x00000000),  // Great Lord Greatsword
        (315000, 0x00000000),  // Great Lord Greatsword
        (350000, 0x00000000),  // Zweihander
        (351000, 0x00000000),  // Greatsword
        (352000, 0x00000000),  // Demon Great Machete
        (354000, 0x00000000),  // Dragon Greatsword
        (355000, 0x00000000),  // Black Knight Greatsword
        (400000, 0x00000000),  // Scimitar
        (401000, 0x00000000),  // Falchion
        (402000, 0x00000000),  // Shotel
        (403000, 0x00000000),  // Jagged Ghost Blade
        (405000, 0x00000000),  // Painting Guardian Sword
        (406000, 0x00000000),  // Quelaag's Furysword
        (450000, 0x00000000),  // Server
        (451000, 0x00000000),  // Murakumo
        (453000, 0x00000000),  // Gravelord Sword
        (500000, 0x00000000),  // Uchigatana
        (501000, 0x00000000),  // Washing Pole
        (502000, 0x00000000),  // Iaito
        (503000, 0x00000000),  // Chaos Blade
        (600000, 0x00000000),  // Mail Breaker
        (601000, 0x00000000),  // Rapier
        (602000, 0x00000000),  // Estoc
        (603000, 0x00000000),  // Velka's Rapier
        (604000, 0x00000000),  // Ricard's Rapier
        (700000, 0x00000000),  // Hand Axe
        (701000, 0x00000000),  // Battle Axe
        (702000, 0x00000000),  // Crescent Axe
        (703000, 0x00000000),  // Butcher Knife
        (704000, 0x00000000),  // Golem Axe
        (705000, 0x00000000),  // Gargoyle Tail Axe
        (750000, 0x00000000),  // Greataxe
        (751000, 0x00000000),  // Demon's Greataxe
        (752000, 0x00000000),  // Dragon King Greataxe
        (753000, 0x00000000),  // Black Knight Greataxe
        (800000, 0x00000000),  // Club
        (801000, 0x00000000),  // Mace
        (802000, 0x00000000),  // Morning Star
        (803000, 0x00000000),  // Warpick
        (804000, 0x00000000),  // Pickaxe
        (809000, 0x00000000),  // Reinforced Club
        (810000, 0x00000000),  // Blacksmith Hammer
        (811000, 0x00000000),  // Blacksmith Giant Hammer
        (812000, 0x00000000),  // Hammer of Vamos
        (850000, 0x00000000),  // Great Club
        (851000, 0x00000000),  // Grant
        (852000, 0x00000000),  // Demon's Great Hammer
        (854000, 0x00000000),  // Dragon Tooth
        (855000, 0x00000000),  // Large Club
        (856000, 0x00000000),  // Smough's Hammer
        (857000, 0x00000000),  // Smough's Hammer
        (900000, 0x00000000),  // Fists
        (901000, 0x00000000),  // Caestus
        (902000, 0x00000000),  // Claw
        (903000, 0x00000000),  // Dragon Bone Fist
        (904000, 0x00000000),  // Dark Hand
        (1000000, 0x00000000),  // Spear
        (1001000, 0x00000000),  // Winged Spear
        (1002000, 0x00000000),  // Partizan
        (1003000, 0x00000000),  // Demon's Spear
        (1004000, 0x00000000),  // Channeler's Trident
        (1006000, 0x00000000),  // Silver Knight Spear
        (1050000, 0x00000000),  // Pike
        (1051000, 0x00000000),  // Dragonslayer Spear
        (1052000, 0x00000000),  // Moonlight Butterfly Horn
        (1100000, 0x00000000),  // Halberd
        (1101000, 0x00000000),  // Giant's Halberd
        (1102000, 0x00000000),  // Titanite Catch Pole
        (1103000, 0x00000000),  // Gargoyle's Halberd
        (1105000, 0x00000000),  // Black Knight Halberd
        (1106000, 0x00000000),  // Lucerne
        (1107000, 0x00000000),  // Scythe
        (1150000, 0x00000000),  // Great Scythe
        (1151000, 0x00000000),  // Lifehunt Scythe
        (1200000, 0x00000000),  // Short Bow
        (1201000, 0x00000000),  // Longbow
        (1202000, 0x00000000),  // Black Bow of Pharis
        (1203000, 0x00000000),  // Dragonslayer Greatbow
        (1204000, 0x00000000),  // Composite Bow
        (1205000, 0x00000000),  // Darkmoon Bow
        (1250000, 0x00000000),  // Light Crossbow
        (1251000, 0x00000000),  // Heavy Crossbow
        (1252000, 0x00000000),  // Avelyn
        (1253000, 0x00000000),  // Sniper Crossbow
        (1300000, 0x00000000),  // Sorcerer's Catalyst
        (1301000, 0x00000000),  // Beatrice's Catalyst
        (1302000, 0x00000000),  // Tin Banishment Catalyst
        (1303000, 0x00000000),  // Logan's Catalyst
        (1304000, 0x00000000),  // Tin Darkmoon Catalyst
        (1305000, 0x00000000),  // Oolacile Ivory Catalyst
        (1306000, 0x00000000),  // Tin Crystallization Ctlyst.
        (1307000, 0x00000000),  // Demon's Catalyst
        (1308000, 0x00000000),  // Izalith Catalyst
        (1330000, 0x00000000),  // Pyromancy Flame
        (1332000, 0x00000000),  // Pyromancy Flame
        (1360000, 0x00000000),  // Talisman
        (1361000, 0x00000000),  // Canvas Talisman
        (1362000, 0x00000000),  // Thorolund Talisman
        (1363000, 0x00000000),  // Ivory Talisman
        (1364000, 0x00000000),  // Gwynevere's Talisman
        (1365000, 0x00000000),  // Sunlight Talisman
        (1366000, 0x00000000),  // Darkmoon Talisman
        (1367000, 0x00000000),  // Velka's Talisman
        (1396000, 0x00000000),  // Skull Lantern
        (1400000, 0x00000000),  // East-West Shield
        (1401000, 0x00000000),  // Wooden Shield
        (1402000, 0x00000000),  // Large Leather Shield
        (1403000, 0x00000000),  // Small Leather Shield
        (1404000, 0x00000000),  // Target Shield
        (1405000, 0x00000000),  // Buckler
        (1406000, 0x00000000),  // Cracked Round Shield
        (1408000, 0x00000000),  // Leather Shield
        (1409000, 0x00000000),  // Plank Shield
        (1410000, 0x00000000),  // Caduceus Round Shield
        (1411000, 0x00000000),  // Crystal Ring Shield
        (1412000, 0x00000000),  // Crystal Ring Shield
        (1413000, 0x00000000),  // Crystal Ring Shield
        (1414000, 0x00000000),  // Crystal Ring Shield
        (1450000, 0x00000000),  // Heater Shield
        (1451000, 0x00000000),  // Knight Shield
        (1452000, 0x00000000),  // Tower Kite Shield
        (1453000, 0x00000000),  // Grass Crest Shield
        (1454000, 0x00000000),  // Hollow Soldier Shield
        (1455000, 0x00000000),  // Balder Shield
        (1456000, 0x00000000),  // Crest Shield
        (1457000, 0x00000000),  // Dragon Crest Shield
        (1460000, 0x00000000),  // Warrior's Round Shield
        (1461000, 0x00000000),  // Iron Round Shield
        (1462000, 0x00000000),  // Spider Shield
        (1470000, 0x00000000),  // Spiked Shield
        (1471000, 0x00000000),  // Crystal Shield
        (1472000, 0x00000000),  // Sunlight Shield
        (1473000, 0x00000000),  // Silver Knight Shield
        (1474000, 0x00000000),  // Black Knight Shield
        (1475000, 0x00000000),  // Pierce Shield
        (1476000, 0x00000000),  // Red and White Round Shield
        (1477000, 0x00000000),  // Caduceus Kite Shield
        (1478000, 0x00000000),  // Gargoyle's Shield
        (1500000, 0x00000000),  // Eagle Shield
        (1501000, 0x00000000),  // Tower Shield
        (1502000, 0x00000000),  // Giant Shield
        (1503000, 0x00000000),  // Stone Greatshield
        (1505000, 0x00000000),  // Havel's Greatshield
        (1506000, 0x00000000),  // Bonewheel Shield
        (1507000, 0x00000000),  // Greatshield of Artorias
        (1508000, 0x00000000),  // Greatshield of Artorias
        (1509000, 0x00000000),  // Greatshield of Artorias
        (1510000, 0x00000000),  // Greatshield of Artorias
        (1600000, 0x00000000),  // Whip
        (1601000, 0x00000000),  // Notched Whip
        (9000000, 0x00000000),  // Effigy Shield
        (9001000, 0x00000000),  // Sanctus
        (9002000, 0x00000000),  // Bloodshield
        (9003000, 0x00000000),  // Black Iron Greatshield
        (1053000, 0x00000000),  // Moonlight Butterfly Horn
        (1054000, 0x00000000),  // Dragonslayer Spear
        (9010000, 0x00000000),  // Gold Tracer
        (9011000, 0x00000000),  // Dark Silver Tracer
        (9012000, 0x00000000),  // Abyss Greatsword
        (9013000, 0x00000000),  // Abyss Greatsword
        (9014000, 0x00000000),  // Cleansing Greatshield
        (9015000, 0x00000000),  // Stone Greataxe
        (9016000, 0x00000000),  // Four-pronged Plow
        (9017000, 0x00000000),  // Manus Catalyst
        (9018000, 0x00000000),  // Oolacile Catalyst
        (9019000, 0x00000000),  // Guardian Tail
        (9020000, 0x00000000),  // Obsidian Greatsword
        (9021000, 0x00000000),  // Gough's Greatbow
        // ── Armor (247) ──
        (10000, 0x10000000),  // Catarina Helm
        (11000, 0x10000000),  // Catarina Armor
        (12000, 0x10000000),  // Catarina Gauntlets
        (13000, 0x10000000),  // Catarina Leggings
        (20000, 0x10000000),  // Paladin Helm
        (21000, 0x10000000),  // Paladin Armor
        (22000, 0x10000000),  // Paladin Gauntlets
        (23000, 0x10000000),  // Paladin Leggings
        (40000, 0x10000000),  // Dark Mask
        (41000, 0x10000000),  // Dark Armor
        (42000, 0x10000000),  // Dark Gauntlets
        (43000, 0x10000000),  // Dark Leggings
        (50000, 0x10000000),  // Brigand Hood
        (51000, 0x10000000),  // Brigand Armor
        (52000, 0x10000000),  // Brigand Gauntlets
        (53000, 0x10000000),  // Brigand Trousers
        (60000, 0x10000000),  // Shadow Mask
        (61000, 0x10000000),  // Shadow Garb
        (62000, 0x10000000),  // Shadow Gauntlets
        (63000, 0x10000000),  // Shadow Leggings
        (70000, 0x10000000),  // Black Iron Helm
        (71000, 0x10000000),  // Black Iron Armor
        (72000, 0x10000000),  // Black Iron Gauntlets
        (73000, 0x10000000),  // Black Iron Leggings
        (80000, 0x10000000),  // Smough's Helm
        (81000, 0x10000000),  // Smough's Armor
        (82000, 0x10000000),  // Smough's Gauntlets
        (83000, 0x10000000),  // Smough's Leggings
        (90000, 0x10000000),  // Six-Eyed Helm of the Channelers
        (91000, 0x10000000),  // Robe of the Channelers
        (92000, 0x10000000),  // Gauntlets of the Channelers
        (93000, 0x10000000),  // Waistcloth of the Channelers
        (100000, 0x10000000),  // Helm of Favor
        (101000, 0x10000000),  // Embraced Armor of Favor
        (102000, 0x10000000),  // Gauntlets of Favor
        (103000, 0x10000000),  // Leggings of Favor
        (110000, 0x10000000),  // Helm of the Wise
        (111000, 0x10000000),  // Armor of the Glorious
        (112000, 0x10000000),  // Gauntlets of the Vanquisher
        (113000, 0x10000000),  // Boots of the Explorer
        (120000, 0x10000000),  // Stone Helm
        (121000, 0x10000000),  // Stone Armor
        (122000, 0x10000000),  // Stone Gauntlets
        (123000, 0x10000000),  // Stone Leggings
        (130000, 0x10000000),  // Crystalline Helm
        (131000, 0x10000000),  // Crystalline Armor
        (132000, 0x10000000),  // Crystalline Gauntlets
        (133000, 0x10000000),  // Crystalline Leggings
        (140000, 0x10000000),  // Mask of the Sealer
        (141000, 0x10000000),  // Crimson Robe
        (142000, 0x10000000),  // Crimson Gloves
        (143000, 0x10000000),  // Crimson Waistcloth
        (150000, 0x10000000),  // Mask of Velka
        (151000, 0x10000000),  // Black Cleric Robe
        (152000, 0x10000000),  // Black Manchette
        (153000, 0x10000000),  // Black Tights
        (160000, 0x10000000),  // Iron Helm
        (161000, 0x10000000),  // Armor of the Sun
        (162000, 0x10000000),  // Iron Bracelet
        (163000, 0x10000000),  // Iron Leggings
        (170000, 0x10000000),  // Chain Helm
        (171000, 0x10000000),  // Chain Armor
        (172000, 0x10000000),  // Leather Gauntlets
        (173000, 0x10000000),  // Chain Leggings
        (180000, 0x10000000),  // Cleric Helm
        (181000, 0x10000000),  // Cleric Armor
        (182000, 0x10000000),  // Cleric Gauntlets
        (183000, 0x10000000),  // Cleric Leggings
        (190000, 0x10000000),  // Sunlight Maggot
        (200000, 0x10000000),  // Helm of Thorns
        (201000, 0x10000000),  // Armor of Thorns
        (202000, 0x10000000),  // Gauntlets of Thorns
        (203000, 0x10000000),  // Leggings of Thorns
        (210000, 0x10000000),  // Standard Helm
        (211000, 0x10000000),  // Hard Leather Armor
        (212000, 0x10000000),  // Hard Leather Gauntlets
        (213000, 0x10000000),  // Hard Leather Boots
        (220000, 0x10000000),  // Sorcerer Hat
        (221000, 0x10000000),  // Sorcerer Cloak
        (222000, 0x10000000),  // Sorcerer Gauntlets
        (223000, 0x10000000),  // Sorcerer Boots
        (230000, 0x10000000),  // Tattered Cloth Hood
        (231000, 0x10000000),  // Tattered Cloth Robe
        (232000, 0x10000000),  // Tattered Cloth Manchette
        (233000, 0x10000000),  // Heavy Boots
        (240000, 0x10000000),  // Pharis's Hat
        (241000, 0x10000000),  // Leather Armor
        (242000, 0x10000000),  // Leather Gloves
        (243000, 0x10000000),  // Leather Boots
        (250000, 0x10000000),  // Painting Guardian Hood
        (251000, 0x10000000),  // Painting Guardian Robe
        (252000, 0x10000000),  // Painting Guardian Gloves
        (253000, 0x10000000),  // Painting Guardian Waistcloth
        (270000, 0x10000000),  // Ornstein's Helm
        (271000, 0x10000000),  // Ornstein's Armor
        (272000, 0x10000000),  // Ornstein's Gauntlets
        (273000, 0x10000000),  // Ornstein's Leggings
        (280000, 0x10000000),  // Eastern Helm
        (281000, 0x10000000),  // Eastern Armor
        (282000, 0x10000000),  // Eastern Gauntlets
        (283000, 0x10000000),  // Eastern Leggings
        (290000, 0x10000000),  // Xanthous Crown
        (291000, 0x10000000),  // Xanthous Overcoat
        (292000, 0x10000000),  // Xanthous Gloves
        (293000, 0x10000000),  // Xanthous Waistcloth
        (300000, 0x10000000),  // Thief Mask
        (301000, 0x10000000),  // Black Leather Armor
        (302000, 0x10000000),  // Black Leather Gloves
        (303000, 0x10000000),  // Black Leather Boots
        (310000, 0x10000000),  // Priest's Hat
        (311000, 0x10000000),  // Holy Robe
        (312000, 0x10000000),  // Traveling Gloves
        (313000, 0x10000000),  // Holy Trousers
        (320000, 0x10000000),  // Black Knight Helm
        (321000, 0x10000000),  // Black Knight Armor
        (322000, 0x10000000),  // Black Knight Gauntlets
        (323000, 0x10000000),  // Black Knight Leggings
        (330000, 0x10000000),  // Crown of Dusk
        (331000, 0x10000000),  // Antiquated Dress
        (332000, 0x10000000),  // Antiquated Gloves
        (333000, 0x10000000),  // Antiquated Skirt
        (340000, 0x10000000),  // Witch Hat
        (341000, 0x10000000),  // Witch Cloak
        (342000, 0x10000000),  // Witch Gloves
        (343000, 0x10000000),  // Witch Skirt
        (350000, 0x10000000),  // Elite Knight Helm
        (351000, 0x10000000),  // Elite Knight Armor
        (352000, 0x10000000),  // Elite Knight Gauntlets
        (353000, 0x10000000),  // Elite Knight Leggings
        (360000, 0x10000000),  // Wanderer Hood
        (361000, 0x10000000),  // Wanderer Coat
        (362000, 0x10000000),  // Wanderer Manchette
        (363000, 0x10000000),  // Wanderer Boots
        (370000, 0x10000000),  // Mage Smith Hat
        (371000, 0x10000000),  // Mage Smith Coat
        (372000, 0x10000000),  // Mage Smith Gauntlets
        (373000, 0x10000000),  // Mage Smith Boots
        (380000, 0x10000000),  // Big Hat
        (381000, 0x10000000),  // Sage Robe
        (382000, 0x10000000),  // Traveling Gloves
        (383000, 0x10000000),  // Traveling Boots
        (390000, 0x10000000),  // Knight Helm
        (391000, 0x10000000),  // Knight Armor
        (392000, 0x10000000),  // Knight Gauntlets
        (393000, 0x10000000),  // Knight Leggings
        (400000, 0x10000000),  // Dingy Hood
        (401000, 0x10000000),  // Dingy Robe
        (402000, 0x10000000),  // Dingy Gloves
        (403000, 0x10000000),  // Blood-Stained Skirt
        (410000, 0x10000000),  // Maiden Hood
        (411000, 0x10000000),  // Maiden Robe
        (412000, 0x10000000),  // Maiden Gloves
        (413000, 0x10000000),  // Maiden Skirt
        (420000, 0x10000000),  // Silver Knight Helm
        (421000, 0x10000000),  // Silver Knight Armor
        (422000, 0x10000000),  // Silver Knight Gauntlets
        (423000, 0x10000000),  // Silver Knight Leggings
        (440000, 0x10000000),  // Havel's Helm
        (441000, 0x10000000),  // Havel's Armor
        (442000, 0x10000000),  // Havel's Gauntlets
        (443000, 0x10000000),  // Havel's Leggings
        (450000, 0x10000000),  // Brass Helm
        (451000, 0x10000000),  // Brass Armor
        (452000, 0x10000000),  // Brass Gauntlets
        (453000, 0x10000000),  // Brass Leggings
        (460000, 0x10000000),  // Gold-Hemmed Black Hood
        (461000, 0x10000000),  // Gold-Hemmed Black Cloak
        (462000, 0x10000000),  // Gold-Hemmed Black Gloves
        (463000, 0x10000000),  // Gold-Hemmed Black Skirt
        (470000, 0x10000000),  // Golem Helm
        (471000, 0x10000000),  // Golem Armor
        (472000, 0x10000000),  // Golem Gauntlets
        (473000, 0x10000000),  // Golem Leggings
        (480000, 0x10000000),  // Hollow Soldier Helm
        (481000, 0x10000000),  // Hollow Soldier Armor
        (482000, 0x10000000),  // Soldier's Gloves
        (483000, 0x10000000),  // Hollow Soldier Waistcloth
        (490000, 0x10000000),  // Steel Helm
        (491000, 0x10000000),  // Steel Armor
        (492000, 0x10000000),  // Steel Gauntlets
        (493000, 0x10000000),  // Steel Leggings
        (500000, 0x10000000),  // Hollow Thief's Hood
        (501000, 0x10000000),  // Hollow Thief's Leather Armor
        (502000, 0x10000000),  // Thief's Gloves
        (503000, 0x10000000),  // Hollow Thief's Tights
        (510000, 0x10000000),  // Balder Helm
        (511000, 0x10000000),  // Balder Armor
        (512000, 0x10000000),  // Balder Gauntlets
        (513000, 0x10000000),  // Balder Leggings
        (520000, 0x10000000),  // Hollow Warrior Helm
        (521000, 0x10000000),  // Hollow Warrior Armor
        (522000, 0x10000000),  // Warrior's Gloves
        (523000, 0x10000000),  // Hollow Warrior Waistcloth
        (530000, 0x10000000),  // Giant Helm
        (531000, 0x10000000),  // Giant Armor
        (532000, 0x10000000),  // Giant Gauntlets
        (533000, 0x10000000),  // Giant Leggings
        (540000, 0x10000000),  // Crown of the Dark Sun
        (541000, 0x10000000),  // Moonlight Robe
        (542000, 0x10000000),  // Moonlight Gloves
        (543000, 0x10000000),  // Moonlight Waistcloth
        (550000, 0x10000000),  // Crown of the Great Lord
        (551000, 0x10000000),  // Robe of the Great Lord
        (552000, 0x10000000),  // Bracelet of the Great Lord
        (553000, 0x10000000),  // Anklet of the Great Lord
        (560000, 0x10000000),  // Sack
        (570000, 0x10000000),  // Symbol of Avarice
        (580000, 0x10000000),  // Royal Helm
        (590000, 0x10000000),  // Mask of the Father
        (600000, 0x10000000),  // Mask of the Mother
        (610000, 0x10000000),  // Mask of the Child
        (620000, 0x10000000),  // Fang Boar Helm
        (630000, 0x10000000),  // Gargoyle Helm
        (640000, 0x10000000),  // Black Sorcerer Hat
        (641000, 0x10000000),  // Black Sorcerer Cloak
        (642000, 0x10000000),  // Black Sorcerer Gauntlets
        (643000, 0x10000000),  // Black Sorcerer Boots
        (650000, 0x10000000),  // Elite Cleric Helm
        (651000, 0x10000000),  // Elite Cleric Armor
        (652000, 0x10000000),  // Elite Cleric Gauntlets
        (653000, 0x10000000),  // Elite Cleric Leggings
        (660000, 0x10000000),  // Helm of Artorias
        (661000, 0x10000000),  // Armor of Artorias
        (662000, 0x10000000),  // Gauntlets of Artorias
        (663000, 0x10000000),  // Leggings of Artorias
        (670000, 0x10000000),  // Porcelain Mask
        (671000, 0x10000000),  // Lord's Blade Robe
        (672000, 0x10000000),  // Lord's Blade Gloves
        (673000, 0x10000000),  // Lord's Blade Waistcloth
        (680000, 0x10000000),  // Gough's Helm
        (681000, 0x10000000),  // Gough's Armor
        (682000, 0x10000000),  // Gough's Gauntlets
        (683000, 0x10000000),  // Gough's Leggings
        (690000, 0x10000000),  // Guardian Helm
        (691000, 0x10000000),  // Guardian Armor
        (692000, 0x10000000),  // Guardian Gauntlets
        (693000, 0x10000000),  // Guardian Leggings
        (700000, 0x10000000),  // Snickering Top Hat
        (701000, 0x10000000),  // Chester's Long Coat
        (702000, 0x10000000),  // Chester's Gloves
        (703000, 0x10000000),  // Chester's Trousers
        (710000, 0x10000000),  // Bloated Head
        (720000, 0x10000000),  // Bloated Sorcerer Head
        (900000, 0x10000000),  // Head
        (901000, 0x10000000),  // Body
        (902000, 0x10000000),  // Arms
        (903000, 0x10000000),  // Legs
        // ── Rings (44) ──
        (100, 0x20000000),  // Havel's Ring
        (101, 0x20000000),  // Red Tearstone Ring
        (102, 0x20000000),  // Darkmoon Blade Covenant Ring
        (103, 0x20000000),  // Cat Covenant Ring
        (104, 0x20000000),  // Cloranthy Ring
        (105, 0x20000000),  // Flame Stoneplate Ring
        (106, 0x20000000),  // Thunder Stoneplate Ring
        (107, 0x20000000),  // Spell Stoneplate Ring
        (108, 0x20000000),  // Speckled Stoneplate Ring
        (109, 0x20000000),  // Bloodbite Ring
        (110, 0x20000000),  // Poisonbite Ring
        (111, 0x20000000),  // Tiny Being's Ring
        (113, 0x20000000),  // Cursebite Ring
        (114, 0x20000000),  // White Seance Ring
        (115, 0x20000000),  // Bellowing Dragoncrest Ring
        (116, 0x20000000),  // Dusk Crown Ring
        (117, 0x20000000),  // Hornet Ring
        (119, 0x20000000),  // Hawk Ring
        (120, 0x20000000),  // Ring of Steel Protection
        (121, 0x20000000),  // Covetous Gold Serpent Ring
        (122, 0x20000000),  // Covetous Silver Serpent Ring
        (123, 0x20000000),  // Slumbering Dragoncrest Ring
        (124, 0x20000000),  // Ring of Fog
        (125, 0x20000000),  // Rusted Iron Ring
        (126, 0x20000000),  // Ring of Sacrifice
        (127, 0x20000000),  // Rare Ring of Sacrifice
        (128, 0x20000000),  // Dark Wood Grain Ring
        (130, 0x20000000),  // Ring of the Sun Princess
        (133, 0x20000000),  // Ring of Condemnation
        (134, 0x20000000),  // Ring of Blind Ghosts
        (137, 0x20000000),  // Old Witch's Ring
        (138, 0x20000000),  // Covenant of Artorias
        (139, 0x20000000),  // Orange Charred Ring
        (140, 0x20000000),  // Ring of Displacement
        (141, 0x20000000),  // Lingering Dragoncrest Ring
        (142, 0x20000000),  // Ring of the Evil Eye
        (143, 0x20000000),  // Ring of Favor and Protection
        (144, 0x20000000),  // Leo Ring
        (145, 0x20000000),  // East Wood Grain Ring
        (146, 0x20000000),  // Wolf Ring
        (147, 0x20000000),  // Blue Tearstone Ring
        (148, 0x20000000),  // Ring of the Sun's Firstborn
        (149, 0x20000000),  // Darkmoon Seance Ring
        (150, 0x20000000),  // Calamity Ring
        // ── Goods, spells & consumables (205) ──
        (100, 0x40000000),  // White Sign Soapstone
        (101, 0x40000000),  // Red Sign Soapstone
        (102, 0x40000000),  // Red Eye Orb
        (103, 0x40000000),  // Black Separation Crystal
        (106, 0x40000000),  // Orange Guidance Soapstone
        (108, 0x40000000),  // Book of the Guilty
        (109, 0x40000000),  // Eye of Death
        (111, 0x40000000),  // Cracked Red Eye Orb
        (112, 0x40000000),  // Servant Roster
        (113, 0x40000000),  // Blue Eye Orb
        (114, 0x40000000),  // Dragon Eye
        (115, 0x40000000),  // Black Eye Orb
        (116, 0x40000000),  // Black Eye Orb
        (117, 0x40000000),  // Darksign
        (240, 0x40000000),  // Divine Blessing
        (260, 0x40000000),  // Green Blossom
        (270, 0x40000000),  // Bloodred Moss Clump
        (271, 0x40000000),  // Purple Moss Clump
        (272, 0x40000000),  // Blooming Purple Moss Clump
        (274, 0x40000000),  // Purging Stone
        (275, 0x40000000),  // Egg Vermifuge
        (280, 0x40000000),  // Repair Powder
        (290, 0x40000000),  // Throwing Knife
        (291, 0x40000000),  // Poison Throwing Knife
        (292, 0x40000000),  // Firebomb
        (293, 0x40000000),  // Dung Pie
        (294, 0x40000000),  // Alluring Skull
        (296, 0x40000000),  // Lloyd's Talisman
        (297, 0x40000000),  // Black Firebomb
        (310, 0x40000000),  // Charcoal Pine Resin
        (311, 0x40000000),  // Gold Pine Resin
        (312, 0x40000000),  // Transient Curse
        (313, 0x40000000),  // Rotten Pine Resin
        (330, 0x40000000),  // Homeward Bone
        (350, 0x40000000),  // Humanity
        (370, 0x40000000),  // Prism Stone
        (371, 0x40000000),  // Binoculars
        (372, 0x40000000),  // Affidavit
        (373, 0x40000000),  // Indictment
        (374, 0x40000000),  // Souvenir of Reprisal
        (375, 0x40000000),  // Sunlight Medal
        (376, 0x40000000),  // Pendant
        (377, 0x40000000),  // Dragon Head Stone
        (378, 0x40000000),  // Dragon Torso Stone
        (380, 0x40000000),  // Rubbish
        (381, 0x40000000),  // Copper Coin
        (382, 0x40000000),  // Silver Coin
        (383, 0x40000000),  // Gold Coin
        (384, 0x40000000),  // Peculiar Doll
        (385, 0x40000000),  // Dried Finger
        (390, 0x40000000),  // Fire Keeper Soul
        (391, 0x40000000),  // Fire Keeper Soul
        (392, 0x40000000),  // Fire Keeper Soul
        (393, 0x40000000),  // Fire Keeper Soul
        (394, 0x40000000),  // Fire Keeper Soul
        (395, 0x40000000),  // Fire Keeper Soul
        (396, 0x40000000),  // Fire Keeper Soul
        (400, 0x40000000),  // Soul of a Lost Undead
        (401, 0x40000000),  // Large Soul of a Lost Undead
        (402, 0x40000000),  // Soul of a Nameless Soldier
        (403, 0x40000000),  // Large Soul of a Nameless Soldier
        (404, 0x40000000),  // Soul of a Proud Knight
        (405, 0x40000000),  // Large Soul of a Proud Knight
        (406, 0x40000000),  // Soul of a Brave Warrior
        (407, 0x40000000),  // Large Soul of a Brave Warrior
        (408, 0x40000000),  // Soul of a Hero
        (409, 0x40000000),  // Soul of a Great Hero
        (500, 0x40000000),  // Humanity
        (501, 0x40000000),  // Twin Humanities
        (700, 0x40000000),  // Soul of Quelaag
        (701, 0x40000000),  // Soul of Sif
        (702, 0x40000000),  // Soul of Gwyn, Lord of Cinder
        (703, 0x40000000),  // Core of an Iron Golem
        (704, 0x40000000),  // Soul of Ornstein
        (705, 0x40000000),  // Soul of the Moonlight Butterfly
        (706, 0x40000000),  // Soul of Smough
        (707, 0x40000000),  // Soul of Priscilla
        (708, 0x40000000),  // Soul of Gwyndolin
        (800, 0x40000000),  // Large Ember
        (801, 0x40000000),  // Very Large Ember
        (802, 0x40000000),  // Crystal Ember
        (806, 0x40000000),  // Large Magic Ember
        (807, 0x40000000),  // Enchanted Ember
        (808, 0x40000000),  // Divine Ember
        (809, 0x40000000),  // Large Divine Ember
        (810, 0x40000000),  // Dark Ember
        (812, 0x40000000),  // Large Flame Ember
        (813, 0x40000000),  // Chaos Flame Ember
        (1000, 0x40000000),  // Titanite Shard
        (1010, 0x40000000),  // Large Titanite Shard
        (1020, 0x40000000),  // Green Titanite Shard
        (1030, 0x40000000),  // Titanite Chunk
        (1040, 0x40000000),  // Blue Titanite Chunk
        (1050, 0x40000000),  // White Titanite Chunk
        (1060, 0x40000000),  // Red Titanite Chunk
        (1070, 0x40000000),  // Titanite Slab
        (1080, 0x40000000),  // Blue Titanite Slab
        (1090, 0x40000000),  // White Titanite Slab
        (1100, 0x40000000),  // Red Titanite Slab
        (1110, 0x40000000),  // Dragon Scale
        (1120, 0x40000000),  // Demon Titanite
        (1130, 0x40000000),  // Twinkling Titanite
        (2600, 0x40000000),  // Weapon Smithbox
        (2601, 0x40000000),  // Armor Smithbox
        (2602, 0x40000000),  // Repairbox
        (2607, 0x40000000),  // Rite of Kindling
        (2608, 0x40000000),  // Bottomless Box
        (3000, 0x40000000),  // Sorcery: Soul Arrow
        (3010, 0x40000000),  // Sorcery: Great Soul Arrow
        (3020, 0x40000000),  // Sorcery: Heavy Soul Arrow
        (3030, 0x40000000),  // Sorcery: Great Heavy Soul Arrow
        (3040, 0x40000000),  // Sorcery: Homing Soulmass
        (3050, 0x40000000),  // Sorcery: Homing Crystal Soulmass
        (3060, 0x40000000),  // Soul Spear
        (3070, 0x40000000),  // Sorcery: Crystal Soul Spear
        (3100, 0x40000000),  // Magic Weapon
        (3110, 0x40000000),  // Sorcery: Great Magic Weapon
        (3120, 0x40000000),  // Sorcery: Crystal Magic Weapon
        (3300, 0x40000000),  // Sorcery: Magic Shield
        (3310, 0x40000000),  // Sorcery: Strong Magic Shield
        (3400, 0x40000000),  // Sorcery: Hidden Weapon
        (3410, 0x40000000),  // Sorcery: Hidden Body
        (3500, 0x40000000),  // Cast Light
        (3510, 0x40000000),  // Sorcery: Hush
        (3520, 0x40000000),  // Sorcery: Aural Decoy
        (3530, 0x40000000),  // Sorcery: Repair
        (3540, 0x40000000),  // Sorcery: Fall Control
        (3550, 0x40000000),  // Sorcery: Chameleon
        (3600, 0x40000000),  // Sorcery: Resist Curse
        (3610, 0x40000000),  // Sorcery: Remedy
        (3700, 0x40000000),  // Sorcery: White Dragon Breath
        (4000, 0x40000000),  // Pyromancy: Fireball
        (4010, 0x40000000),  // Fire Orb
        (4020, 0x40000000),  // Pyromancy: Great Fireball
        (4030, 0x40000000),  // Pyromancy: Firestorm
        (4040, 0x40000000),  // Pyromancy: Fire Tempest
        (4050, 0x40000000),  // Pyromancy: Fire Surge
        (4060, 0x40000000),  // Pyromancy: Fire Whip
        (4100, 0x40000000),  // Pyromancy: Combustion
        (4110, 0x40000000),  // Great Combustion
        (4200, 0x40000000),  // Pyromancy: Poison Mist
        (4210, 0x40000000),  // Pyromancy: Toxic Mist
        (4220, 0x40000000),  // Pyromancy: Acid Surge
        (4300, 0x40000000),  // Iron Flesh
        (4310, 0x40000000),  // Pyromancy: Flash Sweat
        (4360, 0x40000000),  // Pyromancy: Undead Rapport
        (4400, 0x40000000),  // Pyromancy: Power Within
        (4500, 0x40000000),  // Pyromancy: Great Chaos Fireball
        (4510, 0x40000000),  // Pyromancy: Chaos Storm
        (4520, 0x40000000),  // Pyromancy: Chaos Fire Whip
        (5000, 0x40000000),  // Miracle: Heal
        (5010, 0x40000000),  // Miracle: Great Heal
        (5020, 0x40000000),  // Great Heal Excerpt
        (5030, 0x40000000),  // Miracle: Soothing Sunlight
        (5040, 0x40000000),  // Replenishment
        (5050, 0x40000000),  // Miracle: Bountiful Sunlight
        (5100, 0x40000000),  // Miracle: Gravelord Sword Dance
        (5110, 0x40000000),  // Miracle: Gravelord Greatsword Dance
        (5200, 0x40000000),  // Miracle: Escape Death
        (5210, 0x40000000),  // Homeward
        (5300, 0x40000000),  // Miracle: Force
        (5310, 0x40000000),  // Miracle: Wrath of the Gods
        (5320, 0x40000000),  // Miracle: Emit Force
        (5400, 0x40000000),  // Seek Guidance
        (5500, 0x40000000),  // Miracle: Lightning Spear
        (5510, 0x40000000),  // Miracle: Great Lightning Spear
        (5520, 0x40000000),  // Miracle: Sunlight Spear
        (5600, 0x40000000),  // Miracle: Magic Barrier
        (5610, 0x40000000),  // Miracle: Great Magic Barrier
        (5700, 0x40000000),  // Miracle: Karmic Justice
        (5800, 0x40000000),  // Miracle: Tranquil Walk of Peace
        (5810, 0x40000000),  // Miracle: Vow of Silence
        (5900, 0x40000000),  // Miracle: Sunlight Blade
        (5910, 0x40000000),  // Miracle: Darkmoon Blade
        (9000, 0x40000000),  // Beckon
        (9001, 0x40000000),  // Point forward
        (9002, 0x40000000),  // Hurrah!
        (9003, 0x40000000),  // Bow
        (9004, 0x40000000),  // Joy
        (9005, 0x40000000),  // Shrug
        (9006, 0x40000000),  // Wave
        (9007, 0x40000000),  // Praise the Sun
        (9008, 0x40000000),  // Point up
        (9009, 0x40000000),  // Point down
        (9010, 0x40000000),  // Look skyward
        (9011, 0x40000000),  // Well! What is it!
        (9012, 0x40000000),  // Prostration
        (9013, 0x40000000),  // Proper bow
        (9014, 0x40000000),  // Prayer
        (118, 0x40000000),  // Purple Coward's Crystal
        (220, 0x40000000),  // Silver Pendant
        (230, 0x40000000),  // Elizabeth's Mushroom
        (510, 0x40000000),  // Hello Carving
        (511, 0x40000000),  // Thank you Carving
        (512, 0x40000000),  // Very good! Carving
        (513, 0x40000000),  // I'm sorry Carving
        (514, 0x40000000),  // Help me! Carving
        (709, 0x40000000),  // Guardian Soul
        (710, 0x40000000),  // Soul of Artorias
        (711, 0x40000000),  // Soul of Manus
        (3710, 0x40000000),  // Dark Orb
        (3720, 0x40000000),  // Dark Bead
        (3730, 0x40000000),  // Dark Fog
        (3740, 0x40000000),  // Pursuers
        (4530, 0x40000000),  // Black Flame
    };
}
