using SoulsFormats;

namespace DS1Mod.Modding;

// ── SpEffectDef ───────────────────────────────────────────────────────────────

/// <summary>
/// Defines a single <c>SpEffectParam</c> row in Dark Souls: Remastered.
///
/// SpEffects are the core runtime effect system in DS1. They control:
/// - healing and damage over time
/// - stat buffs and debuffs
/// - status buildup (poison, bleed, curse)
/// - movement and animation modifiers
/// - AI and targeting behaviour
///
/// A SpEffect is typically triggered by:
/// - consuming a <see cref="ItemDef"/>
/// - weapon hits (via BehaviorParam / atkOccurrenceSpEffectId)
/// - EMEVD event scripts
///
/// This class provides a simplified, mod-friendly abstraction over the raw
/// SpEffectParam table, with a donor-cloning system and optional field override
/// hook (<see cref="Configure"/>).
/// </summary>
public sealed class SpEffectDef
{
    /// <summary>
    /// SpEffectParam row ID.
    /// Must be unique within the table (recommended: 9000+ for mods).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Existing SpEffectParam row used as a template.
    /// Default is <c>110</c>, a benign vanilla effect suitable for most custom buffs.
    /// </summary>
    public int DonorId { get; set; } = 110;

    /// <summary>
    /// Duration of the effect in seconds.
    /// <c>0</c> = instant application (no lingering effect).
    /// </summary>
    public float Duration { get; set; } = 0f;

    // ── Healing ───────────────────────────────────────────────────────────────

    /// <summary>Flat HP restored instantly when applied.</summary>
    public int HpRecoverPoint { get; set; }

    /// <summary>HP regenerated per second over <see cref="Duration"/>.</summary>
    public float HpRecoverRate { get; set; }

    /// <summary>Flat stamina restored instantly when applied.</summary>
    public int StaminaRecoverPoint { get; set; }

    // ── Stat multipliers ──────────────────────────────────────────────────────

    /// <summary>Multiplier for maximum HP (1.0 = unchanged).</summary>
    public float MaxHpRate { get; set; } = 1f;

    /// <summary>Multiplier for physical attack power.</summary>
    public float PhysAtkPowerRate { get; set; } = 1f;

    /// <summary>Multiplier for magic attack power.</summary>
    public float MagicAtkPowerRate { get; set; } = 1f;

    /// <summary>Multiplier for fire attack power.</summary>
    public float FireAtkPowerRate { get; set; } = 1f;

    /// <summary>Multiplier for lightning attack power.</summary>
    public float ThunderAtkPowerRate { get; set; } = 1f;

    /// <summary>Multiplier for physical defense.</summary>
    public float PhysDefRate { get; set; } = 1f;

    /// <summary>Multiplier for magic defense.</summary>
    public float MagicDefRate { get; set; } = 1f;

    /// <summary>Multiplier for fire defense.</summary>
    public float FireDefRate { get; set; } = 1f;

    /// <summary>Multiplier for lightning defense.</summary>
    public float ThunderDefRate { get; set; } = 1f;

    // ── Extension hook ────────────────────────────────────────────────────────

    /// <summary>
    /// Optional callback executed after cloning the donor row.
    /// Allows direct modification of raw <c>SpEffectParam</c> fields not exposed
    /// by this abstraction.
    /// </summary>
    public Action<PARAM.Row>? Configure { get; set; }
}