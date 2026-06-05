using SoulsFormats;
using ArgType = SoulsFormats.EMEVD.Instruction.ArgType;

namespace DS1Mod.Modding;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum FlagState : byte { Off = 0, On = 1 }
public enum LifeState  : byte { Dead = 0, Alive = 1 }
public enum EnabledState : byte { Disabled = 0, Enabled = 1 }

// ── Instr factory ─────────────────────────────────────────────────────────────

/// <summary>
/// Factory + matchers for EMEVD instructions, so you write
/// <c>Instr.DisplayMessage(123)</c> instead of remembering bank/id/arg-widths.
/// Arg widths must match the EMEDF: use the exact boxed types below.
/// </summary>
public static class Instr
{
    /// <summary>Any instruction by bank/id with explicit args (widths inferred from the boxed types).</summary>
    public static EMEVD.Instruction Raw(int bank, int id, params object[] args) => new(bank, id, args.ToList());

    // ── control flow ──────────────────────────────────────────────────────────

    /// <summary>2000:0 — start an event from the constructor.</summary>
    public static EMEVD.Instruction InitializeEvent(long eventId, int slot = 0) =>
        new(2000, 0, new List<object> { slot, (uint)eventId, (uint)0 });

    /// <summary>1000:4 — unconditional end (EndType 0) or restart (EndType 1).</summary>
    public static EMEVD.Instruction EndUnconditionally(byte endType = 0) =>
        new(1000, 4, new List<object> { endType });

    // ── conditions (condGroup 0 = MAIN, blocks the event until true) ──────────

    /// <summary>3:0 — block until an event flag reaches <paramref name="state"/>.</summary>
    public static EMEVD.Instruction IfEventFlag(FlagState state, int flagId, sbyte condGroup = 0) =>
        new(3, 0, new List<object> { condGroup, (byte)state, (byte)0, flagId });

    /// <summary>4:0 — block until a character is dead or alive.</summary>
    public static EMEVD.Instruction IfCharacterDeadAlive(LifeState state, int entityId, sbyte condGroup = 0) =>
        new(4, 0, new List<object> { condGroup, entityId, (byte)state });

    /// <summary>4:2 — block until entity HP ratio meets a comparison.
    /// Comparison Type: 0=Equal, 1=NotEqual, 2=Greater, 3=Less, 4=GreaterOrEqual, 5=LessOrEqual.</summary>
    public static EMEVD.Instruction IfHpRatio(int entityId, sbyte compType, float ratio, sbyte condGroup = 0) =>
        new(4, 2, new List<object> { condGroup, entityId, compType, ratio });

    /// <summary>3:2 — block until entity is inside (desired=1) or outside (desired=0) an area.</summary>
    public static EMEVD.Instruction IfInsideArea(int entityId, int areaEntityId, byte desired = 1, sbyte condGroup = 0) =>
        new(3, 2, new List<object> { condGroup, desired, entityId, areaEntityId });

    /// <summary>3:4 — block until player has (desired=1) or lacks (desired=0) an item.</summary>
    public static EMEVD.Instruction IfPlayerHasItem(int itemType, int itemId, byte desired = 1, sbyte condGroup = 0) =>
        new(3, 4, new List<object> { condGroup, itemType, itemId, desired });

    /// <summary>4:5 — block until a character has (shouldHave=true) or no longer has (shouldHave=false) a SpEffect active.</summary>
    public static EMEVD.Instruction IfCharacterHasSpEffect(int entityId, int spEffectId, bool shouldHave = true, sbyte condGroup = 0) =>
        new(4, 5, new List<object> { condGroup, entityId, spEffectId, (byte)(shouldHave ? 1 : 0) });

    // ── event flags ───────────────────────────────────────────────────────────

    /// <summary>2003:2 — set an event flag on/off.</summary>
    public static EMEVD.Instruction SetEventFlag(int flagId, FlagState state) =>
        new(2003, 2, new List<object> { flagId, (byte)state });

    // ── items ─────────────────────────────────────────────────────────────────

    /// <summary>2003:4 — give the player an item lot (respects the lot's getItemFlagId for once-only).</summary>
    public static EMEVD.Instruction AwardItemLot(int itemLotId) =>
        new(2003, 4, new List<object> { itemLotId });

    // ── display ───────────────────────────────────────────────────────────────

    /// <summary>2007:4 — pop a centered on-screen message (custom text via Event_text FMG).</summary>
    public static EMEVD.Instruction DisplayMessage(int messageId, byte screenLocation = 0) =>
        new(2007, 4, new List<object> { messageId, screenLocation });

    /// <summary>2007:3 — status/explanation text box (custom text via Event_text FMG).</summary>
    public static EMEVD.Instruction DisplayStatusMessage(int messageId, bool pad = false) =>
        new(2007, 3, new List<object> { messageId, (byte)(pad ? 1 : 0) });

    /// <summary>2007:2 — the big banner. <paramref name="bannerType"/> is a fixed preset (1=Victory, 2=You Died…).</summary>
    public static EMEVD.Instruction DisplayBanner(byte bannerType) =>
        new(2007, 2, new List<object> { bannerType });

    // ── character ─────────────────────────────────────────────────────────────

    /// <summary>2003:18 — force-play an animation on a character.</summary>
    public static EMEVD.Instruction ForceAnimation(int entityId, int animationId,
        bool loop = false, bool waitForCompletion = false, bool ignoreTransition = false) =>
        new(2003, 18, new List<object> { entityId, animationId,
            (byte)(loop ? 1 : 0), (byte)(waitForCompletion ? 1 : 0), (byte)(ignoreTransition ? 1 : 0) });

    /// <summary>2004:5 — enable or disable a character (visibility + collision).</summary>
    public static EMEVD.Instruction SetCharacterEnabled(int entityId, EnabledState state) =>
        new(2004, 5, new List<object> { entityId, (byte)state });

    /// <summary>2004:4 — force character death.</summary>
    public static EMEVD.Instruction KillCharacter(int entityId, bool awardSouls = false) =>
        new(2004, 4, new List<object> { entityId, (byte)(awardSouls ? 1 : 0) });

    /// <summary>2004:1 — enable or disable a character's AI.</summary>
    public static EMEVD.Instruction SetCharacterAI(int entityId, EnabledState state) =>
        new(2004, 1, new List<object> { entityId, (byte)state });

    /// <summary>2004:13 — set a character's home point to a region entity.</summary>
    public static EMEVD.Instruction SetCharacterHome(int entityId, int regionEntityId) =>
        new(2004, 13, new List<object> { entityId, regionEntityId });

    /// <summary>2004:12 — make a character immortal (unkillable) or mortal.</summary>
    public static EMEVD.Instruction SetCharacterImmortal(int entityId, EnabledState state) =>
        new(2004, 12, new List<object> { entityId, (byte)state });

    /// <summary>2004:15 — make a character invincible (no damage taken) or vincible.</summary>
    public static EMEVD.Instruction SetCharacterInvincible(int entityId, EnabledState state) =>
        new(2004, 15, new List<object> { entityId, (byte)state });

    /// <summary>2004:41 — short warp: teleport character to a destination entity (warpType=0 for point).</summary>
    public static EMEVD.Instruction WarpCharacter(int entityId, int destEntityId, byte warpType = 0) =>
        new(2004, 41, new List<object> { entityId, warpType, destEntityId, (int)-1 });

    // ── boss ──────────────────────────────────────────────────────────────────

    /// <summary>2003:11 — show/hide the boss HP bar. nameId is an NPC Name FMG id.</summary>
    public static EMEVD.Instruction DisplayBossHealthBar(int entityId, EnabledState state,
        int slot = 0, short nameId = 0) =>
        new(2003, 11, new List<object> { (sbyte)state, entityId, (short)slot, nameId });

    /// <summary>2003:12 — trigger the boss-death sequence (plays music, drops souls…).</summary>
    public static EMEVD.Instruction HandleBossDefeat(int entityId) =>
        new(2003, 12, new List<object> { entityId });

    // ── condition groups ──────────────────────────────────────────────────────

    /// <summary>0:0 — check a sub-condition group into a result group (AND/OR composition).
    /// <para>resultGroup=0 is MAIN (blocks the event). desired=1 means "wait until true".</para>
    /// <para>targetGroup positive = AND group, negative = OR group.</para></summary>
    public static EMEVD.Instruction IfConditionGroup(sbyte resultGroup, byte desired, sbyte targetGroup) =>
        new(0, 0, new List<object> { resultGroup, desired, targetGroup });

    // ── sfx / sound ───────────────────────────────────────────────────────────

    /// <summary>2006:3 — spawn a one-shot SFX at an entity's dummypoly.</summary>
    public static EMEVD.Instruction SpawnOneshotSfx(int entityType, int entityId, int dummypolyId, int sfxId) =>
        new(2006, 3, new List<object> { entityType, entityId, dummypolyId, sfxId });

    /// <summary>2010:2 — play a sound effect on an entity.</summary>
    public static EMEVD.Instruction PlaySound(int entityId, int soundType, int soundId) =>
        new(2010, 2, new List<object> { entityId, soundType, soundId });

    /// <summary>2008:2 — camera shake at an entity.</summary>
    public static EMEVD.Instruction CameraVibration(int vibrationId, int entityType, int entityId,
        int dummypolyId, float decayStart, float decayEnd) =>
        new(2008, 2, new List<object> { vibrationId, entityType, entityId, dummypolyId, decayStart, decayEnd });

    // ── matchers (for InsertAfter / idempotency) ──────────────────────────────

    public static Func<EMEVD.Instruction, bool> IsForceAnimation(int entityId, int animationId) => i =>
        i.Bank == 2003 && i.ID == 18 && ArgAt(i, 0, ArgType.Int32, ArgType.Int32) == entityId
                                     && ArgAt(i, 1, ArgType.Int32, ArgType.Int32) == animationId;

    public static Func<EMEVD.Instruction, bool> IsDisplayMessage(int messageId) => i =>
        i.Bank == 2007 && i.ID == 4 && ArgAt(i, 0, ArgType.Int32, ArgType.Byte) == messageId;

    internal static long InitTargetId(EMEVD.Instruction i) =>
        ArgAtL(i, 1, ArgType.Int32, ArgType.UInt32, ArgType.UInt32);

    // ── arg decode helpers ────────────────────────────────────────────────────

    private static int ArgAt(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt32(i.UnpackArgs(layout)[idx]); } catch { return int.MinValue; }
    }
    private static long ArgAtL(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt64(i.UnpackArgs(layout)[idx]); } catch { return long.MinValue; }
    }
}

// ── SubConditionBuilder ───────────────────────────────────────────────────────

/// <summary>
/// Builder for one AND or OR condition group, used inside
/// <see cref="EventBuilder.WhenAllOf"/> / <see cref="EventBuilder.WhenAnyOf"/>.
/// Conditions written here are assigned to the allocated sub-group rather than MAIN.
/// </summary>
public sealed class SubConditionBuilder
{
    private readonly List<EMEVD.Instruction> _parent;
    private readonly sbyte _group;

    internal SubConditionBuilder(List<EMEVD.Instruction> parent, sbyte group)
    {
        _parent = parent;
        _group  = group;
    }

    public SubConditionBuilder Flag(int flagId, FlagState state)
    { _parent.Add(Instr.IfEventFlag(state, flagId, _group)); return this; }

    public SubConditionBuilder Dead(int entityId)
    { _parent.Add(Instr.IfCharacterDeadAlive(LifeState.Dead, entityId, _group)); return this; }

    public SubConditionBuilder Alive(int entityId)
    { _parent.Add(Instr.IfCharacterDeadAlive(LifeState.Alive, entityId, _group)); return this; }

    public SubConditionBuilder HpBelow(int entityId, float ratio)
    { _parent.Add(Instr.IfHpRatio(entityId, compType: 3, ratio, _group)); return this; }

    public SubConditionBuilder InsideArea(int entityId, int areaEntityId)
    { _parent.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 1, _group)); return this; }

    public SubConditionBuilder OutsideArea(int entityId, int areaEntityId)
    { _parent.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 0, _group)); return this; }

    public SubConditionBuilder HasItem(int itemType, int itemId)
    { _parent.Add(Instr.IfPlayerHasItem(itemType, itemId, desired: 1, _group)); return this; }

    /// <summary>Append any sub-condition by bank/id/args directly.</summary>
    public SubConditionBuilder Raw(int bank, int id, params object[] args)
    { _parent.Add(Instr.Raw(bank, id, args)); return this; }
}

// ── EventBuilder ──────────────────────────────────────────────────────────────

/// <summary>
/// Fluent builder for a single EMEVD event body. Use via
/// <see cref="EmevdEditor.DefineEvent(long, EMEVD.Event.RestBehaviorType, Action{EventBuilder}, bool, int)"/>.
///
/// <para>Each method appends one or more instructions and returns <c>this</c> for chaining.
/// Call <see cref="End"/> or <see cref="Restart"/> to terminate the event.
/// Use <see cref="Raw"/> for anything not yet covered by a named method.</para>
///
/// <para>Phase 1 limitation: condition groups are not allocated automatically.
/// Each <c>When*</c> call targets the MAIN group (condGroup 0), which blocks the
/// event until the condition is true — suitable for simple linear sequences.
/// Complex AND/OR branches require Phase 2's condition allocator.</para>
/// </summary>
public sealed class EventBuilder
{
    private readonly List<EMEVD.Instruction> _instrs = new();
    private int _nextAndGroup = 1;   // AND groups: 1–7
    private int _nextOrGroup  = -1;  // OR  groups: -1 to -7

    internal IEnumerable<EMEVD.Instruction> Build() => _instrs;

    // ── low-level escape hatch ────────────────────────────────────────────────

    /// <summary>Append any instruction by bank/id/args directly.</summary>
    public EventBuilder Raw(int bank, int id, params object[] args)
    {
        _instrs.Add(Instr.Raw(bank, id, args));
        return this;
    }

    // ── event termination ─────────────────────────────────────────────────────

    /// <summary>Append an unconditional End instruction (event runs once and stops).</summary>
    public EventBuilder End()
    {
        _instrs.Add(Instr.EndUnconditionally(endType: 0));
        return this;
    }

    /// <summary>Append an unconditional Restart instruction (event loops).</summary>
    public EventBuilder Restart()
    {
        _instrs.Add(Instr.EndUnconditionally(endType: 1));
        return this;
    }

    // ── conditions (MAIN group — blocking waits) ──────────────────────────────

    /// <summary>Block until an event flag reaches <paramref name="state"/>.</summary>
    public EventBuilder WhenFlag(int flagId, FlagState state)
    {
        _instrs.Add(Instr.IfEventFlag(state, flagId));
        return this;
    }

    /// <summary>Block until a character is dead.</summary>
    public EventBuilder WhenDead(int entityId)
    {
        _instrs.Add(Instr.IfCharacterDeadAlive(LifeState.Dead, entityId));
        return this;
    }

    /// <summary>Block until a character is alive.</summary>
    public EventBuilder WhenAlive(int entityId)
    {
        _instrs.Add(Instr.IfCharacterDeadAlive(LifeState.Alive, entityId));
        return this;
    }

    /// <summary>Block until entity HP ratio drops below <paramref name="ratio"/> (0.0–1.0).</summary>
    public EventBuilder WhenHpBelow(int entityId, float ratio)
    {
        _instrs.Add(Instr.IfHpRatio(entityId, compType: 3, ratio)); // 3 = Less
        return this;
    }

    /// <summary>Block until entity is inside an area region.</summary>
    public EventBuilder WhenInsideArea(int entityId, int areaEntityId)
    {
        _instrs.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 1));
        return this;
    }

    /// <summary>Block until entity leaves an area region.</summary>
    public EventBuilder WhenOutsideArea(int entityId, int areaEntityId)
    {
        _instrs.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 0));
        return this;
    }

    /// <summary>Block until <paramref name="entityId"/> has SpEffect <paramref name="spEffectId"/> active.</summary>
    public EventBuilder WhenCharacterHasSpEffect(int entityId, int spEffectId)
    {
        _instrs.Add(Instr.IfCharacterHasSpEffect(entityId, spEffectId, shouldHave: true));
        return this;
    }

    /// <summary>Block until <paramref name="entityId"/> no longer has SpEffect <paramref name="spEffectId"/> active.</summary>
    public EventBuilder WhenCharacterLosesSpEffect(int entityId, int spEffectId)
    {
        _instrs.Add(Instr.IfCharacterHasSpEffect(entityId, spEffectId, shouldHave: false));
        return this;
    }

    /// <summary>
    /// Block until ALL of the supplied sub-conditions are true simultaneously (AND group).
    /// <para>Example: wait until a flag is on AND a boss is dead:</para>
    /// <code>
    /// ev.WhenAllOf(and => and.Flag(16, FlagState.On).Dead(1010800))
    /// </code>
    /// DS1 supports up to 7 AND groups per event. Each call to <c>WhenAllOf</c>
    /// allocates one; don't nest more than 7 in a single event.
    /// </summary>
    public EventBuilder WhenAllOf(Action<SubConditionBuilder> conds)
    {
        sbyte group = (sbyte)_nextAndGroup++;
        conds(new SubConditionBuilder(_instrs, group));
        _instrs.Add(Instr.IfConditionGroup(resultGroup: 0, desired: 1, targetGroup: group));
        return this;
    }

    /// <summary>
    /// Block until ANY of the supplied sub-conditions is true (OR group).
    /// <para>Example: wait until either a flag is on OR the boss is dead:</para>
    /// <code>
    /// ev.WhenAnyOf(or => or.Flag(16, FlagState.On).Dead(1010800))
    /// </code>
    /// DS1 supports up to 7 OR groups per event.
    /// </summary>
    public EventBuilder WhenAnyOf(Action<SubConditionBuilder> conds)
    {
        sbyte group = (sbyte)_nextOrGroup--;
        conds(new SubConditionBuilder(_instrs, group));
        _instrs.Add(Instr.IfConditionGroup(resultGroup: 0, desired: 1, targetGroup: group));
        return this;
    }

    // ── actions: flags ────────────────────────────────────────────────────────

    /// <summary>Set an event flag on or off.</summary>
    public EventBuilder SetFlag(int flagId, FlagState state)
    {
        _instrs.Add(Instr.SetEventFlag(flagId, state));
        return this;
    }

    // ── actions: items ────────────────────────────────────────────────────────

    /// <summary>Award an item lot to the player (once-only if the lot has a getItemFlagId).</summary>
    public EventBuilder AwardItemLot(int itemLotId)
    {
        _instrs.Add(Instr.AwardItemLot(itemLotId));
        return this;
    }

    // ── actions: display ──────────────────────────────────────────────────────

    /// <summary>Show a centered on-screen message (text from Event_text FMG).</summary>
    public EventBuilder DisplayMessage(int messageId, byte screenLocation = 0)
    {
        _instrs.Add(Instr.DisplayMessage(messageId, screenLocation));
        return this;
    }

    /// <summary>Show a status/explanation text box (text from Event_text FMG).</summary>
    public EventBuilder DisplayStatusMessage(int messageId)
    {
        _instrs.Add(Instr.DisplayStatusMessage(messageId));
        return this;
    }

    /// <summary>Show a big banner with a fixed preset (1=Victory Achieved, 2=You Died…).</summary>
    public EventBuilder DisplayBanner(byte bannerType)
    {
        _instrs.Add(Instr.DisplayBanner(bannerType));
        return this;
    }

    /// <summary>Show or hide the boss HP bar.</summary>
    public EventBuilder DisplayBossHealthBar(int entityId, EnabledState state, int slot = 0, short nameId = 0)
    {
        _instrs.Add(Instr.DisplayBossHealthBar(entityId, state, slot, nameId));
        return this;
    }

    // ── actions: character ────────────────────────────────────────────────────

    /// <summary>Force-play an animation on a character.</summary>
    public EventBuilder ForceAnimation(int entityId, int animId,
        bool loop = false, bool wait = false, bool ignoreTransition = false)
    {
        _instrs.Add(Instr.ForceAnimation(entityId, animId, loop, wait, ignoreTransition));
        return this;
    }

    /// <summary>Enable or disable a character (visibility + collision).</summary>
    public EventBuilder SetCharacterEnabled(int entityId, EnabledState state)
    {
        _instrs.Add(Instr.SetCharacterEnabled(entityId, state));
        return this;
    }

    /// <summary>Force character death.</summary>
    public EventBuilder KillCharacter(int entityId, bool awardSouls = false)
    {
        _instrs.Add(Instr.KillCharacter(entityId, awardSouls));
        return this;
    }

    /// <summary>Enable or disable a character's AI.</summary>
    public EventBuilder SetCharacterAI(int entityId, EnabledState state)
    {
        _instrs.Add(Instr.SetCharacterAI(entityId, state));
        return this;
    }

    /// <summary>Set a character's home point to a region entity.</summary>
    public EventBuilder SetCharacterHome(int entityId, int regionEntityId)
    {
        _instrs.Add(Instr.SetCharacterHome(entityId, regionEntityId));
        return this;
    }

    /// <summary>Make a character immortal (unkillable) or mortal.</summary>
    public EventBuilder SetCharacterImmortal(int entityId, EnabledState state)
    {
        _instrs.Add(Instr.SetCharacterImmortal(entityId, state));
        return this;
    }

    /// <summary>Make a character invincible (no damage) or vincible.</summary>
    public EventBuilder SetCharacterInvincible(int entityId, EnabledState state)
    {
        _instrs.Add(Instr.SetCharacterInvincible(entityId, state));
        return this;
    }

    /// <summary>Teleport a character to a destination entity.</summary>
    public EventBuilder WarpCharacter(int entityId, int destEntityId)
    {
        _instrs.Add(Instr.WarpCharacter(entityId, destEntityId));
        return this;
    }

    // ── actions: boss ─────────────────────────────────────────────────────────

    /// <summary>Trigger the boss-death sequence (music, soul award…).</summary>
    public EventBuilder HandleBossDefeat(int entityId)
    {
        _instrs.Add(Instr.HandleBossDefeat(entityId));
        return this;
    }
}

// ── EmevdEditor ───────────────────────────────────────────────────────────────

/// <summary>
/// High-level edits over an EMEVD, with the gotchas baked in. All operations are
/// idempotent so a patcher can run every launch.
/// </summary>
public sealed class EmevdEditor
{
    public EMEVD Evd { get; }
    private readonly Action<string>? _record;

    public EmevdEditor(EMEVD evd, Action<string>? record = null)
    {
        Evd = evd;
        _record = record;
    }

    public EMEVD.Event Constructor => Evd.Events.First(e => e.ID == 0);
    public EMEVD.Event? Event(long id) => Evd.Events.FirstOrDefault(e => e.ID == id);

    /// <summary>
    /// Define (or replace) an event using a fluent <see cref="EventBuilder"/> and,
    /// by default, register it at the top of the constructor.
    ///
    /// <code>
    /// emevd.DefineEvent(11819100, RestBehavior.Default, ev => ev
    ///     .WhenFlag(16, FlagState.On)
    ///     .AwardItemLot(8500)
    ///     .End());
    /// </code>
    /// </summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest,
        Action<EventBuilder> build, bool register = true, int slot = 0)
    {
        var builder = new EventBuilder();
        build(builder);
        return DefineEvent(id, rest, builder.Build(), register, slot);
    }

    /// <summary>
    /// Define (or replace) an event and, by default, register it. Registration
    /// goes at the TOP of the constructor — event 0 has multiplayer SKIPs and an
    /// END IF partway through, so a registration appended at the end can be
    /// skipped/terminated and never run. (Event 0 has no parameters and SKIP
    /// offsets are relative, so prepending is safe.)
    /// </summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest,
        IEnumerable<EMEVD.Instruction> body, bool register = true, int slot = 0)
    {
        _record?.Invoke($"EMEVD:Event:{id}");
        Evd.Events.RemoveAll(e => e.ID == id);
        var ev = new EMEVD.Event(id, rest);
        ev.Instructions.AddRange(body);
        Evd.Events.Add(ev);
        if (register) RegisterAtConstructorTop(id, slot);
        return ev;
    }

    /// <summary>Convenience overload: <c>DefineEvent(id, rest, instr, instr, …)</c>.</summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest, params EMEVD.Instruction[] body) =>
        DefineEvent(id, rest, body, register: true);

    /// <summary>Register an event at the top of the constructor (idempotent).</summary>
    public void RegisterAtConstructorTop(long eventId, int slot = 0)
    {
        EMEVD.Event c = Constructor;
        c.Instructions.RemoveAll(x => x.Bank == 2000 && x.ID == 0 && Instr.InitTargetId(x) == eventId);
        c.Instructions.Insert(0, Instr.InitializeEvent(eventId, slot));
    }

    /// <summary>
    /// Insert <paramref name="toInsert"/> immediately after the first instruction
    /// in event <paramref name="eventId"/> matching <paramref name="match"/>.
    /// Skips if <paramref name="alreadyPresent"/> already matches something in the
    /// event (idempotency). Fixes up event-parameter indices. Returns true if inserted.
    /// </summary>
    public bool InsertAfter(long eventId, Func<EMEVD.Instruction, bool> match, EMEVD.Instruction toInsert,
        Func<EMEVD.Instruction, bool>? alreadyPresent = null)
    {
        EMEVD.Event? ev = Event(eventId);
        if (ev == null) return false;
        if (alreadyPresent != null && ev.Instructions.Any(alreadyPresent)) return false;
        for (int i = 0; i < ev.Instructions.Count; i++)
        {
            if (!match(ev.Instructions[i])) continue;
            ev.Instructions.Insert(i + 1, toInsert);
            foreach (EMEVD.Parameter p in ev.Parameters)
                if (p.InstructionIndex >= i + 1) p.InstructionIndex++;
            return true;
        }
        return false;
    }
}
