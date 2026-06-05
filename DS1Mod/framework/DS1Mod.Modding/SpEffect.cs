using SoulsFormats;

namespace DS1Mod.Modding;

// ── SpEffectDef ───────────────────────────────────────────────────────────────

/// <summary>
/// Everything needed to define a new SpEffect row in one place.
/// Pass to <see cref="GamePatch.DefineSpEffect"/>.
///
/// SpEffects drive almost all triggered behavior in DS1 — HP restores, buffs,
/// status infliction, etc. An item's <see cref="ItemDef.SpEffectId"/> wires the
/// item's use to a SpEffect; <see cref="GamePatch.DefineItemTrigger"/> then
/// emits an EMEVD bridge so C# code can react to the use event.
///
/// <para>Common pattern:</para>
/// <code>
/// // 1. Define what happens on use
/// g.DefineSpEffect(paramdefs, new SpEffectDef
/// {
///     Id             = 9000,
///     Duration       = 0f,          // instant (fires once, no lingering)
///     HpRecoverPoint = 400,         // restore 400 HP
/// });
///
/// // 2. Link item → SpEffect
/// g.DefineGoods(paramdefs, new ItemDef
/// {
///     Id         = 8000,
///     SpEffectId = 9000,
///     Name       = "Goofy Draught",
/// });
///
/// // 3. EMEVD bridge → event flag (optional — for C# callback on use)
/// g.DefineItemTrigger("m18_01_00_00", spEffectId: 9000, triggerFlagId: 11819200);
///
/// // 4. In mod's OnTick():
/// //    if (reader.GetEventFlag(11819200)) { /* custom logic */ }
/// //    OR subscribe via hooks.RegisterItemUsed(8000, 11819200)
/// </code>
/// </summary>
public sealed class SpEffectDef
{
    /// <summary>SpEffectParam row ID. Must be unique (use 9000+ range for mods).</summary>
    public int Id { get; set; }

    /// <summary>
    /// Existing SpEffect row to clone as the base.
    /// Default <c>110</c> — a vanilla DSR row with benign properties
    /// (instant, targets self+player, no state side-effect) that makes a
    /// safe template for custom on-use effects. Override if you need a
    /// different starting point (e.g. a stat-multiplier buff).
    /// </summary>
    public int DonorId { get; set; } = 110;

    /// <summary>
    /// How long the effect lasts in seconds.
    /// <c>0</c> = instant (fires once then disappears).
    /// </summary>
    public float Duration { get; set; } = 0f;

    // ── healing ───────────────────────────────────────────────────────────────

    /// <summary>Flat HP restored instantly on application.</summary>
    public int HpRecoverPoint { get; set; }

    /// <summary>HP restored per second over <see cref="Duration"/>.</summary>
    public float HpRecoverRate { get; set; }

    /// <summary>Flat stamina restored instantly.</summary>
    public int StaminaRecoverPoint { get; set; }

    // ── stat multipliers ─────────────────────────────────────────────────────

    /// <summary>Max HP multiplier. 1.0 = unchanged, 1.2 = +20% max HP.</summary>
    public float MaxHpRate { get; set; } = 1f;

    /// <summary>Physical attack power multiplier. 1.0 = unchanged.</summary>
    public float PhysAtkPowerRate { get; set; } = 1f;

    /// <summary>Magic attack power multiplier.</summary>
    public float MagicAtkPowerRate { get; set; } = 1f;

    /// <summary>Fire attack power multiplier.</summary>
    public float FireAtkPowerRate { get; set; } = 1f;

    /// <summary>Lightning attack power multiplier.</summary>
    public float ThunderAtkPowerRate { get; set; } = 1f;

    /// <summary>Physical defense multiplier. 1.0 = unchanged.</summary>
    public float PhysDefRate { get; set; } = 1f;

    /// <summary>Magic defense multiplier.</summary>
    public float MagicDefRate { get; set; } = 1f;

    /// <summary>Fire defense multiplier.</summary>
    public float FireDefRate { get; set; } = 1f;

    /// <summary>Lightning defense multiplier.</summary>
    public float ThunderDefRate { get; set; } = 1f;

    /// <summary>
    /// Called after cloning donor row — use to set any additional SpEffectParam
    /// fields not covered above (see DS1 paramdef for full field list).
    /// </summary>
    public Action<PARAM.Row>? Configure { get; set; }
}
