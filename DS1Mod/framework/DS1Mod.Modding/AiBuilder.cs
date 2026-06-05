using System.Diagnostics;
using System.Text;

namespace DS1Mod.Modding;

// ── Target / Dist / GoalResult enums ──────────────────────────────────────────

/// <summary>Target selector constants from <c>ai_define.lua</c>.</summary>
public enum Target
{
    None         = -2,
    Self         = -1,
    Enemy0       = 0,
    Friend0      = 10,
    Event        = 20,
    LocalPlayer  = 21,
}

/// <summary>Distance constants from <c>ai_define.lua</c>.</summary>
public enum Dist
{
    Near   = -1,
    Middle = -2,
    Far    = -3,
    Out    = -4,
    None   = -5,
}

// ── SubGoalQueue ──────────────────────────────────────────────────────────────

/// <summary>
/// Fluent queue of AddSubGoal calls for one act or OnActivate body.
/// </summary>
public sealed class SubGoalQueue
{
    internal readonly List<string> Lines = new();

    /// <summary>
    /// Approach a target to within <paramref name="distMeters"/> world-units.
    /// <para>IMPORTANT: pass a positive float (e.g. 8.0) — the DS1 engine
    /// interprets the distance argument of GOAL_COMMON_ApproachTarget as a
    /// literal world-unit radius, not a DIST_* sentinel. Passing a DIST_*
    /// constant (-1/-2/-3…) would make the demon try to reach a negative
    /// distance, which never terminates.</para>
    /// </summary>
    public SubGoalQueue ApproachTarget(Target target = Target.Enemy0,
        float distMeters = 8f, int cancelTime = 10, bool walk = false)
    {
        string walkStr = walk ? "true" : "false";
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_ApproachTarget, {cancelTime}," +
            $" {LuaTarget(target)}, {distMeters.ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture)}, TARGET_SELF, {walkStr}, -1)");
        return this;
    }

    /// <summary>Play an attack animation (<paramref name="animId"/>) then wait.</summary>
    public SubGoalQueue Attack(int animId, int cancelTime = 10,
        Target target = Target.Enemy0, Dist dist = Dist.Middle)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_Attack, {cancelTime}," +
            $" {animId}, {LuaTarget(target)}, {LuaDist(dist)}, 0)");
        return this;
    }

    /// <summary>Play an attack then chain into a combo follow-up.</summary>
    public SubGoalQueue ComboAttack(int animId, int cancelTime = 10,
        Target target = Target.Enemy0, Dist dist = Dist.Middle)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_ComboAttack, {cancelTime}," +
            $" {animId}, {LuaTarget(target)}, {LuaDist(dist)}, 0)");
        return this;
    }

    /// <summary>Final hit of a combo.</summary>
    public SubGoalQueue ComboFinal(int animId, int cancelTime = 10,
        Target target = Target.Enemy0, Dist dist = Dist.Middle)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_ComboFinal, {cancelTime}," +
            $" {animId}, {LuaTarget(target)}, {LuaDist(dist)}, 0)");
        return this;
    }

    /// <summary>Spin-attack (used for axe/greataxe slam style moves).</summary>
    public SubGoalQueue TunableSpinAttack(int animId, int cancelTime = 10,
        Target target = Target.Enemy0, Dist dist = Dist.Middle)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_AttackTunableSpin, {cancelTime}," +
            $" {animId}, {LuaTarget(target)}, {LuaDist(dist)}, 0, -1)");
        return this;
    }

    /// <summary>Dash into an attack.</summary>
    public SubGoalQueue DashAttack(int animId, int cancelTime = 10,
        Target target = Target.Enemy0, Dist dist = Dist.Middle)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_DashAttack, {cancelTime}," +
            $" {animId}, {LuaTarget(target)}, {LuaDist(dist)}, 0)");
        return this;
    }

    /// <summary>Guard posture (block).</summary>
    public SubGoalQueue Guard(int cancelTime = 5)
    {
        Lines.Add($"    goal:AddSubGoal(GOAL_COMMON_Guard, {cancelTime}, TARGET_ENE_0, 0, 0, 0)");
        return this;
    }

    /// <summary>Sidestep / spin-step evasion.</summary>
    public SubGoalQueue SpinStep(int animId = 701, int cancelTime = 5)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_SpinStep, {cancelTime}," +
            $" {animId}, TARGET_ENE_0, 0, AI_DIR_TYPE_B, 6)");
        return this;
    }

    /// <summary>Idle wait for a fixed duration.</summary>
    public SubGoalQueue Wait(int cancelTime = 3)
    {
        Lines.Add($"    goal:AddSubGoal(GOAL_COMMON_Wait, {cancelTime}, TARGET_ENE_0, 0, 0, 0)");
        return this;
    }

    /// <summary>Idle wait for a random duration between <paramref name="minTime"/> and <paramref name="maxTime"/>.</summary>
    public SubGoalQueue WaitRandom(float minTime, float maxTime)
    {
        Lines.Add($"    goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float({F(minTime)}, {F(maxTime)}), TARGET_ENE_0, 0, 0, 0)");
        return this;
    }

    /// <summary>Turn to face a target.</summary>
    public SubGoalQueue Turn(Target target = Target.Enemy0, int cancelTime = 2)
    {
        Lines.Add($"    goal:AddSubGoal(GOAL_COMMON_Turn, {cancelTime}, {LuaTarget(target)}, 0, 0, 0)");
        return this;
    }

    /// <summary>Return to initial spawn position and wait.</summary>
    public SubGoalQueue BackToHome(int cancelTime = 5)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_ApproachTarget, {cancelTime}," +
            $" POINT_INITIAL, 2, TARGET_SELF, true, -1)");
        return this;
    }

    /// <summary>Sideway shuffle (evasion / positioning). direction: 0=left, 1=right.</summary>
    public SubGoalQueue SidewayMove(Target target = Target.Enemy0,
        int direction = 0, int cancelTime = 2)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_SidewayMove, {cancelTime}," +
            $" {LuaTarget(target)}, {direction}, 20, true, true, -1)");
        return this;
    }

    /// <summary>
    /// Back away from target until at least <paramref name="distMeters"/> world-units away.
    /// <para>IMPORTANT: pass a positive float — same reason as <see cref="ApproachTarget"/>.</para>
    /// </summary>
    public SubGoalQueue LeaveTarget(Target target = Target.Enemy0,
        float distMeters = 10f, int cancelTime = 8)
    {
        Lines.Add(
            $"    goal:AddSubGoal(GOAL_COMMON_LeaveTarget, {cancelTime}," +
            $" {LuaTarget(target)}, {distMeters.ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture)}, TARGET_ENE_0, true, -1)");
        return this;
    }

    /// <summary>
    /// Set an event flag from within the AI script.
    /// Use this to broadcast state back to C# (e.g. mood indicators readable via
    /// <c>IModContext.Reader.GetEventFlag</c>).
    /// </summary>
    public SubGoalQueue SetEventFlag(int flagId, bool on)
    {
        Lines.Add($"    ai:SetEventFlag({flagId}, {(on ? "true" : "false")})");
        return this;
    }

    /// <summary>
    /// Clear a contiguous block of <paramref name="count"/> flags starting at
    /// <paramref name="baseFlag"/>, then set the one at index <paramref name="active"/>.
    /// Emits a Lua for-loop — ideal for "mood" patterns where exactly one flag is active.
    /// </summary>
    public SubGoalQueue SetActiveFlagInRange(int baseFlag, int count, int active)
    {
        Lines.Add($"    for _i = 0, {count - 1} do ai:SetEventFlag({baseFlag} + _i, false) end");
        Lines.Add($"    ai:SetEventFlag({baseFlag + active}, true)");
        return this;
    }

    /// <summary>
    /// Raw Lua line — use as an escape hatch when no named method covers your case.
    /// <para>The line is appended as-is inside the act function body (with 4-space indent).</para>
    /// </summary>
    public SubGoalQueue Raw(string luaLine)
    {
        Lines.Add("    " + luaLine);
        return this;
    }

    private static string F(float v) => v.ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture);

    private static string LuaTarget(Target t) => t switch
    {
        Target.None        => "TARGET_NONE",
        Target.Self        => "TARGET_SELF",
        Target.Enemy0      => "TARGET_ENE_0",
        Target.Friend0     => "TARGET_FRI_0",
        Target.Event       => "TARGET_EVENT",
        Target.LocalPlayer => "TARGET_LOCALPLAYER",
        _                  => ((int)t).ToString(),
    };

    private static string LuaDist(Dist d) => d switch
    {
        Dist.Near   => "DIST_Near",
        Dist.Middle => "DIST_Middle",
        Dist.Far    => "DIST_Far",
        Dist.Out    => "DIST_Out",
        Dist.None   => "DIST_None",
        _           => ((int)d).ToString(),
    };
}

// ── Act ───────────────────────────────────────────────────────────────────────

/// <summary>
/// One weighted attack act — a body (subgoal queue) and a relative weight
/// for the random-selection table.
/// </summary>
internal sealed class Act
{
    public int Weight { get; }
    public SubGoalQueue Queue { get; }
    public int GetWellSpaceOdds { get; }

    public Act(int weight, SubGoalQueue queue, int getWellSpaceOdds = 100)
    {
        Weight             = weight;
        Queue              = queue;
        GetWellSpaceOdds   = getWellSpaceOdds;
    }
}

// ── GoalBuilder ───────────────────────────────────────────────────────────────

/// <summary>
/// Fluent builder for one AI goal (a <c>Battle</c> or <c>Idle</c> function set).
/// Obtain via <see cref="AiBuilder.Goal"/>.
/// </summary>
public sealed class GoalBuilder
{
    internal readonly string Name;
    private readonly List<Act> _acts = new();
    private bool _canInterrupt = false;
    private SubGoalQueue? _singleActivate;
    internal readonly List<(string FnName, string Body)> Helpers = new();

    internal GoalBuilder(string name) => Name = name;

    /// <summary>
    /// Define a named Lua helper function that will be emitted before the goal functions.
    /// Use this for shared logic called from multiple acts (e.g. clearing a flag range).
    /// <para>The body is the function body — do not include the <c>function</c> declaration.</para>
    /// <code>
    /// goal.Helper("ClearMoods", "for i=0,9 do ai:SetEventFlag(11815700+i,false) end")
    /// // emits: function ClearMoods(ai, goal) ... end
    /// </code>
    /// </summary>
    public GoalBuilder Helper(string fnName, string body)
    {
        Helpers.Add((fnName, body));
        return this;
    }

    /// <summary>
    /// Define a single sequential behaviour for OnActivate.
    /// Use this when you want one deterministic attack sequence rather than
    /// a weighted random selection.
    /// </summary>
    public GoalBuilder OnActivate(Action<SubGoalQueue> build)
    {
        var q = new SubGoalQueue();
        build(q);
        _singleActivate = q;
        return this;
    }

    /// <summary>
    /// Add a weighted attack act. Each call adds one entry to the random-selection
    /// table. Weights are relative (they don't have to sum to 100).
    /// <para>Example — 70% slam, 30% stomp:</para>
    /// <code>
    /// goal.Act(70, q => q.ApproachTarget().Attack(3008))
    ///     .Act(30, q => q.ApproachTarget().Attack(3000));
    /// </code>
    /// </summary>
    public GoalBuilder Act(int weight, Action<SubGoalQueue> build,
        int getWellSpaceOdds = 100)
    {
        var q = new SubGoalQueue();
        build(q);
        _acts.Add(new Act(weight, q, getWellSpaceOdds));
        return this;
    }

    /// <summary>
    /// Control whether this goal can be pre-empted (Interupt function).
    /// <c>true</c> = always allow interruption; <c>false</c> = never (default).
    /// </summary>
    public GoalBuilder OnInterrupt(Func<object, bool> policy)
    {
        _canInterrupt = policy(null!);
        return this;
    }

    internal void Emit(StringBuilder sb, string npcId)
    {
        string goalConst  = $"GOAL_{npcId}_{Name}";
        string activateFn = $"{npcId}{Name}_Activate";
        string updateFn   = $"{npcId}{Name}_Update";
        string terminateFn= $"{npcId}{Name}_Terminate";
        string interuptFn = $"{npcId}{Name}_Interupt";

        sb.AppendLine($"REGISTER_GOAL({goalConst}, \"{npcId}{Name}\")");
        sb.AppendLine($"REGISTER_GOAL_NO_UPDATE({goalConst}, 1)");
        sb.AppendLine();

        // ── Helper functions ──────────────────────────────────────────────────
        foreach (var (fnName, body) in Helpers)
        {
            sb.AppendLine($"function {fnName}(ai, goal)");
            foreach (string line in body.Split('\n'))
            {
                string trimmed = line.TrimEnd();
                if (trimmed.Length > 0) sb.AppendLine("    " + trimmed.TrimStart());
            }
            sb.AppendLine("end");
            sb.AppendLine();
        }

        // ── Activate ──────────────────────────────────────────────────────────
        sb.AppendLine($"function {activateFn}(ai, goal)");

        if (_singleActivate != null)
        {
            // Simple single-act path — no weighted table needed
            foreach (string line in _singleActivate.Lines)
                sb.AppendLine(line);
        }
        else
        {
            // Weighted random act table via Common_Battle_Activate
            sb.AppendLine("    local actPerArr = {}");
            sb.AppendLine("    local actFuncArr = {}");
            sb.AppendLine("    local defFuncParamTbl = {}");
            sb.AppendLine("    Common_Clear_Param(actPerArr, actFuncArr, defFuncParamTbl)");

            // Normalise weights → percentages (integer, sum may not be exactly 100)
            int totalWeight = _acts.Sum(a => a.Weight);
            int cumulative  = 0;
            for (int i = 0; i < _acts.Count; i++)
            {
                int slot = i + 1;
                int pct  = (i == _acts.Count - 1)
                    ? 100 - cumulative
                    : (int)Math.Round(_acts[i].Weight * 100.0 / totalWeight);
                cumulative += pct;
                sb.AppendLine($"    actPerArr[{slot}] = {pct}");
            }

            sb.AppendLine();
            for (int i = 0; i < _acts.Count; i++)
                sb.AppendLine($"    actFuncArr[{i + 1}] = REGIST_FUNC(ai, goal, {npcId}_Act{i + 1:D2})");

            sb.AppendLine($"    local atkAfterFunc = REGIST_FUNC(ai, goal, {npcId}_ActAfterDefault)");
            sb.AppendLine("    Common_Battle_Activate(ai, goal, actPerArr, actFuncArr, atkAfterFunc, defFuncParamTbl)");
        }

        sb.AppendLine("end");
        sb.AppendLine();

        // ── Act functions (weighted mode only) ────────────────────────────────
        if (_singleActivate == null)
        {
            for (int i = 0; i < _acts.Count; i++)
            {
                Act act = _acts[i];
                sb.AppendLine($"function {npcId}_Act{i + 1:D2}(ai, goal, paramTbl)");
                foreach (string line in act.Queue.Lines)
                    sb.AppendLine(line);
                sb.AppendLine($"    return {act.GetWellSpaceOdds}");
                sb.AppendLine("end");
                sb.AppendLine();
            }

            // Default after-act function (no repositioning)
            sb.AppendLine($"function {npcId}_ActAfterDefault(ai, goal, paramTbl)");
            sb.AppendLine("    return false");
            sb.AppendLine("end");
            sb.AppendLine();
        }

        // ── Update / Terminate / Interupt ─────────────────────────────────────
        sb.AppendLine($"function {updateFn}(ai, goal)");
        sb.AppendLine("    return GOAL_RESULT_Continue");
        sb.AppendLine("end");
        sb.AppendLine();

        sb.AppendLine($"function {terminateFn}(ai, goal)");
        sb.AppendLine("end");
        sb.AppendLine();

        sb.AppendLine($"function {interuptFn}(ai, goal)");
        sb.AppendLine($"    return {(_canInterrupt ? "true" : "false")}");
        sb.AppendLine("end");
        sb.AppendLine();
    }
}

// ── AiBuilder ─────────────────────────────────────────────────────────────────

/// <summary>
/// Top-level fluent builder for a complete NPC AI script.
/// Use via <see cref="GamePatch.EditAi"/>.
///
/// <code>
/// g.EditAi("m18_01_00_00", "223200", ai => ai
///     .Goal("Battle", goal => goal
///         .OnActivate(q => q
///             .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 10)
///             .Attack(animId: 3008, cancelTime: 5))
///         .OnInterrupt(_ => true)));
/// </code>
/// </summary>
public sealed class AiBuilder
{
    private readonly List<GoalBuilder> _goals = new();

    /// <summary>Define a goal (Battle, Idle, etc.).</summary>
    public AiBuilder Goal(string name, Action<GoalBuilder> build)
    {
        var g = new GoalBuilder(name);
        build(g);
        _goals.Add(g);
        return this;
    }

    /// <summary>
    /// Emit the Lua 5.0 source for this AI script.
    /// <paramref name="npcId"/> is used as the Lua identifier prefix for function names;
    /// it must be a valid Lua identifier (letters, digits, underscores, not starting with a digit).
    /// If it starts with a digit, pass a separate <paramref name="luaId"/> like <c>"Npc223200"</c>.
    /// </summary>
    public string EmitLua(string npcId, string? luaId = null)
    {
        // Ensure the identifier used in Lua source is valid
        string id = luaId ?? (char.IsDigit(npcId[0]) ? "Npc" + npcId : npcId);
        var sb = new StringBuilder();
        foreach (GoalBuilder g in _goals)
            g.Emit(sb, id);
        return sb.ToString();
    }
}

// ── Luac50 ────────────────────────────────────────────────────────────────────

/// <summary>
/// Wrapper around the <c>luac50</c> binary that compiles Lua 5.0 source to the
/// bytecode format DSR expects.
///
/// <para>By default the wrapper looks for the binary at:
/// <list type="number">
///   <item><description><c>&lt;appBaseDir&gt;/tools/luac50</c> (Windows: luac50.exe)</description></item>
///   <item><description><c>luac50</c> / <c>luac5.0</c> on <c>PATH</c></description></item>
/// </list>
/// Override with <see cref="Configure"/>.</para>
/// </summary>
public static class Luac50
{
    private static string? _override;

    /// <summary>Override the binary path. Call before any <see cref="Compile"/> call.</summary>
    public static void Configure(string path) => _override = path;

    /// <summary>
    /// Compile <paramref name="source"/> and return the Lua 5.0 bytecode bytes.
    /// Throws <see cref="InvalidOperationException"/> if the compiler is not found,
    /// or <see cref="Exception"/> with stderr output on compile error.
    /// </summary>
    public static byte[] Compile(string source)
    {
        string luac = FindLuac();
        string srcPath = Path.Combine(Path.GetTempPath(), $"ds1mod_{Guid.NewGuid():N}.lua");
        string outPath = srcPath.Replace(".lua", ".luac");
        try
        {
            File.WriteAllText(srcPath, source, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var psi = new ProcessStartInfo(luac, $"-o \"{outPath}\" \"{srcPath}\"")
            {
                RedirectStandardError  = true,
                RedirectStandardOutput = true,
                UseShellExecute        = false,
            };
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("luac50 failed to start");
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception($"luac50 compile error:\n{stderr}\n\nSource:\n{source}");
            return File.ReadAllBytes(outPath);
        }
        finally
        {
            if (File.Exists(srcPath)) File.Delete(srcPath);
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    private static string FindLuac()
    {
        if (_override != null) return _override;

        // 1. tools/ next to the app
        string toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        string winBin   = Path.Combine(toolsDir, "luac50.exe");
        string nixBin   = Path.Combine(toolsDir, "luac50");
        if (File.Exists(winBin)) return winBin;
        if (File.Exists(nixBin)) return nixBin;

        // 2. PATH — try common names
        foreach (string candidate in new[] { "luac50", "luac5.0", "luac-5.0", "luac" })
        {
            string? found = FindOnPath(candidate);
            if (found != null) return found;
        }

        throw new InvalidOperationException(
            "luac50 not found. Put the Lua 5.0 compiler binary at tools/luac50 " +
            "(or tools/luac50.exe on Windows) relative to your app, or call " +
            "Luac50.Configure(path) before patching.");
    }

    private static string? FindOnPath(string name)
    {
        string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        string ext = OperatingSystem.IsWindows() ? ".exe" : "";
        foreach (string dir in pathEnv.Split(Path.PathSeparator))
        {
            string full = Path.Combine(dir, name + ext);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}
