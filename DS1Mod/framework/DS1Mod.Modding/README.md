# DS1Mod.Modding

A helper library for writing DSR **game-data modification** mods on top of
[SoulsFormats](../../SoulsFormats). It captures the patterns (and the *gotchas*)
worked out building `DS1Mod.GoofyDemon`: DCX round-tripping, idempotent FMG /
PARAM / EMEVD edits, and the two traps that cost real debugging — registering
new events at the **top** of the constructor, and event-flag **sections**.

Framework-agnostic — only depends on SoulsFormats — so it works inside a
DS1Mod `IGamePatcher` or any standalone tool.

---

## The shape of every edit

`GamePatch` wraps "resolve a path under the game dir → back it up → decompress →
hand you the parsed object → recompress and write back":

```csharp
var g = new GamePatch(ctx.GameDir, ctx.BackupFile, Log);   // from IPatchContext

g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>        // one archive
    bnd.SetFileContaining("223200_battle.lua", myLuaBytes));

g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>            // every language
    Texts.Set(bnd, Texts.EventText, 6900690, "*farts*"));

g.EditEmevd("m18_01_00_00", e => { /* events */ });        // a map's event script
g.EditParams(embeddedParamdefBytes, repo => { /* rows */ });// GameParam
```

All edits are idempotent — the same `Patch()` call can run every launch.

---

## Text (FMG)

```csharp
Texts.Set(bnd, Texts.EventText, msgId, "hello");   // both DSR copies updated
Texts.Set(bnd, Texts.GoodsName, 8000, "My Item");
string? s = Texts.Get(bnd, Texts.GoodsName, 8000);
```

Common FMG constants: `Texts.EventText`, `Texts.GoodsName`, `Texts.GoodsInfo`,
`Texts.WeaponName`, `Texts.WeaponInfo`, `Texts.NpcName`.

---

## Params

`AddClone` copies a vanilla donor row (every field valid), adds it idempotently,
and lets you tweak fields:

```csharp
g.EditParams(paramdefs, repo => {
    repo.Edit("EquipParamGoods", p =>
        ParamRepository.AddClone(p, donorId: 384, newId: 8000, "My Item",
            r => r["maxNum"].Value = (ushort)1));

    repo.Edit("ItemLotParam", p =>
        ParamRepository.AddClone(p, 1000, 8500, "My drop", r => {
            r["lotItemId01"].Value       = 8000;
            r["lotItemCategory01"].Value = LotCategory.Goods;
            r["getItemFlagId"].Value     = 50009000;  // once-only pickup
        }));
});
```

The paramdefbnd (layout) isn't shipped by the game — **embed it** in your mod
and pass the bytes (`GamePatch.EditParams` calls `ParamRepository.LoadDefs`).

`LotCategory` constants: `Weapon`, `Protector` (armor), `Accessory` (rings), `Goods`.

---

## Events (EMEVD)

Events drive everything in DSR — spawns, triggers, item awards, boss sequences.
`EmevdEditor` wraps the SoulsFormats EMEVD with idempotency and the constructor/
flag gotchas handled for you.

### Quick start — simple linear event

```csharp
g.EditEmevd("m18_01_00_00", e => {
    // Define a run-once event: wait for the boss-killed flag, then award a drop.
    // DefineEvent registers it at the TOP of Event 0 automatically.
    e.DefineEvent(11819100, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenFlag(16, FlagState.On)
        .AwardItemLot(8500)
        .End());
});
```

### Looping event

```csharp
e.DefineEvent(11819000, EMEVD.Event.RestBehaviorType.Restart, ev => ev
    .WhenFlag(11815000, FlagState.On)    // wait for mood flag
    .DisplayMessage(6900690)             // show text
    .WhenFlag(11815000, FlagState.Off)   // wait for it to go off, then loop
    .Restart());
```

`RestBehaviorType.Restart` means the event re-runs automatically from the top
after it ends. `.Restart()` at the end is an explicit unconditional restart —
use it when you want to loop even if the conditions haven't changed.

### Compound conditions — WhenAllOf / WhenAnyOf

Use `WhenAllOf` to block until **every** condition in the group is true (AND),
and `WhenAnyOf` to block until **any** is true (OR). The condition group number
is allocated automatically — you never need to pick one:

```csharp
// Wait until the boss is dead AND the player got the flag, then give the reward.
e.DefineEvent(11819200, EMEVD.Event.RestBehaviorType.Default, ev => ev
    .WhenAllOf(and => and
        .Dead(1810800)                   // boss entity
        .Flag(16, FlagState.On))         // game completion flag
    .AwardItemLot(8500)
    .End());

// Wait until the player enters the arena OR the boss flag fires.
e.DefineEvent(11819300, EMEVD.Event.RestBehaviorType.Default, ev => ev
    .WhenAnyOf(or => or
        .InsideArea(10000, 1815900)      // player inside trigger region
        .Flag(11810000, FlagState.On))   // vanilla boss music trigger
    .SetCharacterEnabled(1810800, EnabledState.Enabled)
    .End());
```

DS1 supports up to **7 AND groups** and **7 OR groups** per event. Each call to
`WhenAllOf`/`WhenAnyOf` allocates one slot, so don't use more than 7 of either
in a single event body.

`SubConditionBuilder` methods: `Flag`, `Dead`, `Alive`, `HpBelow`, `InsideArea`,
`OutsideArea`, `HasItem`, `Raw`.

### Patching into existing events

```csharp
// Insert a DisplayMessage immediately after a ForceAnimation in event 11810310.
// The alreadyPresent matcher makes it idempotent.
e.InsertAfter(11810310,
    match:         Instr.IsForceAnimation(1810800, 9060),
    toInsert:      Instr.DisplayMessage(6900690),
    alreadyPresent: Instr.IsDisplayMessage(6900690));
```

### The `Instr` factory

`Instr` builds typed instructions by name rather than bank/id pairs.

**Control flow**
| Method | Bank:ID | Notes |
|---|---|---|
| `InitializeEvent(id, slot)` | 2000:0 | Register event in constructor |
| `EndUnconditionally(endType)` | 1000:4 | 0=end, 1=restart |
| `IfConditionGroup(result, desired, target)` | 0:0 | AND/OR group composition |

**Conditions** (condGroup 0 = MAIN — blocks the event)
| Method | Bank:ID |
|---|---|
| `IfEventFlag(state, flagId, condGroup)` | 3:0 |
| `IfCharacterDeadAlive(state, entityId, condGroup)` | 4:0 |
| `IfHpRatio(entityId, compType, ratio, condGroup)` | 4:2 |
| `IfInsideArea(entityId, areaEntityId, desired, condGroup)` | 3:2 |
| `IfPlayerHasItem(itemType, itemId, desired, condGroup)` | 3:4 |

**Flags**
| Method | Bank:ID |
|---|---|
| `SetEventFlag(flagId, state)` | 2003:2 |

**Items & display**
| Method | Bank:ID |
|---|---|
| `AwardItemLot(itemLotId)` | 2003:4 |
| `DisplayMessage(messageId)` | 2007:4 |
| `DisplayStatusMessage(messageId)` | 2007:3 |
| `DisplayBanner(bannerType)` | 2007:2 |
| `DisplayBossHealthBar(entityId, state, slot, nameId)` | 2003:11 |

**Characters**
| Method | Bank:ID |
|---|---|
| `ForceAnimation(entityId, animId, ...)` | 2003:18 |
| `SetCharacterEnabled(entityId, state)` | 2004:5 |
| `KillCharacter(entityId, awardSouls)` | 2004:4 |
| `SetCharacterAI(entityId, state)` | 2004:1 |
| `SetCharacterHome(entityId, regionEntityId)` | 2004:13 |
| `SetCharacterImmortal(entityId, state)` | 2004:12 |
| `SetCharacterInvincible(entityId, state)` | 2004:15 |
| `WarpCharacter(entityId, destEntityId)` | 2004:41 |
| `HandleBossDefeat(entityId)` | 2003:12 |

**SFX / sound**
| Method | Bank:ID |
|---|---|
| `SpawnOneshotSfx(entityType, entityId, dummypolyId, sfxId)` | 2006:3 |
| `PlaySound(entityId, soundType, soundId)` | 2010:2 |
| `CameraVibration(vibrationId, ...)` | 2008:2 |

Use `Instr.Raw(bank, id, args...)` for anything not yet named.

---

## Flags — the section guard

```csharp
Flags.Section(11815700);                 // returns 5
Flags.IsSectionAllocated(evd, 11815700); // true/false
```

A map only allocates *some* flag sections. Writing to an unallocated section
silently does nothing at runtime. Validate the flag IDs you broadcast or watch
against the map's EMEVD before shipping.

---

## EventBuilder reference

Every `When*` method is a blocking wait in MAIN (condGroup=0). Combine them
with `WhenAllOf`/`WhenAnyOf` for AND/OR logic.

| Builder method | What it does |
|---|---|
| `WhenFlag(id, state)` | Wait until flag is On or Off |
| `WhenDead(entityId)` | Wait until entity is dead |
| `WhenAlive(entityId)` | Wait until entity is alive |
| `WhenHpBelow(entityId, ratio)` | Wait until HP ratio < value (0.0–1.0) |
| `WhenInsideArea(entity, area)` | Wait until entity is inside a region |
| `WhenOutsideArea(entity, area)` | Wait until entity is outside a region |
| `WhenAllOf(conds => ...)` | Wait until ALL sub-conditions are true |
| `WhenAnyOf(conds => ...)` | Wait until ANY sub-condition is true |
| `SetFlag(id, state)` | Set event flag on or off |
| `AwardItemLot(lotId)` | Give item lot to player |
| `DisplayMessage(msgId)` | Centered on-screen message |
| `DisplayStatusMessage(msgId)` | Status/explanation text box |
| `DisplayBanner(type)` | Big banner (1=Victory, 2=You Died…) |
| `DisplayBossHealthBar(entity, state, ...)` | Show/hide boss HP bar |
| `ForceAnimation(entity, animId, ...)` | Force-play an animation |
| `SetCharacterEnabled(entity, state)` | Enable/disable character |
| `KillCharacter(entity, awardSouls)` | Force character death |
| `SetCharacterAI(entity, state)` | Enable/disable AI |
| `SetCharacterHome(entity, region)` | Set home point |
| `SetCharacterImmortal(entity, state)` | Unkillable flag |
| `SetCharacterInvincible(entity, state)` | No-damage flag |
| `WarpCharacter(entity, dest)` | Teleport to entity |
| `HandleBossDefeat(entity)` | Boss-death music/souls sequence |
| `End()` | Unconditional end (run once) |
| `Restart()` | Unconditional restart (loop) |
| `Raw(bank, id, args...)` | Any instruction by bank/id |

---

## Full example — boss encounter mod

This is the pattern used by `DS1Mod.GoofyDemon`:

```csharp
// In your IGamePatcher.Patch(IPatchContext ctx):
var g = new GamePatch(ctx);

// 1. Swap the AI script
g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>
    bnd.SetFileContaining("223200_battle.lua", _luaBytecode));

// 2. Add FMG strings for HUD text + item name/info
g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd => {
    Texts.Set(bnd, Texts.EventText, 6900690, "Goofy Demon activated its mood: CHAOS");
    Texts.Set(bnd, Texts.GoodsName, 8000, "Demon Fart");
    Texts.Set(bnd, Texts.GoodsInfo, 8000, "Smells terrible.");
});

// 3. Add item params
g.EditParams(_paramdefBytes, repo => {
    repo.Edit("EquipParamGoods", p =>
        ParamRepository.AddClone(p, 384, 8000, "Demon Fart",
            r => r["maxNum"].Value = (ushort)1));
    repo.Edit("ItemLotParam", p =>
        ParamRepository.AddClone(p, 1000, 8500, "Demon Fart drop", r => {
            r["lotItemId01"].Value       = 8000;
            r["lotItemCategory01"].Value = LotCategory.Goods;
            r["getItemFlagId"].Value     = 50009000;
        }));
});

// 4. Add EMEVD events
g.EditEmevd("m18_01_00_00", e => {
    // Looping HUD event: show current mood while it's active
    e.DefineEvent(11819000, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(11815700, FlagState.On)
        .DisplayMessage(6900690)
        .WhenFlag(11815700, FlagState.Off)
        .Restart());

    // One-shot item drop on boss death
    e.DefineEvent(11819100, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenFlag(16, FlagState.On)          // Asylum Demon is dead
        .AwardItemLot(8500)
        .End());
});
```

---

## What the library replaces

Before `DS1Mod.Modding`, the GoofyDemon patch logic was ~150 lines of hand-rolled
SoulsFormats boilerplate spread across 6 methods. With the library it's ~50 lines
of intent, with idempotency and the constructor/flag gotchas handled automatically.
