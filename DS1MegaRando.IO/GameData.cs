using SoulsFormats;

namespace DS1MegaRando.IO;

/// <summary>
/// In-memory representation of all game files relevant to randomization.
/// Populated by GameFileReader; consumed by all randomizer modules and written by GameFileWriter.
/// </summary>
public class GameData
{
    /// <summary>ItemLotParam rows, keyed by row ID. Apply paramdef before use.</summary>
    public PARAM? ItemLotParam      { get; set; }

    /// <summary>ShopLineupParam rows, keyed by row ID.</summary>
    public PARAM? ShopLineupParam   { get; set; }

    /// <summary>NpcParam rows, keyed by row ID.</summary>
    public PARAM? NpcParam          { get; set; }

    /// <summary>CharaInitParam rows, keyed by row ID.</summary>
    public PARAM? CharaInitParam    { get; set; }

    /// <summary>MapId → MSB for each map file.</summary>
    public Dictionary<string, MSB1> Maps { get; set; } = new();

    /// <summary>MapId → original source path (used to write back in the same format).</summary>
    public Dictionary<string, string> MapSourcePaths { get; set; } = new();

    /// <summary>
    /// All enemy model IDs that appear in at least one vanilla MSB (e.g. "c2500").
    /// Populated by GameFileReader.  Only models in this set are safe to reference
    /// in modified MSBs — every model here is guaranteed to have a chrbnd.dcx file
    /// in the game directory.
    /// </summary>
    public HashSet<string> KnownEnemyModels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The BND3 containing all game params — held open so we can repack it on write.</summary>
    public BND3? ParamBnd { get; set; }

    /// <summary>DCX compression type of the original parambnd.dcx.</summary>
    public DCX.Type ParamBndCompression { get; set; } = DCX.Type.None;
}
