using SoulsFormats;
using ArgType = SoulsFormats.EMEVD.Instruction.ArgType;

namespace DS1Mod.Modding;

/// <summary>
/// Factory + matchers for EMEVD instructions, so you write
/// <c>Instr.DisplayMessage(123)</c> instead of remembering bank/id/arg-widths.
/// Arg widths must match the EMEDF: use the exact boxed types below.
/// </summary>
public static class Instr
{
    /// <summary>Any instruction by bank/id with explicit args (widths inferred from the boxed types).</summary>
    public static EMEVD.Instruction Raw(int bank, int id, params object[] args) => new(bank, id, args.ToList());

    // ── control flow ──
    /// <summary>2000:0 — start an event from the constructor.</summary>
    public static EMEVD.Instruction InitializeEvent(long eventId, int slot = 0) =>
        new(2000, 0, new List<object> { slot, (uint)eventId, (uint)0 });

    /// <summary>3:0 — wait/condition on an event flag. group 0 = MAIN (blocking).</summary>
    public static EMEVD.Instruction IfEventFlag(bool on, int flagId, sbyte conditionGroup = 0) =>
        new(3, 0, new List<object> { conditionGroup, (byte)(on ? 1 : 0), (byte)0, flagId });

    /// <summary>2003:2 — set an event flag on/off.</summary>
    public static EMEVD.Instruction SetEventFlag(int flagId, bool on) =>
        new(2003, 2, new List<object> { flagId, (byte)(on ? 1 : 0) });

    // ── actions ──
    /// <summary>2007:4 — pop a centered on-screen message (custom text via Event_text FMG).</summary>
    public static EMEVD.Instruction DisplayMessage(int messageId, byte screenLocation = 0) =>
        new(2007, 4, new List<object> { messageId, screenLocation });

    /// <summary>2007:3 — status/explanation text box (custom text via Event_text FMG).</summary>
    public static EMEVD.Instruction DisplayStatusMessage(int messageId, bool pad = false) =>
        new(2007, 3, new List<object> { messageId, (byte)(pad ? 1 : 0) });

    /// <summary>2007:2 — the big banner. <paramref name="bannerType"/> is a fixed preset (1=Victory, 2=You Died, …).</summary>
    public static EMEVD.Instruction DisplayBanner(byte bannerType) =>
        new(2007, 2, new List<object> { bannerType });

    /// <summary>2003:4 — give the player an item lot (respects the lot's getItemFlagId for once-only).</summary>
    public static EMEVD.Instruction AwardItemLot(int itemLotId) =>
        new(2003, 4, new List<object> { itemLotId });

    /// <summary>2003:18 — force-play an animation on a character.</summary>
    public static EMEVD.Instruction ForceAnimation(int entityId, int animationId,
        bool loop = false, bool waitForCompletion = false, bool ignoreTransition = false) =>
        new(2003, 18, new List<object> { entityId, animationId,
            (byte)(loop ? 1 : 0), (byte)(waitForCompletion ? 1 : 0), (byte)(ignoreTransition ? 1 : 0) });

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

    // ── matchers (for InsertAfter / idempotency) ──
    public static Func<EMEVD.Instruction, bool> IsForceAnimation(int entityId, int animationId) => i =>
        i.Bank == 2003 && i.ID == 18 && ArgAt(i, 0, ArgType.Int32, ArgType.Int32) == entityId
                                     && ArgAt(i, 1, ArgType.Int32, ArgType.Int32) == animationId;

    public static Func<EMEVD.Instruction, bool> IsDisplayMessage(int messageId) => i =>
        i.Bank == 2007 && i.ID == 4 && ArgAt(i, 0, ArgType.Int32, ArgType.Byte) == messageId;

    internal static long InitTargetId(EMEVD.Instruction i) =>
        ArgAtL(i, 1, ArgType.Int32, ArgType.UInt32, ArgType.UInt32);

    // ── arg decode helpers ──
    private static int ArgAt(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt32(i.UnpackArgs(layout)[idx]); } catch { return int.MinValue; }
    }
    private static long ArgAtL(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt64(i.UnpackArgs(layout)[idx]); } catch { return long.MinValue; }
    }
}

/// <summary>
/// High-level edits over an EMEVD, with the gotchas baked in. All operations are
/// idempotent so a patcher can run every launch.
/// </summary>
public sealed class EmevdEditor
{
    public EMEVD Evd { get; }
    public EmevdEditor(EMEVD evd) => Evd = evd;

    public EMEVD.Event Constructor => Evd.Events.First(e => e.ID == 0);
    public EMEVD.Event? Event(long id) => Evd.Events.FirstOrDefault(e => e.ID == id);

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
        Evd.Events.RemoveAll(e => e.ID == id);
        var ev = new EMEVD.Event(id, rest);
        ev.Instructions.AddRange(body);
        Evd.Events.Add(ev);
        if (register) RegisterAtConstructorTop(id, slot);
        return ev;
    }

    /// <summary>Convenience overload: <c>DefineEvent(id, rest, register, instr, instr, …)</c>.</summary>
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
