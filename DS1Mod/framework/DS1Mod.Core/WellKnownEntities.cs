namespace DS1Mod.Core;

/// <summary>
/// Well-known NPC and boss entity references used across multiple mods.
/// Contains NPC Param IDs and model IDs for commonly-referenced entities.
/// </summary>
public static class WellKnownEntities
{
    // ── Asylum Demon ──────────────────────────────────────────────────────
    /// <summary>Asylum Demon — NPC Param ID used for the boss slot entity.</summary>
    public const int AsylumDemonNpcId = 223200;

    /// <summary>Asylum Demon — Model ID (c2232) used for replacements.</summary>
    public const string AsylumDemonModelId = "c2232";

    // ── Stray Demon ───────────────────────────────────────────────────────
    /// <summary>Stray Demon — NPC Param ID for the second Asylum slot.</summary>
    public const int StrayDemonNpcId = 223000;

    /// <summary>Stray Demon — Model ID (c2230) used for replacements.</summary>
    public const string StrayDemonModelId = "c2230";

    // ── AI File IDs (for script/mXX_XX_XX_XX.luabnd.dcx modifications) ────
    /// <summary>Asylum Demon AI file ID in the Lua bundle.</summary>
    public const string AsylumDemonAiFileId = "223200";
}
