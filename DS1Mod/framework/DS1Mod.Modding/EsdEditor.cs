using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Fluent editor for ESD (EZState) state machine files.
///
/// ESD encodes NPC dialog trees, character action state machines, and other
/// game logic as a directed graph of states connected by bytecode-evaluated
/// conditions. DSR stores them in <c>.esdbnd.dcx</c> archives:
/// <list type="bullet">
///   <item><c>script/talk/t{npcId}.talkesdbnd.dcx</c> — NPC dialog (group 1)</item>
///   <item><c>script/talk/m{mapId}.talkesdbnd.dcx</c> — map-level talk scripts</item>
/// </list>
///
/// Obtain an instance via <see cref="GamePatch.EditEsd"/>:
/// <code>
/// g.EditEsd("script/talk/t200000.talkesdbnd.dcx", "200000.esd", esd =>
/// {
///     // Unconditionally transition state 1 → state 10:
///     esd.AddTransition(groupId: 1, fromState: 1, toState: 10,
///         evaluator: EsdBytecode.Always());
///
///     // On enter state 10, set an event flag ON:
///     esd.AddEntryCommand(groupId: 1, stateId: 10,
///         TalkCmd.SetEventFlag(flagId: 11810400, on: true));
/// });
/// </code>
/// </summary>
public class EsdEditor
{
    /// <summary>The parsed ESD being edited. Access directly for operations not covered by the helpers.</summary>
    public ESD Esd { get; }

    public EsdEditor(ESD esd) => Esd = esd;

    // ── State / group access ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the state group with <paramref name="groupId"/>, creating an
    /// empty one if it does not exist.
    /// </summary>
    public Dictionary<long, ESD.State> GetOrAddGroup(long groupId)
    {
        if (!Esd.StateGroups.TryGetValue(groupId, out var group))
        {
            group = new Dictionary<long, ESD.State>();
            Esd.StateGroups[groupId] = group;
        }
        return group;
    }

    /// <summary>
    /// Returns the state <paramref name="stateId"/> in <paramref name="groupId"/>,
    /// creating both if they do not exist.
    /// </summary>
    public ESD.State GetOrAddState(long groupId, long stateId)
    {
        var group = GetOrAddGroup(groupId);
        if (!group.TryGetValue(stateId, out var state))
        {
            state = new ESD.State();
            group[stateId] = state;
        }
        return state;
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a condition to <paramref name="fromState"/> that transitions to
    /// <paramref name="toState"/> when <paramref name="evaluator"/> returns
    /// nonzero. Conditions are checked in list order; the first passing one wins.
    /// Use <see cref="EsdBytecode"/> to build the evaluator.
    /// </summary>
    public void AddTransition(long groupId, long fromState, long toState, byte[] evaluator)
    {
        var from = GetOrAddState(groupId, fromState);
        _ = GetOrAddState(groupId, toState);
        from.Conditions.Add(new ESD.Condition(toState, evaluator));
    }

    /// <summary>
    /// Inserts a condition at <paramref name="index"/> in <paramref name="fromState"/>'s
    /// condition list — useful for injecting a high-priority transition before vanilla logic.
    /// </summary>
    public void InsertTransition(long groupId, long fromState, long toState, byte[] evaluator, int index = 0)
    {
        var from = GetOrAddState(groupId, fromState);
        _ = GetOrAddState(groupId, toState);
        from.Conditions.Insert(index, new ESD.Condition(toState, evaluator));
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a command to <paramref name="stateId"/> that fires once when the
    /// machine enters that state. Build <paramref name="cmd"/> with
    /// <see cref="TalkCmd"/> or construct an <see cref="ESD.CommandCall"/> directly.
    /// </summary>
    public void AddEntryCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).EntryCommands.Add(cmd);

    /// <summary>Adds a command that fires once when the machine leaves <paramref name="stateId"/>.</summary>
    public void AddExitCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).ExitCommands.Add(cmd);

    /// <summary>Adds a command that fires every frame while the machine is in <paramref name="stateId"/>.</summary>
    public void AddWhileCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).WhileCommands.Add(cmd);

    // Raw bank/id overloads for convenience

    /// <inheritdoc cref="AddEntryCommand(long,long,ESD.CommandCall)"/>
    public void AddEntryCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddEntryCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));

    /// <inheritdoc cref="AddExitCommand(long,long,ESD.CommandCall)"/>
    public void AddExitCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddExitCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));

    /// <inheritdoc cref="AddWhileCommand(long,long,ESD.CommandCall)"/>
    public void AddWhileCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddWhileCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));

    // ── Talk list helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds the B1:19 (AddTalkListData) entry command matching <paramref name="talkId"/>
    /// in <paramref name="stateId"/> and replaces its gate-flag argument with
    /// <paramref name="newGateFlag"/>. Pass <c>-1</c> to remove the gate entirely
    /// (the item is always visible).
    /// Returns <c>true</c> if the entry was found and updated.
    /// <code>
    /// // Remove the event-flag gate on the Level Up menu item:
    /// esd.SetTalkListGateFlag(groupId: 1, stateId: 4, talkId: 15000100, newGateFlag: -1);
    /// </code>
    /// </summary>
    public bool SetTalkListGateFlag(long groupId, long stateId, int talkId, int newGateFlag)
    {
        if (!Esd.StateGroups.TryGetValue(groupId, out var group) ||
            !group.TryGetValue(stateId, out var state))
            return false;

        foreach (var cmd in state.EntryCommands)
        {
            if (cmd.CommandBank != 1 || cmd.CommandID != 19 || cmd.Arguments.Count < 3)
                continue;
            if (DecodeIntArg(cmd.Arguments[1]) != talkId)
                continue;

            cmd.Arguments[2] = EsdBytecode.PushInt(newGateFlag);
            return true;
        }
        return false;
    }

    // Decode a PushInt argument byte array back to its integer value.
    // Handles both compact single-byte form and 5-byte i32 form.
    private static int DecodeIntArg(byte[] arg)
    {
        if (arg.Length == 6 && arg[0] == 0x82)
            return BitConverter.ToInt32(arg, 1);
        if (arg.Length >= 2)
            return arg[0] - 0x40;
        return 0;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// EsdBytecode — evaluator expression factory
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Factory for ESD evaluator bytecode and command-argument expressions.
///
/// ESD uses a 32-bit stack-based VM. Call conventions (DS1/DSR, ground-truth
/// from community research):
/// <list type="bullet">
///   <item>Push small int: opcode = <c>0x40 + value</c> for value in [−64, 62]</item>
///   <item>Push i32: <c>0x82</c> + 4 bytes LE</item>
///   <item>Push f64: <c>0x81</c> + 8 bytes LE</item>
///   <item>Call function N with args: push N (as int), push args, then <c>0x84</c>/0x85/0x86 for 0/1/2 args</item>
///   <item>Compare <c>==</c>: <c>0x95</c></item>
///   <item>Compare <c>!=</c>: <c>0x96</c></item>
///   <item>Compare <c>&gt;=</c>: <c>0x92</c></item>
///   <item>AND: <c>0x98</c> — OR: <c>0x99</c></item>
///   <item>Terminate: <c>0xA1</c></item>
/// </list>
///
/// Every public method returns a <em>complete expression</em> ending with
/// <c>0xA1</c>. Composition helpers (<see cref="And"/>, <see cref="Or"/>,
/// <see cref="Not"/>) strip the trailing terminator of sub-expressions before
/// merging — nesting is safe:
/// <code>
/// // Transition when flag 11810400 is ON AND dialog is closed:
/// byte[] cond = EsdBytecode.And(
///     EsdBytecode.GetEventFlag(11810400),
///     EsdBytecode.Not(EsdBytecode.IsGenericDialogOpen()));
/// </code>
/// </summary>
public static class EsdBytecode
{
    // ── Opcode constants ──────────────────────────────────────────────────────

    private const byte OpPushI32 = 0x82;  // push 4-byte signed integer
    private const byte OpPushF64 = 0x81;  // push 8-byte double
    private const byte OpCall0   = 0x84;  // pop func_id, call with 0 args, push result
    private const byte OpCall1   = 0x85;  // pop func_id + 1 arg, call, push result
    private const byte OpCall2   = 0x86;  // pop func_id + 2 args, call, push result
    private const byte OpGe      = 0x92;  // pop b, pop a, push (a >= b)
    private const byte OpEq      = 0x95;  // pop b, pop a, push (a == b)
    private const byte OpNe      = 0x96;  // pop b, pop a, push (a != b)
    private const byte OpAnd     = 0x98;  // pop b, pop a, push (a && b)
    private const byte OpOr      = 0x99;  // pop b, pop a, push (a || b)
    private const byte OpEnd     = 0xA1;  // terminate; top of stack is the result

    // ── Literals ──────────────────────────────────────────────────────────────

    /// <summary>Evaluator that always passes — pushes 1 (truthy).</summary>
    public static byte[] Always() => [0x41, OpEnd];

    /// <summary>Evaluator that never passes — pushes 0 (falsy).</summary>
    public static byte[] Never()  => [0x40, OpEnd];

    /// <summary>
    /// Push an integer. Uses the compact single-byte encoding for values in
    /// [−64, 62], or a 5-byte i32 for larger values.
    /// </summary>
    public static byte[] PushInt(int value)
    {
        if (value >= -64 && value < 63)
            return [(byte)(0x40 + value), OpEnd];
        return
        [
            OpPushI32,
            (byte)( value        & 0xFF),
            (byte)((value >>  8) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 24) & 0xFF),
            OpEnd,
        ];
    }

    /// <summary>Push a 64-bit double-precision float.</summary>
    public static byte[] PushDouble(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        return
        [
            OpPushF64,
            (byte)( bits        & 0xFF),
            (byte)((bits >>  8) & 0xFF),
            (byte)((bits >> 16) & 0xFF),
            (byte)((bits >> 24) & 0xFF),
            (byte)((bits >> 32) & 0xFF),
            (byte)((bits >> 40) & 0xFF),
            (byte)((bits >> 48) & 0xFF),
            (byte)((bits >> 56) & 0xFF),
            OpEnd,
        ];
    }

    // ── Function calls ────────────────────────────────────────────────────────

    /// <summary>
    /// Call condition function <paramref name="funcId"/> with no arguments.
    /// Push the result.
    /// </summary>
    public static byte[] CallFunc0(int funcId) =>
        [.. PushId(funcId), OpCall0, OpEnd];

    /// <summary>
    /// Call condition function <paramref name="funcId"/> with one argument.
    /// Push the result.
    /// </summary>
    public static byte[] CallFunc1(int funcId, byte[] arg) =>
        [.. PushId(funcId), .. Strip(arg), OpCall1, OpEnd];

    /// <summary>
    /// Call condition function <paramref name="funcId"/> with two arguments.
    /// Push the result.
    /// </summary>
    public static byte[] CallFunc2(int funcId, byte[] arg1, byte[] arg2) =>
        [.. PushId(funcId), .. Strip(arg1), .. Strip(arg2), OpCall2, OpEnd];

    // ── Logical composition ───────────────────────────────────────────────────

    /// <summary>Passes when both <paramref name="a"/> and <paramref name="b"/> pass.</summary>
    public static byte[] And(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpAnd, OpEnd];

    /// <summary>Passes when either <paramref name="a"/> or <paramref name="b"/> passes.</summary>
    public static byte[] Or(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpOr, OpEnd];

    /// <summary>Inverts a complete expression (equivalent to <c>expr == false</c>).</summary>
    public static byte[] Not(byte[] a) =>
        [.. Strip(a), 0x40, OpEq, OpEnd];

    // ── Comparisons ───────────────────────────────────────────────────────────

    /// <summary>Passes when <paramref name="a"/> == <paramref name="b"/>.</summary>
    public static byte[] Eq(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpEq, OpEnd];

    /// <summary>Passes when <paramref name="a"/> != <paramref name="b"/>.</summary>
    public static byte[] Ne(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpNe, OpEnd];

    /// <summary>Passes when <paramref name="a"/> >= <paramref name="b"/>.</summary>
    public static byte[] Ge(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpGe, OpEnd];

    // ── Named condition functions (DS1/DSR, confirmed) ────────────────────────

    /// <summary>
    /// Check event flag state — condition function 15 (DS1/DSR).
    /// Returns 1 if the flag is ON, 0 if OFF.
    /// <code>
    /// // Passes when flag 11810400 is ON:
    /// EsdBytecode.GetEventFlag(11810400)
    ///
    /// // Passes when flag is OFF:
    /// EsdBytecode.Not(EsdBytecode.GetEventFlag(11810400))
    /// </code>
    /// </summary>
    public static byte[] GetEventFlag(int flagId) =>
        CallFunc1(15, PushInt(flagId));

    /// <summary>
    /// Check whether a generic dialog box is currently open — condition
    /// function 58 (DS1/DSR). Pass 0 to match any NPC; pass an entity ID
    /// to match a specific one. Returns 1 if open, 0 if closed.
    /// </summary>
    public static byte[] IsGenericDialogOpen(int personId = 0) =>
        CallFunc1(58, PushInt(personId));

    /// <summary>
    /// Get which button the player pressed in an open generic dialog —
    /// condition function 22 (DS1/DSR). Compare with a button index:
    /// <code>
    /// // Passes when the player chose option 1:
    /// EsdBytecode.Eq(EsdBytecode.GetDialogButtonResult(), EsdBytecode.PushInt(1))
    /// </code>
    /// </summary>
    public static byte[] GetDialogButtonResult() =>
        CallFunc0(22);

    /// <summary>
    /// Passes the instant a generic dialog box just closed AND the player's
    /// answer matches <paramref name="button"/> — condition function 58 (open
    /// check) combined with function 22 (button result). This is the exact
    /// pattern DSR itself uses to detect dialog answers (confirmed from the
    /// vanilla bonfire warp-confirmation flow):
    /// <c>!IsGenericDialogOpen(personId) &amp;&amp; GetDialogButtonResult() == button</c>
    /// <code>
    /// // Branch to the "confirmed" state once the player presses Yes (button 1):
    /// esd.AddTransition(1, waitState, confirmedState,
    ///     EsdBytecode.DialogClosedWithButton(1));
    /// </code>
    /// </summary>
    public static byte[] DialogClosedWithButton(int button, int personId = 0) =>
        And(Not(IsGenericDialogOpen(personId)), Eq(GetDialogButtonResult(), PushInt(button)));

    /// <summary>
    /// Get the elapsed time, in seconds, the state machine has spent in its
    /// current state — condition function 103 (DS1/DSR; confirmed from the
    /// vanilla bonfire warp failsafe, which restarts the flow via
    /// <c>GetTimeInState() &gt;= 5</c> if the warp dialog never resolves).
    ///
    /// Useful for timeouts, idle triggers, or "do X after N seconds" beats:
    /// <code>
    /// // Auto-advance after 8 seconds of no input:
    /// esd.AddTransition(1, waitState, timeoutState,
    ///     EsdBytecode.Ge(EsdBytecode.GetTimeInState(), EsdBytecode.PushInt(8)));
    /// </code>
    /// </summary>
    public static byte[] GetTimeInState() => CallFunc0(103);

    /// <summary>
    /// Get the index of the currently highlighted/selected bonfire or NPC talk
    /// menu item — condition function 23 (DS1/DSR). Returns the
    /// <c>listIndex</c> value that was used in the matching
    /// <see cref="TalkCmd.AddTalkListData"/> call.
    ///
    /// Use with <see cref="Eq"/> or the shorthand <see cref="SelectedItem"/>
    /// to dispatch each menu choice to its handler state:
    /// <code>
    /// esd.AddTransition(1, menuState, levelUpState,
    ///     EsdBytecode.SelectedItem(listIndex: 1));
    /// </code>
    /// </summary>
    public static byte[] GetMenuSelection() => CallFunc0(23);

    /// <summary>
    /// Passes when the player selected talk list item <paramref name="listIndex"/>.
    /// Shorthand for <c>Eq(GetMenuSelection(), PushInt(listIndex))</c>.
    /// </summary>
    public static byte[] SelectedItem(int listIndex) =>
        Eq(GetMenuSelection(), PushInt(listIndex));

    // ── Escape hatch ──────────────────────────────────────────────────────────

    /// <summary>
    /// Build a complete expression from space-separated hex bytes, appending
    /// <c>0xA1</c> if not already present. Use to paste raw bytecode from
    /// ESDLang, Yabber, or a hex editor.
    /// <code>
    /// EsdBytecode.FromHex("4F 82 01 00 00 00 85 41 95 A1"); // GetEventFlag(1) == true
    /// </code>
    /// </summary>
    public static byte[] FromHex(string hex)
    {
        var bytes = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Select(h => Convert.ToByte(h, 16))
                       .ToArray();
        return bytes.Length > 0 && bytes[^1] == OpEnd ? bytes : [.. bytes, OpEnd];
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    // Strip the trailing terminator so expressions can be composed.
    private static byte[] Strip(byte[] expr) =>
        expr.Length > 0 && expr[^1] == OpEnd ? expr[..^1] : expr;

    // Emit the push for a function ID — no trailing OpEnd (used for call composition).
    private static byte[] PushId(int id)
    {
        if (id >= -64 && id < 63)
            return [(byte)(0x40 + id)];
        return [OpPushI32, (byte)(id & 0xFF), (byte)((id >> 8) & 0xFF), (byte)((id >> 16) & 0xFF), (byte)((id >> 24) & 0xFF)];
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TalkCmd — named command factory for DS1/DSR talk ESD (Bank 1 + Bank 5)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Factory for named <see cref="ESD.CommandCall"/> instances for the DS1/DSR
/// talk ESD command set (confirmed from binary analysis of DSR talkesdbnd files).
///
/// Pass the returned <see cref="ESD.CommandCall"/> to
/// <see cref="EsdEditor.AddEntryCommand"/>,
/// <see cref="EsdEditor.AddExitCommand"/>, or
/// <see cref="EsdEditor.AddWhileCommand"/>.
///
/// <para><b>Command argument encoding:</b> every argument is a complete
/// <see cref="EsdBytecode"/> expression — use <see cref="EsdBytecode.PushInt"/>,
/// <see cref="EsdBytecode.PushDouble"/>, etc. to build them. The helper
/// methods here do this for you.</para>
/// </summary>
public static class TalkCmd
{
    // ── Event flags ───────────────────────────────────────────────────────────

    /// <summary>
    /// B1:11 — Set an event flag ON or OFF.
    /// This is the most common command in DS1 talk ESDs (confirmed, 3187×).
    /// <code>
    /// // On entering state 10, set flag 11810400 ON:
    /// esd.AddEntryCommand(1, 10, TalkCmd.SetEventFlag(11810400, on: true));
    /// </code>
    /// </summary>
    public static ESD.CommandCall SetEventFlag(int flagId, bool on) =>
        new(1, 11, EsdBytecode.PushInt(flagId), EsdBytecode.PushInt(on ? 1 : 0));

    // ── Dialog ────────────────────────────────────────────────────────────────

    /// <summary>
    /// B1:17 — Open a generic dialog box (yes/no prompt, message with buttons).
    /// <paramref name="dialogType"/> controls the visual style (8 = standard).
    /// <paramref name="messageId"/> is the FMG text entry to display.
    /// <paramref name="buttonType"/> and <paramref name="numButtons"/> control button layout.
    /// Compare the result with <see cref="EsdBytecode.GetDialogButtonResult"/>.
    /// </summary>
    public static ESD.CommandCall OpenGenericDialog(
        int dialogType, int messageId, int buttonType, int numButtons, int unk = 2) =>
        new(1, 17,
            EsdBytecode.PushInt(dialogType),
            EsdBytecode.PushInt(messageId),
            EsdBytecode.PushInt(buttonType),
            EsdBytecode.PushInt(numButtons),
            EsdBytecode.PushInt(unk));

    // ── Talk list ─────────────────────────────────────────────────────────────

    /// <summary>
    /// B1:19 — Add an entry to the bonfire/NPC talk list menu.
    /// This is the standard form used in vanilla bonfire ESDs.
    ///
    /// <paramref name="listIndex"/> is the menu slot (0-based); the value returned
    /// by <see cref="EsdBytecode.GetMenuSelection"/> when the player picks this item.
    /// <paramref name="talkId"/> is the FMG text entry ID for the menu label.
    /// <paramref name="gateFlag"/> is the event flag that must be ON for the item
    /// to appear — pass <c>-1</c> (default) to always show it regardless of flags.
    ///
    /// <code>
    /// // Always-visible Level Up option:
    /// esd.AddEntryCommand(1, menuState, TalkCmd.AddTalkListData(1, 15000100));
    ///
    /// // Option visible only after flag 11810000 is set:
    /// esd.AddEntryCommand(1, menuState, TalkCmd.AddTalkListData(1, 15000100, gateFlag: 11810000));
    /// </code>
    /// </summary>
    public static ESD.CommandCall AddTalkListData(int listIndex, int talkId, int gateFlag = -1) =>
        new(1, 19, EsdBytecode.PushInt(listIndex), EsdBytecode.PushInt(talkId), EsdBytecode.PushInt(gateFlag));

    /// <summary>
    /// B5:19 — Conditionally add an entry to the NPC talk menu list using a
    /// full bytecode condition expression as the gate (FogMod / warp menu style).
    /// For simpler flag-based gating, prefer <see cref="AddTalkListData"/>.
    ///
    /// <paramref name="condition"/> is a complete evaluator expression; the entry
    /// is only shown when it passes.
    /// <paramref name="listIndex"/> is the menu slot (0-based).
    /// <paramref name="talkId"/> is the FMG text entry for the menu item label.
    /// </summary>
    public static ESD.CommandCall AddTalkListDataIf(
        byte[] condition, int listIndex, int talkId, int unk = 0) =>
        new(5, 19, condition, EsdBytecode.PushInt(listIndex), EsdBytecode.PushInt(talkId), EsdBytecode.PushInt(unk));

    // ── Misc (Bank 1, DS1, confirmed presence in DSR talk ESDs) ─────────────

    /// <summary>
    /// B1:10 — Show a shop/wares message. Commonly paired with states that
    /// open vendor menus.
    /// </summary>
    public static ESD.CommandCall ShowShopMessage() =>
        new(1, 10);

    /// <summary>
    /// B1:101 — Update the player's respawn bonfire. Used in bonfire talk ESDs.
    /// </summary>
    public static ESD.CommandCall UpdateRespawnPoint(int bonfireEntityId) =>
        new(1, 101, EsdBytecode.PushInt(bonfireEntityId));

    /// <summary>
    /// B1:11 raw — raw SetEventFlag with pre-encoded argument bytes.
    /// Useful when copying arguments verbatim from an existing ESD condition
    /// or from ESDLang output.
    /// </summary>
    public static ESD.CommandCall SetEventFlagRaw(byte[] flagIdBytes, byte[] stateBytes) =>
        new(1, 11, flagIdBytes, stateBytes);
}
