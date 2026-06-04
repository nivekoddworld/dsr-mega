# Editing event scripts (EMEVD / "DarkScript")

Map logic — boss intros, fog gates, triggers, item awards, on-screen text — is
**EMEVD**, a compiled event bytecode. It is **not** Lua. "DarkScript" is just the
human-readable *decompiled* form of EMEVD.

## Where it lives

- `event/m??_??_??_??.emevd.dcx` — per-map event scripts.
- `event/common.emevd.dcx` — shared logic initialized by every map.
- `.emeld.dcx` — companion parameter data (we didn't need to touch it).

Decompiled copies are in [`../decompiled_emevd/`](../decompiled_emevd/); the
decompiler is in [`../event_tools/`](../event_tools/).

## The model

An EMEVD file is a list of **Events**. Each event has:

- an **ID**, a **RestBehavior** (`Default` = run once, `Restart` = loop, `End`),
- **Instructions** — each is `(Bank, ID, args)`,
- optional **Parameters** (values injected by the event that *initializes* it).

Instruction names and argument types come from the **EMEDF** — the DS1
definition file (`event_tools/ds1emedf.json`, from
[DarkScript3](https://github.com/AinTunez/DarkScript3) / [soulsmods](https://soulsmods.github.io/emedf/ds1-emedf.html)).
EMEDF arg **type codes**: `0`=u8 `1`=u16 `2`=u32 `3`=s8 `4`=s16 `5`=s32 `6`=f32 —
which map 1:1 to SoulsFormats `EMEVD.Instruction.ArgType`.

## Decompiling on Linux (no DarkScript3 needed)

SoulsFormats parses the binary; the EMEDF supplies names. See
`event_tools/emevd_decompile/`:

```csharp
EMEVD evd = EMEVD.Read(DCX.Decompress(path));
foreach (var ev in evd.Events)
  foreach (var ins in ev.Instructions) {
    var def = emedf[(ins.Bank, ins.ID)];                  // name + arg types
    var vals = ins.UnpackArgs(def.ArgTypes);              // decode the bytes
    // render: name(argName=value, ...), enums resolved, params shown as X<byte>
  }
```

Run it:

```sh
dotnet run --project event_tools/emevd_decompile -- \
    event_tools/ds1emedf.json  DSR_Event_Folder/event  decompiled_emevd
```

## Execution model (important)

Instructions run **top to bottom**. The key construct is conditions:

- `IF X (MAIN)` — registers condition `X` into the **MAIN** group and **blocks**
  the event there until `X` is true, then continues. (This is how a boss intro
  "waits for the player to enter an area".)
- `IF X (AND_01 / OR_01 / ...)` — non-blocking; accumulates into a sub-group that
  a later instruction checks.
- `SKIP n ...` — conditionally skips the next `n` instructions (offsets are
  **relative** to the SKIP).
- `END IF ...` / `END` — terminates the event.

## Editing an existing event (the easy, reliable case)

Build an instruction with SoulsFormats and insert it. Example — the "fart on
landing" (a `Display Message` right after the boss's jump-down animation):

```csharp
using ArgType = SoulsFormats.EMEVD.Instruction.ArgType;
var ev = evd.Events.First(e => e.ID == 11810310);          // the entrance event
// find Force Animation Playback(1810800, 9060) and insert after it
ev.Instructions.Insert(i+1, new EMEVD.Instruction(2007, 4, new List<object>{ msgId, (byte)0 })); // Display Message
foreach (var p in ev.Parameters) if (p.InstructionIndex >= i+1) p.InstructionIndex++; // keep params valid
```

This is reliable because you're injecting into an event the game **already
runs**. Construct instructions are byte-verifiable — dump `ins.ArgData` and
compare to a vanilla instance of the same instruction.

## Adding a brand-new event

Events don't run on their own — the **map constructor (event 0)** starts each one
with `Initialize Event(slot, eventId, params)` (`2000:0`). So:

```csharp
// 1) build the event
var me = new EMEVD.Event(11819000, EMEVD.Event.RestBehaviorType.Restart);
me.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object>{ (sbyte)0,(byte)1,(byte)0, flag }));  // IF Event Flag ON
me.Instructions.Add(new EMEVD.Instruction(2007, 4, new List<object>{ msgId, (byte)0 }));               // Display Message
me.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object>{ (sbyte)0,(byte)0,(byte)0, flag }));  // IF Event Flag OFF
evd.Events.Add(me);

// 2) register it — AT THE TOP of event 0
var ev0 = evd.Events.First(e => e.ID == 0);
ev0.Instructions.Insert(0, new EMEVD.Instruction(2000, 0, new List<object>{ (int)0, (uint)11819000, (uint)0 }));
```

> ### GOTCHA: register at the *top* of the constructor
> Event 0 has multiplayer `SKIP`s and an `END IF` partway through. A registration
> **appended at the end** can be skipped or the event terminated before reaching
> it — so the new event never starts (this cost us a whole debug cycle: the
> *fart* showed but the mood events didn't). Event 0 has **0 parameters** and
> SKIP offsets are relative, so **prepending** at index 0 is safe.

Parameterless `Initialize Event` is 12 bytes: `slot(int32) eventId(uint32)
params(uint32=0)`. Clone a vanilla one if unsure.

## On-screen text

The text instructions (bank `2007`) pull strings from the **`Event_text` FMG**
(inside `msg/<lang>/menu.msgbnd.dcx`):

| Instruction | Text source |
|---|---|
| `Display Message` (2007:4) — `msgId, screenLoc` | FMG id — **custom text** ✅ |
| `Display Status Message` (2007:3) | FMG id — custom text ✅ |
| `Display Banner` (2007:2) — `bannerType` | **fixed presets only** ("Victory Achieved"…) ❌ |

So custom text = add an entry to the `Event_text` FMG (`FMG.Read` →
`Entries.Add(new FMG.Entry(id, "..."))`) and point a `Display Message` at it.

## Event flags

`Set Event Flag` / `IF Event Flag` (and `ai:SetEventFlag` from Lua — the
**AI↔EMEVD bridge**, e.g. vanilla enemy `286000` sets `11505105` and m15's EMEVD
reads it) all share the global event-flag store.

> ### GOTCHA: flag *section* must be allocated
> Map flags are `1<area><section><number>` (e.g. `11_181_5_700` = area 181,
> section 5, number 700). A map only allocates **some sections**. m18_01 uses
> **section 0 (`11810xxx`)** and **section 5 (`11815xxx`)** — and nothing else.
> We first used `11817xxx` (section 7); it isn't allocated, so the flags silently
> did nothing. **Check which sections the map already uses** (grep the decompiled
> emevd for `Event Flag ID=1181`) and pick a free number in one of those.
> "Item obtained" flags use the `50000000+` range.

## Animations & the Asylum entrance

The jump-down intro (event `11810310`) = **warp** the boss to a ledge
(`1812305`) + **`Force Animation Playback(9060)`** whose **root motion** carries
him down + impact SFX + `Set Character Home`. The game sets the *start* position;
the animation moves him (displacement is baked into the clip).

- `Force Animation Playback` (2003:18) takes `entity, animId, loop, wait,
  ignoreTransition` — **no speed/direction**. You can't play an animation
  backwards from EMEVD. A reversed clip means baking a reversed **HKX** (root
  motion reverses too) in DSAnimStudio, then playing that new id forward.

## Useful instruction reference (DS1)

| Instruction | Bank:ID | Args |
|---|---|---|
| Initialize Event | 2000:0 | slot, eventId, params… |
| IF Event Flag | 3:0 | condGroup(s8), state(u8 ON/OFF), flagType(u8), flagId(s32) |
| Set Event Flag | 2003:66 | flagId, state |
| Force Animation Playback | 2003:18 | entity, animId, loop, wait, ignoreTransition |
| Display Message | 2007:4 | msgId(s32), screenLoc(u8) |
| Display Banner | 2007:2 | bannerType(u8, preset enum) |
| Award Item Lot | 2003:4 | itemLotId(s32) |
| Play Cutscene To Player | 2002:3 | cutsceneId, playMode, playerEntity |
| Spawn Oneshot SFX | 2006:3 | entityType, entity, dummypoly, sfxId |
| Set Camera Vibration | 2008:2 | vibrationId, entityType, entity, dummypoly, … |

Full set: `event_tools/ds1emedf.json` or
[the EMEDF reference](https://soulsmods.github.io/emedf/ds1-emedf.html).
