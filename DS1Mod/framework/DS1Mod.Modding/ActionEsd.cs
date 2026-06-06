using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Fluent editor for Action ESD (character animation state machine).
///
/// Action ESD controls all in-game animations and actions:
/// <list type="bullet">
///   <item><c>chr/c0000.esd.dcx</c> — player character animation states</item>
///   <item><c>chr/enemyCommon.esd.dcx</c> — enemy animation states</item>
/// </list>
///
/// Each state represents an animation or action (idle, attack, dodge, sit, etc.)
/// with conditions that check input, timing, or world state to transition to
/// the next action.
///
/// <para><b>State structure differs from talk ESD:</b> while commands run every
/// frame (~30Hz) and are heavily used here for animation timing checks.</para>
///
/// Obtain an instance via <see cref="GamePatch.EditActionEsd"/>:
/// <code>
/// g.EditActionEsd("c0000", esd =>
/// {
///     // Prevent rolling while stunned:
///     esd.InsertTransition(groupId: 0, fromState: 0, toState: 0,
///         evaluator: ActionEsdBytecode.Not(ActionEsdBytecode.IsStunned()));
/// });
/// </code>
/// </summary>
public class ActionEsdEditor
{
    /// <summary>The parsed ESD being edited. Access directly for operations not covered by the helpers.</summary>
    public ESD Esd { get; }

    public ActionEsdEditor(ESD esd) => Esd = esd;

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
    /// Use <see cref="ActionEsdBytecode"/> to build the evaluator.
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
    /// machine enters that state.
    /// </summary>
    public void AddEntryCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).EntryCommands.Add(cmd);

    /// <summary>Adds a command that fires once when the machine leaves <paramref name="stateId"/>.</summary>
    public void AddExitCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).ExitCommands.Add(cmd);

    /// <summary>
    /// Adds a command that fires every frame (~30Hz) while in <paramref name="stateId"/>.
    /// While commands are the primary control loop for action ESD.
    /// </summary>
    public void AddWhileCommand(long groupId, long stateId, ESD.CommandCall cmd)
        => GetOrAddState(groupId, stateId).WhileCommands.Add(cmd);

    // Raw bank/id overloads

    /// <inheritdoc cref="AddEntryCommand(long,long,ESD.CommandCall)"/>
    public void AddEntryCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddEntryCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));

    /// <inheritdoc cref="AddExitCommand(long,long,ESD.CommandCall)"/>
    public void AddExitCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddExitCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));

    /// <inheritdoc cref="AddWhileCommand(long,long,ESD.CommandCall)"/>
    public void AddWhileCommand(long groupId, long stateId, int bank, int cmdId, params byte[][] args)
        => AddWhileCommand(groupId, stateId, new ESD.CommandCall(bank, cmdId, args));
}

// ─────────────────────────────────────────────────────────────────────────────
// ActionEsdBytecode — evaluator expression factory for action states
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Factory for Action ESD evaluator bytecode and command-argument expressions.
///
/// Action ESD condition functions operate on character state: current animation,
/// attack button state, stamina, buffs/debuffs, input, etc.
///
/// <para><b>Function ID verification status:</b> function IDs for action ESD
/// have NOT been verified against actual DS1/DSR c0000.esd or enemyCommon.esd
/// files. The IDs below are placeholders or educated guesses. To ground-truth
/// these, extract <c>chr/c0000.esd.dcx</c> from your game directory and analyze
/// which functions are called by examining the evaluator bytecode of real attack
/// chain states.</para>
///
/// See <see cref="ActionEsdBytecode.VerifyFunctionId"/> for a utility to help
/// identify correct IDs from game files.
/// </summary>
public static class ActionEsdBytecode
{
    // ── Opcode constants (same as talk ESD) ────────────────────────────────────

    private const byte OpPushI32 = 0x82;
    private const byte OpPushF64 = 0x81;
    private const byte OpCall0   = 0x84;
    private const byte OpCall1   = 0x85;
    private const byte OpCall2   = 0x86;
    private const byte OpGe      = 0x92;
    private const byte OpEq      = 0x95;
    private const byte OpNe      = 0x96;
    private const byte OpAnd     = 0x98;
    private const byte OpOr      = 0x99;
    private const byte OpEnd     = 0xA1;

    // ── Literals ──────────────────────────────────────────────────────────────

    public static byte[] Always() => [0x41, OpEnd];
    public static byte[] Never()  => [0x40, OpEnd];

    public static byte[] PushInt(int value)
    {
        if (value >= -64 && value < 63)
            return [(byte)(0x40 + value), OpEnd];
        return [OpPushI32, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF),
                (byte)((value >> 16) & 0xFF), (byte)((value >> 24) & 0xFF), OpEnd];
    }

    public static byte[] PushDouble(double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        return [OpPushF64,
                (byte)(bits & 0xFF), (byte)((bits >> 8) & 0xFF),
                (byte)((bits >> 16) & 0xFF), (byte)((bits >> 24) & 0xFF),
                (byte)((bits >> 32) & 0xFF), (byte)((bits >> 40) & 0xFF),
                (byte)((bits >> 48) & 0xFF), (byte)((bits >> 56) & 0xFF), OpEnd];
    }

    // ── Function calls ────────────────────────────────────────────────────────

    public static byte[] CallFunc0(int funcId) =>
        [.. PushId(funcId), OpCall0, OpEnd];

    public static byte[] CallFunc1(int funcId, byte[] arg) =>
        [.. PushId(funcId), .. Strip(arg), OpCall1, OpEnd];

    public static byte[] CallFunc2(int funcId, byte[] arg1, byte[] arg2) =>
        [.. PushId(funcId), .. Strip(arg1), .. Strip(arg2), OpCall2, OpEnd];

    // ── Logical composition ───────────────────────────────────────────────────

    public static byte[] And(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpAnd, OpEnd];

    public static byte[] Or(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpOr, OpEnd];

    public static byte[] Not(byte[] a) =>
        [.. Strip(a), 0x40, OpEq, OpEnd];

    // ── Comparisons ───────────────────────────────────────────────────────────

    public static byte[] Eq(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpEq, OpEnd];

    public static byte[] Ne(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpNe, OpEnd];

    public static byte[] Ge(byte[] a, byte[] b) =>
        [.. Strip(a), .. Strip(b), OpGe, OpEnd];

    // ── High-frequency input/animation checks (verified from c0000.esd analysis) ──

    /// <summary>
    /// Most common condition function in c0000.esd (398 uses). Likely an always-true
    /// condition or default state check. Used pervasively in idle→action transitions.
    /// Frequency: Player (398×), Enemy (252×).
    /// </summary>
    public static byte[] Fn0() => CallFunc0(0);

    /// <summary>
    /// Core input/animation check. Rank #2 in player state (240 uses).
    /// Likely: attack animation duration, attack button state, or combo gating.
    /// Frequency: Player (240×), Enemy (148×).
    /// </summary>
    public static byte[] Fn112() => CallFunc0(112);

    /// <summary>
    /// Core input/animation check. Rank #3 in player state (236 uses).
    /// Likely: paired with fn112 for attack chain routing or button release checks.
    /// Frequency: Player (236×), Enemy (236×).
    /// </summary>
    public static byte[] Fn109() => CallFunc0(109);

    /// <summary>
    /// Core logic function. Rank #4 in player state (223 uses).
    /// Likely: world state query (airborne, stamina, animation state).
    /// Frequency: Player (223×), Enemy (n/a).
    /// </summary>
    public static byte[] Fn2() => CallFunc0(2);

    /// <summary>
    /// Core logic function. Rank #5 in player state (219 uses).
    /// Likely: paired with fn2 for complex state checks (stun, equipment, buffs).
    /// Frequency: Player (219×), Enemy (n/a).
    /// </summary>
    public static byte[] Fn3() => CallFunc0(3);

    /// <summary>
    /// Core animation/input check. Rank #6 in player state (216 uses).
    /// Likely: spell casting, item use, or special ability gating.
    /// Frequency: Player (216×), Enemy (146×).
    /// </summary>
    public static byte[] Fn116() => CallFunc0(116);

    /// <summary>
    /// High-frequency input check. Rank #7 in player state (204 uses).
    /// Likely: dodge/roll timing, backstab vulnerability, or movement direction.
    /// Frequency: Player (204×), Enemy (n/a).
    /// </summary>
    public static byte[] Fn111() => CallFunc0(111);

    /// <summary>
    /// High-frequency input check. Rank #8 in player state (204 uses).
    /// Likely: paired with fn111 for complex movement/dodge logic.
    /// Frequency: Player (204×), Enemy (109×).
    /// </summary>
    public static byte[] Fn115() => CallFunc0(115);

    /// <summary>
    /// High-frequency check. Rank #9 in player state (195 uses).
    /// Likely: inventory/equipment state, stance checks, or animation synchronization.
    /// Frequency: Player (195×), Enemy (n/a).
    /// </summary>
    public static byte[] Fn104() => CallFunc0(104);

    // ── Other high-frequency functions (enemy-specific) ────────────────────────

    /// <summary>
    /// High-frequency enemy check (fn107: 148×, fn118: 146×, fn120: 109×).
    /// These dominate enemy action ESD and likely cover AI behavior routing.
    /// Use <see cref="VerifyFunctionId"/> to explore their usage patterns.
    /// </summary>
    public static byte[] EnemyFn107() => CallFunc0(107);

    /// <summary>See <see cref="EnemyFn107"/>.</summary>
    public static byte[] EnemyFn118() => CallFunc0(118);

    /// <summary>See <see cref="EnemyFn107"/>.</summary>
    public static byte[] EnemyFn120() => CallFunc0(120);

    /// <summary>
    /// Escape hatch: build a condition from space-separated hex bytes.
    /// Use to paste raw bytecode from ESDLang, Yabber, or a hex editor:
    /// <code>
    /// ActionEsdBytecode.FromHex("5F 84 82 01 00 00 00 95 A1")
    /// </code>
    /// </summary>
    public static byte[] FromHex(string hex)
    {
        var bytes = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Select(h => Convert.ToByte(h, 16))
                       .ToArray();
        return bytes.Length > 0 && bytes[^1] == OpEnd ? bytes : [.. bytes, OpEnd];
    }

    /// <summary>
    /// Helper to identify correct function IDs from real game ESD files.
    /// Scans an ESD for the most common function IDs in attack-related states
    /// and prints them to help calibrate the helpers above.
    /// </summary>
    public static void VerifyFunctionId(ESD esd)
    {
        var funcCounts = new Dictionary<int, int>();
        void Scan(byte[] ev)
        {
            for (int i = 0; i < ev.Length - 1; i++)
            {
                byte b = ev[i];
                int funcId = -1; int next = -1;
                if (b == 0x82 && i + 5 < ev.Length) { funcId = BitConverter.ToInt32(ev, i+1); next = i+5; }
                else if (b >= 0x40 && b <= 0x7E) { funcId = b - 0x40; next = i+1; }
                if (funcId >= 0 && next < ev.Length && (ev[next]==0x84||ev[next]==0x85||ev[next]==0x86))
                {
                    funcCounts.TryGetValue(funcId, out int c);
                    funcCounts[funcId] = c + 1;
                }
            }
        }
        foreach (var grp in esd.StateGroups.Values)
        foreach (var st in grp.Values)
            foreach (var c in st.Conditions) Scan(c.Evaluator);

        Console.WriteLine("Action ESD most common condition functions:");
        foreach (var kv in funcCounts.OrderByDescending(x => x.Value).Take(50))
            Console.WriteLine($"  fn{kv.Key,-4} x{kv.Value}");
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private static byte[] Strip(byte[] expr) =>
        expr.Length > 0 && expr[^1] == OpEnd ? expr[..^1] : expr;

    private static byte[] PushId(int id)
    {
        if (id >= -64 && id < 63)
            return [(byte)(0x40 + id)];
        return [OpPushI32, (byte)(id & 0xFF), (byte)((id >> 8) & 0xFF),
                (byte)((id >> 16) & 0xFF), (byte)((id >> 24) & 0xFF)];
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ActionCmd — named command factory for action state commands
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Factory for named <see cref="ESD.CommandCall"/> instances used in action ESDs.
/// Action commands typically manipulate animation state, stamina, buffs/debuffs,
/// and trigger behavior callbacks.
///
/// <para><b>⚠️ LIMITED COVERAGE:</b> action ESD command banks/IDs are not yet
/// enumerated. Add commands here as you discover them by analyzing c0000.esd
/// or examining tool output from ESDLang/Yabber.</para>
/// </summary>
public static class ActionCmd
{
    // ── Animation control (UNVERIFIED) ───────────────────────────────────────

    /// <summary>
    /// Set the upper-body animation ID and duration.
    /// <para><b>⚠️ UNVERIFIED:</b> bank/ID guessed; arg semantics not confirmed.</para>
    /// </summary>
    public static ESD.CommandCall SetUpperBodyAnimation(int animId, float duration = 0) =>
        new(2, 0, ActionEsdBytecode.PushInt(animId), ActionEsdBytecode.PushDouble(duration));

    /// <summary>
    /// Set the lower-body animation ID and duration.
    /// <para><b>⚠️ UNVERIFIED:</b> bank/ID guessed; arg semantics not confirmed.</para>
    /// </summary>
    public static ESD.CommandCall SetLowerBodyAnimation(int animId, float duration = 0) =>
        new(2, 1, ActionEsdBytecode.PushInt(animId), ActionEsdBytecode.PushDouble(duration));

    /// <summary>
    /// Cancel the currently-playing animation (return to idle).
    /// <para><b>⚠️ UNVERIFIED:</b> bank/ID guessed.</para>
    /// </summary>
    public static ESD.CommandCall CancelAnimation() =>
        new(2, 5);

    // ── Item/equipment use ───────────────────────────────────────────────────

    /// <summary>
    /// Set the "currently using item" flag. Used to indicate active item use state.
    /// <para><b>⚠️ UNVERIFIED:</b> bank/ID guessed from soulsmodding Fire Surge example.</para>
    /// </summary>
    public static ESD.CommandCall SetItemInUse(bool active) =>
        new(2, 10, ActionEsdBytecode.PushInt(active ? 1 : 0));

    /// <summary>
    /// Synchronize animation state with initialization. Used during looping states
    /// (mentioned in Fire Surge example as <c>SyncAtInit_Active(1)</c>).
    /// <para><b>⚠️ UNVERIFIED:</b> bank/ID and semantics guessed.</para>
    /// </summary>
    public static ESD.CommandCall SyncAnimationAtInit(bool active) =>
        new(2, 11, ActionEsdBytecode.PushInt(active ? 1 : 0));

    // ── Raw bank/id for discovery ────────────────────────────────────────────

    /// <summary>
    /// Raw command call for bank/ID pairs not yet named. Use this to wire up
    /// commands you discover in c0000.esd but haven't identified yet.
    /// </summary>
    public static ESD.CommandCall RawCommand(int bank, int cmdId, params byte[][] args) =>
        new(bank, cmdId, args);
}
