# Plan: Pure-C# EMEVD and Lua AI API

Eliminate the need to write raw Lua or raw `new Instruction(bank, id, args)` calls.
Everything a mod author needs — event scripting and AI scripting — should be expressible
in idiomatic C# with full IntelliSense, type-safety, and no Lua files in the mod project.

**Status: Phase 1 + Phase 2 complete (named instructions + EventBuilder + condition allocator)**  
**Depends on:** DS1Mod.Modding (Phase 1 is additive to existing `EmevdEditor`)

---

## Problem statement

Today the EMEVD side requires knowing bank/ID pairs and argument types from memory:

```csharp
new EMEVD.Instruction(3, 0, new List<object>{ (sbyte)0, (byte)1, (byte)0, flagId })
// ^ what is this?  IfEventFlag, condGroup=0, state=ON, flagType=EventFlag, id=flagId
```

The Lua AI side requires writing and shipping a separate `.lua` file alongside the C# mod,
compiled offline with a vendored `luac 5.0` binary and embedded as a byte array:

```csharp
// GoofyDemon today: C# just embeds pre-compiled Lua bytecode
var lua = LoadResource("GoofyDemon_battle.luac");
bnd.AddOrReplace("c2230_battle.lua", lua);
```

The author must context-switch to Lua to write or change the AI behavior. Lua errors produce
no meaningful diagnostics; the AI simply stops working.

---

## Goal

Mod authors write only C#. The framework generates correct Lua 5.0 bytecode and EMEVD
binary automatically at patch time. The API surface should feel like this:

```csharp
// EMEVD — event scripting in C#
g.DefineEvent(11819100, RestBehavior.Default, ev => ev
    .WhenFlag(16, FlagState.On)
    .AwardItemLot(8500));

// Lua AI — goal scripting in C#
g.DefineAiBehavior("c2230", ai => ai
    .Goal("Battle", goal => goal
        .OnActivate(q => q
            .AddSubGoal(CommonGoal.ApproachTarget, target: Target.Enemy0, dist: Dist.Middle, cancelTime: 10)
            .AddSubGoal(CommonGoal.Attack, animId: 3008, cancelTime: 5))
        .OnInterrupt(_ => true)));
```

---

## Part 1 — Fluent EMEVD Builder

### Design

The existing `EmevdEditor` in `DS1Mod.Modding` wraps raw EMEVD manipulation with a helper
that handles idempotency and Event 0 registration. The builder layer sits on top of
`SoulsFormats.EMEVD` and is EMEDF-driven — instruction names and argument types come from
the same `ds1emedf.json` the decompile tool uses.

**Two tiers:**

**Tier 1 — Named fluent methods (covers 95% of use cases)**

```csharp
// DS1Mod.Modding — EventBuilder
public sealed class EventBuilder
{
    // Condition blocks
    public EventBuilder WhenFlag(int flagId, FlagState state) { ... }
    public EventBuilder WhenAllOf(Action<ConditionGroup> conds) { ... }
    public EventBuilder WhenAnyOf(Action<ConditionGroup> conds) { ... }
    public EventBuilder WhenEntityDead(int entityId) { ... }
    public EventBuilder WhenInsideRegion(int regionId) { ... }

    // Actions
    public EventBuilder AwardItemLot(int lotId) { ... }
    public EventBuilder SetFlag(int flagId, FlagState state) { ... }
    public EventBuilder DisplayMessage(int msgId, ScreenLoc loc = ScreenLoc.Center) { ... }
    public EventBuilder DisableCharacter(int entityId) { ... }
    public EventBuilder ForceAnimation(int entityId, int animId, bool wait = false) { ... }
    public EventBuilder End() { ... }

    // Raw escape hatch — for anything not yet named
    public EventBuilder Instruction(int bank, int id, params object[] args) { ... }

    internal EMEVD.Event Build() { ... }
}
```

Usage (from a `GamePatch` context):

```csharp
// EmevdEditor.DefineEvent() creates the event, registers it at the top of Event 0,
// and calls Build() — all idempotent via event ID check.
g.EditEmevd("m18_01_00_00", emevd => {
    emevd.DefineEvent(11819100, RestBehavior.Default, ev => ev
        .WhenFlag(16, FlagState.On)          // boss-killed flag
        .AwardItemLot(8500)
        .End());

    emevd.DefineEvent(11815700, RestBehavior.Restart, ev => ev
        .WhenFlag(11815000, FlagState.On)    // mood flag
        .DisplayMessage(6900690)
        .WhenFlag(11815000, FlagState.Off)
        .End());
});
```

**Tier 2 — Condition group allocation (auto, no manual register numbers)**

Condition register numbers (AND groups 1–15, OR groups −1 to −15 in DS1) are an
implementation detail. The builder allocates them automatically when `WhenAllOf` /
`WhenAnyOf` are nested:

```csharp
ev.WhenAllOf(and => and
        .Flag(16, FlagState.On)
        .EntityDead(1010800))
    .AwardItemLot(8500);
// → internally: allocate AND group 1, emit IfEventFlag(AND_01,...) + IfCharacterDead(AND_01,...),
//   then IfConditionState(MAIN, AND_01), then AwardItemLot
```

This is what DarkScript3's EventCFG does in its condition register pass. The builder
replicates that logic in C# without needing a JS frontend.

### Implementation sketch

```
DS1Mod.Modding/
  Emevd/
    EventBuilder.cs        — fluent builder; named methods; accumulates List<Intermediate>
    EmevdCompiler.cs       — condition register allocator + skip-count resolver
                            (port of DarkScript3's EventCFG into C#)
    EmevdDefs.cs           — EMEDF loader: name → (bank, id, ArgType[])
    Intermediates.cs       — AST nodes: Instr, IfBlock, WaitCond, End (port of DarkScript3 ScriptAst)
```

The EMEDF file (`tools/event_tools/ds1emedf.json`) is loaded at startup and embedded as a
resource in `DS1Mod.Modding.dll` so mod authors don't need the file at runtime.

**DarkScript3 prior art:** DarkScript3's `ScriptAst.cs` (internal AST) + `EventCFG.cs`
(compiler back-end) already solve the hard problem of condition register allocation and
skip-count resolution. The implementation is a C# port of that logic, removing the JS
frontend and replacing it with the fluent builder above.

---

## Part 2 — C# Lua AI Builder

### Why not transpile C# to Lua?

The options are:
- **CSharp.lua transpiler** — pulls in CoreSystem.lua (Lua 5.1 runtime shim); DSR runs Lua
  5.0 which lacks several 5.1 features. The version gap + runtime overhead rules this out.
- **Expression trees interpreted at patch time** — clean but control flow (if/while) requires
  `BlockExpression` gymnastics; goal scripts have very little branching.
- **C# DSL that emits Lua source** — the right fit. The DSR AI API surface is tiny (4
  lifecycle callbacks + `AddSubGoal` + ~15 common primitive goals). A C# builder can cover
  100% of it with named methods.

**Conclusion:** write a C# `AiGoalBuilder` that emits Lua 5.0 source text, then shell out
to a vendored `luac` to produce the binary. No transpiler needed; the Lua DSL surface is so
small the builder writes it directly.

### The DSR Lua AI API

Every enemy AI script defines goals. A goal has four entry points:

```lua
function Goal.Activate(goal, entity)  -- called when goal becomes active
function Goal.Update(goal, entity)    -- called every AI tick (→ GOAL_RESULT_*)
function Goal.Terminate(goal, entity) -- called when goal exits
function Goal.Interrupt(goal, entity) -- return true if goal can be preempted
```

Inside `Activate`, the only meaningful operation is queuing subgoals:

```lua
goal:AddSubGoal(GOAL_COMMON_ApproachTarget, cancelTime, TARGET_ENE_0, DIST_Middle, 0, 0)
goal:AddSubGoal(GOAL_COMMON_Attack, cancelTime, animId, TARGET_ENE_0, DIST_Near, 0)
```

`OnIf_XXXXXXXX(goal, entity)` functions can branch mid-subgoal (e.g., combo followups based
on distance after an animation frame fires). These are the only constructs needed for most
boss AI.

### C# API

```csharp
// DS1Mod.Modding — AiScriptBuilder
g.DefineAiBehavior("c2230", ai => ai
    .Goal("Battle", goal => goal
        .OnActivate(q => q
            .ApproachTarget(Target.Enemy0, dist: Dist.Middle, cancelTime: 10)
            .Attack(animId: 3008, cancelTime: 5)
            .Wait(cancelTime: 3))
        .OnUpdate(_ => GoalResult.Continue)
        .OnInterrupt(_ => true)
        .OnIf(animFrame: 3008, branchName: "FollowUp", branch => branch
            .IfDistanceLessThan(Target.Enemy0, dist: 2.5f, q => q
                .Attack(animId: 3010, cancelTime: 4))))

    .Goal("Idle", goal => goal
        .OnActivate(q => q.Wait(cancelTime: 30))
        .OnInterrupt(_ => true)));
```

`DefineAiBehavior` at the end:
1. Walks the builder tree and emits Lua 5.0 source text into a string
2. Calls `Luac50.Compile(source)` — shells out to vendored `luac` 5.0 binary (embedded as
   a resource in `DS1Mod.Modding.dll`, extracted to a temp path on first use)
3. Returns the compiled bytecode bytes
4. Caller injects into the map's `luabnd.dcx` via `GamePatch.EditBnd3`

The emitted Lua is human-readable; it is written to a `.lua` file alongside the mod's
output for debugging (opt-in, off by default).

### Primitive goal vocabulary

Named C# methods map to the `GOAL_COMMON_*` constants from `goal_list.lua`:

| C# method | Lua goal constant | Notes |
|---|---|---|
| `.ApproachTarget(target, dist, cancelTime)` | `GOAL_COMMON_ApproachTarget` | |
| `.Attack(animId, cancelTime)` | `GOAL_COMMON_Attack` | |
| `.DashAttack(animId, cancelTime)` | `GOAL_COMMON_DashAttack` | |
| `.Guard(cancelTime)` | `GOAL_COMMON_Guard` | |
| `.SpinStep(cancelTime)` | `GOAL_COMMON_SpinStep` | |
| `.Wait(cancelTime)` | `GOAL_COMMON_Wait` | |
| `.RawSubGoal(goalId, ...)` | any | raw escape hatch |

The complete `GOAL_COMMON_*` range (1000–2254 in `goal_list.lua`) can be added on demand.

### Vendored luac 5.0

The Lua 5.0 reference interpreter ships a `luac` binary that compiles `.lua` source to
the 5.0 bytecode format DSR uses. `DSLuaDecompiler` proves this format is understood.

`DS1Mod.Modding` embeds `luac50.exe` (Windows) and `luac50` (Linux) as embedded resources.
`Luac50.Compile(string source)` extracts the appropriate binary to a temp path and invokes it
via `Process.Start`, capturing stdout as the bytecode bytes. The binary is small (~100 KB).

Alternative: port the Lua 5.0 bytecode emitter directly to C# using the format documented by
`DSLuaDecompiler` — no subprocess needed, works on any platform. More work upfront but
eliminates the subprocess dependency. Defer to Phase 3.

---

## Part 3 — ID Allocation Integration

Both builders integrate with the `ModIdSpace.BaseFor(assemblyName)` allocator from the
conflict resolution plan. The context passes an `IIdAllocator` to each builder, so authors
can write:

```csharp
// IDs are stable per-assembly, never collide with other mods
int flagId  = ctx.Ids.Next(IdSpace.EventFlag);   // in 50000000+ range
int msgId   = ctx.Ids.Next(IdSpace.EventText);
int evtId   = ctx.Ids.Next(IdSpace.EmevdEvent);

g.EditEmevd("m18_01_00_00", emevd =>
    emevd.DefineEvent(evtId, RestBehavior.Default, ev => ev
        .WhenFlag(flagId, FlagState.On)
        .DisplayMessage(msgId)));
```

---

## Implementation phases

### Phase 1 — Named EMEVD instruction methods (no condition allocator yet)

- Add named methods to `EmevdEditor` for the 20 most common instructions
- Each method takes typed C# arguments; creates a raw `EMEVD.Instruction` internally
- No builder chain yet — methods still call `InsertAtTop` / `AppendToEvent` like today
- Breaking change: none — purely additive

**Deliverable:** mod authors can write `emevd.WhenFlag(...)` and `emevd.AwardItemLot(...)`
instead of `new EMEVD.Instruction(3, 0, ...)`.

### Phase 2 — EventBuilder + condition allocator

- Port `ScriptAst` intermediate types from DarkScript3 into C#
- Port `EventCFG` condition register allocator and skip-count resolver
- Expose `DefineEvent(id, rest, ev => ...)` on `EmevdEditor`
- Control flow: `WhenAllOf` / `WhenAnyOf` + auto register allocation

**Deliverable:** complex condition-group events expressible entirely in C#.

### Phase 3 — AiGoalBuilder + Lua emitter

- `AiGoalBuilder` C# DSL → Lua source string emitter
- Luac 5.0 wrapper (`Luac50.Compile`)
- `DefineAiBehavior(npcId, ai => ...)` on `GamePatch` context
- Named methods for `GOAL_COMMON_*` vocabulary

**Deliverable:** AI scripts written entirely in C#. No `.lua` files in mod projects.

### Phase 4 — Native Lua 5.0 bytecode emitter (optional)

- Port Lua 5.0 bytecode format (documented by DSLuaDecompiler) to a C# emitter
- Eliminates subprocess dependency and works on all platforms at build time
- Enables compile-time validation of goal IDs and argument types

---

## Design reference

| Problem | Art we're copying | Language |
|---|---|---|
| EMEVD ScriptAst IR | DarkScript3 `ScriptAst.cs` | C# (already C#) |
| EMEVD condition register allocator | DarkScript3 `EventCFG.cs` | C# (already C#) |
| Host-language-as-EMEVD-DSL | Soulstruct EVS | Python (port concept to C#) |
| Lua goal builder → Lua source text | War3Net / Drake53 stub API model | C# for WC3 |
| AI goal lifecycle | SoulsAI `goal_list.lua` | Lua reference |
| Fluent builder with `.End()` scope stack | FluentBehaviorTree, Fluid BT | C# |
| Extension-method vocabulary injection | Fluid BT `BehaviorTreeBuilderExtensions` | C# |

---

## What this unlocks for mod authors

```csharp
// Before (today): raw Lua file + raw instruction construction
// c2230_battle.lua shipped as embedded resource, compiled offline
var lua = LoadResource("c2230_battle.lua.bin");
bnd.AddOrReplace("c2230_battle.lua", lua);

new EMEVD.Instruction(3, 0, new List<object>{ (sbyte)0, (byte)1, (byte)0, 16 })
new EMEVD.Instruction(2003, 4, new List<object>{ 8500 })

// After: pure C#, full IntelliSense, no external files
g.DefineAiBehavior("c2230", ai => ai
    .Goal("Battle", goal => goal
        .OnActivate(q => q
            .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 10)
            .Attack(animId: 3008, cancelTime: 5))
        .OnInterrupt(_ => true)));

g.EditEmevd("m18_01_00_00", emevd =>
    emevd.DefineEvent(ctx.Ids.Next(IdSpace.EmevdEvent), RestBehavior.Default, ev => ev
        .WhenFlag(16, FlagState.On)
        .AwardItemLot(8500)));
```

One language. One file. No mental model switching.
