# ESD Editing Guide for DS1Mod Modders

ESD (EZState) is FromSoft's graph-based state machine scripting system used in Dark Souls Remastered for:
- **Talk ESD** (`script/talk/t*.talkesdbnd.dcx`) — NPC dialog, bonfire menus, shop interactions
- **Action ESD** (`chr/c0000.esd.dcx`, `chr/enemyCommon.esd.dcx`) — all animations and character action states

This guide covers the DS1Mod modding framework's fluent C# API for editing both.

## Quick Start

### Unlock Level Up on All Bonfires

```csharp
g.EditEsdBySize("script/talk", 23012, esd =>
    esd.SetTalkListGateFlag(1, 4, 15000100, -1));
```

This removes the event-flag gate (`11810000`) from the Level Up menu item on all vanilla bonfire ESDs (detected by exact size 23012 bytes). Setting gate to `-1` means "always show, regardless of flags."

### Create a Custom Dialog Tree

```csharp
g.EditEsd("script/talk/t100001.talkesdbnd.dcx", "100001.esd", esd =>
{
    // Add entry point dialog
    var talkState = esd.GetOrAddState(1, 50);
    talkState.AddEntryCommand(1, 50, TalkCmd.OpenGenericDialog(
        dialogType: 8,
        messageId: 999,     // Your FMG message ID
        buttonType: 3,
        numButtons: 2,      // Yes / No
        unk: 2));
    
    // Route on player answer
    esd.AddTransition(1, 50, 51,
        EsdBytecode.DialogClosedWithButton(1));  // Yes button → state 51
    esd.AddTransition(1, 50, 5,
        EsdBytecode.DialogClosedWithButton(2));  // No button → state 5
    
    // State 51: set a flag on Yes
    var yesState = esd.GetOrAddState(1, 51);
    yesState.AddEntryCommand(1, 51, TalkCmd.SetEventFlag(11810500, true));
});
```

### Prevent Attacking While Stunned

```csharp
g.EditActionEsd("c0000", esd =>
{
    var idleState = esd.GetOrAddState(0, 0);  // Group 0 = main action machine, State 0 = idle
    
    // Insert stun check at highest priority (index 0)
    // When stunned (Fn3 active), stay in idle; don't transition to attack
    idleState.InsertTransition(0, 0, 0,
        ActionEsdBytecode.Not(ActionEsdBytecode.Fn3()),
        index: 0);
});
```

---

## Talk ESD: Dialog & Menus

### Entry Points

```csharp
// Edit a single talk ESD by file path and name
g.EditEsd("script/talk/t100001.talkesdbnd.dcx", "100001.esd", esd => { ... });

// Edit all ESDs matching a size (useful for bonfires, which are all the same)
g.EditEsdBySize("script/talk", 23012, esd => { ... });
```

### Talk ESD Condition Functions

| Function | Purpose |
|----------|---------|
| `GetEventFlag(flagId)` | Returns 1 if flag is ON, 0 if OFF |
| `GetMenuSelection()` | Which menu item is highlighted (0-based index) |
| `GetDialogButtonResult()` | Which button player pressed in a dialog (1=Yes, 2=No, etc.) |
| `IsGenericDialogOpen(personId=0)` | Returns 1 if dialog is open, 0 if closed |
| `GetTimeInState()` | Seconds elapsed in current state (for timeouts/delays) |
| `DialogClosedWithButton(button)` | Passes when dialog just closed AND player chose button X |
| `SelectedItem(listIndex)` | Passes when player selected menu item at index X |

### Talk ESD Commands

| Command | Purpose |
|---------|---------|
| `SetEventFlag(flagId, on)` | Set flag ON/OFF (3187 uses — most common command) |
| `OpenGenericDialog(type, msgId, btnType, numBtns, unk)` | Show dialog box with text and buttons |
| `AddTalkListData(listIdx, talkId, gateFlag=-1)` | Add menu item (gate=-1 always shows) |
| `AddTalkListDataIf(condition, listIdx, talkId, unk)` | Add menu item only if condition passes |
| `ClearTalkListData()` | Clear the menu list — call before repopulating with `AddTalkListData` |
| `ShowShopMessage(a=0, b=0, c=0)` | Display the shop/wares message (vanilla always passes `(0, 0, 0)`) |

> **Note on `UpdateRespawnPoint`**: an earlier revision of this guide listed a
> `B1:101 UpdateRespawnPoint(bonfireEntityId)` Talk ESD command. Scanning all
> 357 ESDs across every DSR map's `talkesdbnd` archives turned up zero uses of
> Bank 1 command 101 — it does not exist in Talk ESD. Setting the player's
> respawn bonfire is an **EMEVD**-level operation (`SetPlayerRespawnPoint`, as
> shown in the soulsmodding advanced-ESD tutorial's warp-event example), not a
> Talk ESD command. The API has been removed.

### State Structure

States in Talk ESD execute in this order:
1. **Entry commands** — run once on entry
2. **While commands** — run every frame at 30Hz (rarely used in talk ESD)
3. **Conditions** — check bytecode evaluators in order; first passing one triggers transition
4. **Exit commands** — run once on exit (when transitioning to another state)

Example: Dialog flow with confirmation
```csharp
esd.GetOrAddState(1, 10);  // Dialog prompt state
esd.GetOrAddState(1, 11);  // Confirmed state
esd.GetOrAddState(1, 5);   // Cancelled state

var dialogState = esd.GetOrAddState(1, 10);
dialogState.AddEntryCommand(1, 10, TalkCmd.OpenGenericDialog(8, 999, 3, 2));

esd.AddTransition(1, 10, 11, EsdBytecode.DialogClosedWithButton(1));  // Yes
esd.AddTransition(1, 10, 5, EsdBytecode.DialogClosedWithButton(2));   // No
```

---

## Action ESD: Animation & Combat States

### Entry Points

```csharp
// Edit player animation state machine
g.EditActionEsd("c0000", esd => { ... });

// Edit all enemy animation state machines
g.EditActionEsd("enemyCommon", esd => { ... });
```

### Action ESD Condition Functions (Verified from Game Files)

Top 9 most common (extracted from c0000.esd):

| Function | Freq | Purpose (Inferred) |
|----------|------|-------------------|
| `Fn0()` | 398× | Always-true / default state check |
| `Fn112()` | 240× | Attack animation duration / combo gating |
| `Fn109()` | 236× | Button release / state routing |
| `Fn2()` | 223× | World state (airborne, stamina, animation) |
| `Fn3()` | 219× | Stun / equipment / buff checks |
| `Fn116()` | 216× | Spell / item / ability gating |
| `Fn111()` | 204× | Dodge / roll / backstab timing |
| `Fn115()` | 204× | Movement logic |
| `Fn104()` | 195× | Inventory / stance / animation sync |

Enemy-specific (from enemyCommon.esd):
- `EnemyFn107()` (148×), `EnemyFn118()` (146×), `EnemyFn120()` (109×) — AI behavior routing

### Action ESD Commands

| Command | Purpose |
|---------|---------|
| `SetUpperBodyAnimation(animId, duration)` | Play animation in upper body slot |
| `SetLowerBodyAnimation(animId, duration)` | Play animation in lower body slot |
| `CancelAnimation()` | Return to idle |
| `SetItemInUse(active)` | Flag "item is being used" |
| `SyncAnimationAtInit(active)` | Synchronize animation state with initialization |
| `RawCommand(bank, cmdId, args)` | Escape hatch for unknown commands |

### State Structure

Action ESD states run in this order:
1. **Entry commands** — run once on entry
2. **While commands** — run every frame at 30Hz (primary control loop)
3. **Conditions** — check evaluators; first passing transitions
4. **Exit commands** — run once on exit

Example: Attack chain with stamina gating
```csharp
g.EditActionEsd("c0000", esd =>
{
    var attackState = esd.GetOrAddState(0, 100);  // Attack loop state
    
    // Gate: only allow attack if stamina ≥ 30
    attackState.AddTransition(0, 100, 0,
        EsdBytecode.Ge(ActionEsdBytecode.Fn2(), ActionEsdBytecode.PushInt(30)));
});
```

---

## Bytecode Expressions

All condition functions return **complete bytecode expressions** ending with `0xA1` (terminate).

### Literals

```csharp
EsdBytecode.Always()           // Always true
EsdBytecode.Never()            // Always false
EsdBytecode.PushInt(value)     // Integer literal
EsdBytecode.PushDouble(value)  // Double literal
```

### Composition (Composition helpers strip trailing terminators before merging)

```csharp
EsdBytecode.And(a, b)    // a AND b
EsdBytecode.Or(a, b)     // a OR b
EsdBytecode.Not(a)       // NOT a (equivalent to a == false)
EsdBytecode.Eq(a, b)     // a == b
EsdBytecode.Ne(a, b)     // a != b
EsdBytecode.Ge(a, b)     // a >= b

// Example: Stun and stamina low?
var expr = EsdBytecode.And(
    ActionEsdBytecode.Fn3(),  // Is stunned
    EsdBytecode.Le(ActionEsdBytecode.Fn2(), EsdBytecode.PushInt(20)));
```

### Raw Bytecode (if needed)

```csharp
// Paste hex from ESDLang, hex editor, etc.
EsdBytecode.FromHex("4F 82 E5 9F D5 00 85 41 95 A1");
```

---

## Common Patterns

### Bonfire: Add a New Menu Option (flag → EMEVD bridge)

The bonfire UI has a fixed layout (see `AddBonfireMenuItem` / the "ESD Modding
Patterns" section of `CLAUDE.md`), so new options are wired through a flag that
EMEVD listens for — not through ESD-side warp/respawn commands (there is no
such Talk ESD command; see the note on `UpdateRespawnPoint` above).

```csharp
g.EditEsdBySize("script/talk", 23012, esd =>
{
    const long menuState = 4;
    const long actionState = 100;
    const int  flagOnSelect = 11810600; // EMEVD listens for this

    // Add the new item at list index 16: "Teleport to Firelink"
    esd.AddEntryCommand(1, menuState,
        TalkCmd.AddTalkListData(16, 15000300, gateFlag: -1));

    // Route to the action state when the player selects it
    esd.AddTransition(1, menuState, actionState,
        EsdBytecode.SelectedItem(16));

    // Action state: set the flag and let EMEVD do the actual warp
    esd.AddEntryCommand(1, actionState,
        TalkCmd.SetEventFlag(flagOnSelect, on: true));
});
```

Then in EMEVD, listen for `flagOnSelect` and call `WarpPlayer` /
`SetPlayerRespawnPoint` — see
`reference/SoulsModding_Advanced_ESD_Tutorial.md` for the full event handler.

### Combat: Stagger Breaks Chain

```csharp
g.EditActionEsd("enemyCommon", esd =>
{
    // Find the attack loop state (example: state 50 in group 0)
    var attackLoop = esd.GetOrAddState(0, 50);
    
    // If stunned, exit attack immediately
    attackLoop.InsertTransition(0, 50, 200,  // 200 = stagger state
        ActionEsdBytecode.Fn3(),  // Fn3 = stun check
        index: 0);
});
```

### Dialog: Locked Behind Progression

```csharp
g.EditEsd("script/talk/t100001.talkesdbnd.dcx", "100001.esd", esd =>
{
    var menuState = esd.GetOrAddState(1, 4);
    
    // Only show "Buy" if player has flag 11810100
    menuState.AddEntryCommand(1, 4,
        TalkCmd.AddTalkListDataIf(
            EsdBytecode.GetEventFlag(11810100),
            listIndex: 5,
            talkId: 15000050));
});
```

---

## Debugging & Discovery

### Find Unknown Function IDs

Extract the actual game file and dump all condition functions:

```csharp
var esd = ESD.Read(File.ReadAllBytes(@"C:\Path\to\game\chr\c0000.esd.dcx"));
ActionEsdBytecode.VerifyFunctionId(esd);
// Prints: fn0 x398, fn112 x240, fn109 x236, ...
```

Then use the frequency ranking to guess semantics (high-frequency functions are core logic).

### Inspect Raw Bytecode

The `Evaluator` field on `ESD.Condition` contains the raw bytecode:

```csharp
var condition = state.Conditions[0];
Console.WriteLine(string.Join(" ", condition.Evaluator.Select(b => b.ToString("X2"))));
// Output: "65 82 00 00 00 00 85 82 01 00 00 00 95 A1"
```

Compare against known patterns to identify functions.

---

## Limits & Caveats

1. **Action ESD function semantics are inferred** — we have verified IDs and frequency, but don't know exactly what Fn112 vs Fn109 check. Use game testing to calibrate.
2. **No multi-file coordination** — each `EditEsd()` call is independent. Complex scenarios require multiple calls.
3. **No state graph querying** — no built-in "find all transitions to state X" helper. Use raw `Esd.StateGroups` for deep analysis.
4. **ESD-only edits** — dialog branches without backing PARAM/EMEVD logic won't do anything meaningful. Coordinate with other patchers.

---

## Resources

- **Soulsmodding ESD docs**: https://www.soulsmodding.com/doku.php?id=format:esd
- **FogMod source** (ground-truth function IDs): `reference/FogMod-master/FogMod/GameDataWriter3.cs`
- **SoulsFormats library** (ESD parse/serialize): `lib/SoulsFormats/SoulsFormats/Formats/ESD.cs`
- **Bonfire patch example**: `src/DS1MegaRando.Core/GameFileWriter.cs` line 725
