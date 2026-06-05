namespace DS1Mod.Core;

/// <summary>
/// Standard ID spaces for mod allocation. Use these instead of magic strings
/// to get IDE autocomplete, type safety, and easy refactoring.
/// </summary>
public static class IdSpaces
{
    // ── PARAM Rows (Global) ───────────────────────────────────────────────

    /// <summary>EquipParamGoods — consumable and key items.</summary>
    public const string EquipParamGoods = "EquipParamGoods";

    /// <summary>ItemLotParam — drop lots that award items.</summary>
    public const string ItemLotParam = "ItemLotParam";

    /// <summary>SpEffectParam — status effects and buffs.</summary>
    public const string SpEffectParam = "SpEffectParam";

    // ── FMG Entries (Global) ──────────────────────────────────────────────

    /// <summary>EventText (menu.msgbnd) — event messages, HUD text, pop-ups.</summary>
    public const string EventText = "EventText";

    /// <summary>StatusText (menu.msgbnd) — status box explanations.</summary>
    public const string StatusText = "StatusText";

    /// <summary>ItemName (item.msgbnd) — consumable/equipment names.</summary>
    public const string ItemName = "ItemName";

    /// <summary>ItemDescription (item.msgbnd) — short item descriptions.</summary>
    public const string ItemDescription = "ItemDescription";

    /// <summary>ItemLongDesc (item.msgbnd) — long item descriptions.</summary>
    public const string ItemLongDesc = "ItemLongDesc";

    // ── Item Obtained Flags (Global) ──────────────────────────────────────

    /// <summary>ItemObtainedFlags (50000000+) — permanent "item was collected" markers.</summary>
    public const string ItemObtainedFlags = "ItemObtainedFlags";

    // ── Map-Local Spaces ──────────────────────────────────────────────────
    // Use the helper methods below to generate space names for specific maps.

    /// <summary>Generate the EventFlags space name for a map (e.g., "EventFlags_m18_01").</summary>
    public static string EventFlags(string mapId) => $"EventFlags_{mapId}";

    /// <summary>Generate the EMEVD event ID space name for a map (e.g., "EmevdEvents_m18_01").</summary>
    public static string EmevdEvents(string mapId) => $"EmevdEvents_{mapId}";

    /// <summary>Generate the MSB entity ID space name for a map (e.g., "MsbEntities_m18_01").</summary>
    public static string MsbEntities(string mapId) => $"MsbEntities_{mapId}";

    // ── Common Map IDs ────────────────────────────────────────────────────
    // Use these constants to avoid typos in map IDs.

    public const string Asylum = "m18_01_00_00";
    public const string UndeadParish = "m10_01_00_00";
    public const string TheDepths = "m10_00_00_00";
    public const string Blighttown = "m14_00_00_00";
    public const string DemonRuins = "m14_01_00_00";
    public const string LostIzalith = "m14_01_00_00";
    public const string PaintedWorld = "m11_00_00_00";
    public const string DarkrootGarden = "m12_00_00_01";
    public const string DarkrootBasin = "m12_00_00_00";
    public const string TombOfTheGiants = "m13_00_00_00";
    public const string NewLondoRuins = "m16_00_00_00";
    public const string TheAbyss = "m16_00_00_01";
    public const string SensFortress = "m15_00_00_00";
    public const string AnorLondo = "m15_01_00_00";
    public const string DukesArchives = "m17_00_00_00";
    public const string KilnOfTheFirstFlame = "m18_00_00_00";
    public const string Oolacile = "m12_01_00_00";
    public const string OolacileArena = "m12_01_00_01";
}
