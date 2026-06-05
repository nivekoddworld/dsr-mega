# DS1Mod Framework — How-To Reference

This document covers every API available to mod authors. Each section has a
brief explanation followed by a working code example drawn from `ItemsDemoMod`.

---

## 1. Conflict Detection

`GamePatch` records every file+selector it writes when constructed from an
`IPatchContext`. The host (`PatchContext`) collects these records across all
loaded mods and logs a warning when two mods write the same target.

**Mod authors never call `RecordEdit` directly.** It is called automatically
inside every `GamePatch` write method (`EditParams`, `EditEmevd`, `EditBnd3`,
`EditMsb`, `EditAi`, and so on). The only requirement is that you construct
`GamePatch` from the `IPatchContext` — not from the low-level overload.

```csharp
// Correct — conflict detection active
var g = new GamePatch(ctx);   // ctx is the IPatchContext passed to Patch()

// Low-level overload — no conflict detection
var g = new GamePatch(ctx.GameDir, ctx.BackupFile, ctx.Log);
```

When two mods write the same location the host prints:

```
[CONFLICT] DS1Mod.GoofyDemon and DS1Mod.ItemsDemo both modified
           script/m18_01_00_00.luabnd.dcx :: BND3:script/m18_01_00_00.luabnd.dcx
```

The second mod's write wins. To test this: install both `GoofyDemon` and
`ItemsDemo` — both patch `223200_battle.lua` in the Asylum luabnd.

---

## 2. EMEVD / EventBuilder

`g.EditEmevd(mapId, emevd => ...)` opens the map's `.emevd.dcx`, hands you an
`EmevdEditor`, and writes back. Call `emevd.DefineEvent(id, rest, ev => ...)`
to create (or idempotently replace) an event using the fluent `EventBuilder`.

The event is auto-registered at the top of event 0 (the constructor) so it
always runs — appending at the end can be skipped by DS1's multiplayer SKIPs.

```csharp
g.EditEmevd("m18_01_00_00", emevd =>
    emevd.DefineEvent(11819405, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(11819400, FlagState.On)
        .DisplayMessage(6900760)
        .SetFlag(11819402, FlagState.On)
        .WhenFlag(11819400, FlagState.Off)
        .Restart()));
```

`RestBehaviorType.Default` — event runs once then stops.  
`RestBehaviorType.Restart` — event loops (use for polling / repeating effects).

### Conditions

Each condition blocks the event until the test is true.

| Method | Blocks until |
|---|---|
| `WhenFlag(flagId, FlagState.On/Off)` | Event flag reaches the given state |
| `WhenDead(entityId)` | Character is dead |
| `WhenAlive(entityId)` | Character is alive |
| `WhenHpBelow(entityId, ratio)` | HP ratio drops below threshold (0.0–1.0) |
| `WhenInsideArea(entityId, areaEntityId)` | Entity enters an area region |
| `WhenOutsideArea(entityId, areaEntityId)` | Entity leaves an area region |
| `WhenCharacterHasSpEffect(entityId, spEffectId)` | SpEffect is active on entity |
| `WhenCharacterLosesSpEffect(entityId, spEffectId)` | SpEffect expires on entity |

### Compound conditions

**`WhenAllOf`** — AND group: block until every sub-condition is true simultaneously.

```csharp
ev.WhenAllOf(and => and
    .HpBelow(1810800, 0.5f)   // demon at half health
    .Alive(10000))            // and player is alive
```

**`WhenAnyOf`** — OR group: block until at least one sub-condition is true.

```csharp
ev.WhenAnyOf(or => or
    .Alive(1810800)
    .Flag(16, FlagState.Off))
```

DS1 supports up to 7 AND groups and 7 OR groups per event. `SubConditionBuilder`
methods: `.Flag`, `.Dead`, `.Alive`, `.HpBelow`, `.InsideArea`, `.OutsideArea`,
`.HasItem`, `.Raw(bank, id, args)`.

### Actions

| Method | Effect |
|---|---|
| `SetFlag(flagId, FlagState)` | Set an event flag on or off |
| `AwardItemLot(lotId)` | Give the player an item lot |
| `DisplayMessage(msgId)` | Centered on-screen message (Event_text FMG) |
| `DisplayStatusMessage(msgId)` | Status text box (Event_text FMG) |
| `DisplayBanner(bannerType)` | Big banner preset (1=Victory, 2=You Died…) |
| `DisplayBossHealthBar(entityId, state, slot, nameId)` | Show/hide boss HP bar |
| `ForceAnimation(entityId, animId)` | Force-play an animation on a character |
| `SetCharacterEnabled(entityId, state)` | Enable / disable visibility + collision |
| `KillCharacter(entityId, awardSouls)` | Force character death |
| `SetCharacterAI(entityId, state)` | Enable / disable character AI |
| `SetCharacterHome(entityId, regionEntityId)` | Set AI home point to a region |
| `SetCharacterImmortal(entityId, state)` | Immortal (unkillable) on/off |
| `SetCharacterInvincible(entityId, state)` | Invincible (no damage) on/off |
| `WarpCharacter(entityId, destEntityId)` | Teleport character to destination |
| `HandleBossDefeat(entityId)` | Trigger boss-death music + soul award |

### Control flow

| Method | Effect |
|---|---|
| `.End()` | Unconditional end — event runs once |
| `.Restart()` | Unconditional restart — event loops |
| `.Raw(bank, id, args)` | Append any instruction by bank/id with explicit args |

**Raw escape hatch example** — spawn a one-shot SFX (bank 2006, id 3):

```csharp
ev.Raw(2006, 3, 1, 1810800, 220, 5090)   // SpawnOneshotSfx at demon dummypoly 220
```

---

## 3. Lua AI / AiBuilder

`g.EditAi(mapId, npcFileId, ai => ...)` builds a Lua 5.0 AI script, compiles
it with `luac50`, and injects the bytecode into the map's `.luabnd.dcx`. The
builder emits a standard DS1 AI skeleton (REGISTER_GOAL, Activate/Update/
Terminate/Interupt functions) so you only describe the behaviour, not the boilerplate.

```csharp
g.EditAi("m18_01_00_00", "223200", ai => ai
    .Goal("Battle", goal => goal
        .Act(70, q => q
            .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 12)
            .Attack(animId: 3008, cancelTime: 8))
        .Act(30, q => q
            .SpinStep(cancelTime: 5)
            .LeaveTarget(Target.Enemy0, Dist.Far, cancelTime: 8))
        .OnInterrupt(_ => true)),
    luaId: "DemoAI");
```

`luaId` is the Lua identifier prefix for generated function names (default
`"Npc" + npcFileId`). Pass it when `npcFileId` starts with a digit.

### GoalBuilder methods

| Method | Purpose |
|---|---|
| `.Act(weight, q => ...)` | Weighted random action table entry. Weights are relative. |
| `.OnActivate(q => ...)` | Single deterministic on-activate sequence (no random table) |
| `.OnInterrupt(_ => bool)` | Whether the goal can be pre-empted (`true`/`false`) |
| `.Helper(name, body)` | Emit a named Lua helper function before the goal functions |

**Helper example:**

```csharp
goal.Helper("ClearMoods",
    "for _i = 0, 1 do\n" +
    "    ai:SetEventFlag(11819403 + _i, false)\n" +
    "end")
// emits:
//   function ClearMoods(ai, goal)
//       for _i = 0, 1 do
//           ai:SetEventFlag(11819403 + _i, false)
//       end
//   end
```

Call from an act with `.Raw("ClearMoods(ai, goal)")`.

### SubGoalQueue methods

Each method appends one `goal:AddSubGoal(...)` call and returns `this`.

| Method | Behaviour |
|---|---|
| `ApproachTarget(target, dist, cancelTime)` | Move to within `dist` of `target` |
| `Attack(animId, cancelTime)` | Play attack animation |
| `SpinStep(cancelTime)` | Evasive spin-step |
| `Wait(cancelTime)` | Idle wait for fixed time |
| `SidewayMove(direction, cancelTime)` | Lateral shuffle (0=left, 1=right) |
| `LeaveTarget(dist, cancelTime)` | Back away to `dist` |
| `WaitRandom(minTime, maxTime)` | Idle wait for random duration |
| `SetEventFlag(flagId, on)` | Set an event flag from Lua |
| `SetActiveFlagInRange(baseFlag, count, active)` | Clear a flag range; set one active index |
| `Raw(luaLine)` | Append any Lua line verbatim (4-space indent applied) |

**Target enum:** `Enemy0`, `Self`, `Friend0`, `Event`, `LocalPlayer`, `None`  
**Dist enum:** `Near`, `Middle`, `Far`, `Out`, `None`

### Override the luac50 binary path

```csharp
Luac50.Configure("/home/user/dsr-mega/tools/luac50");
// Call before any g.EditAi() call.
```

The default search order is: `<appBase>/tools/luac50[.exe]`, then `PATH`.

---

## 4. Items

### DefineSpEffect

Creates a `SpEffectParam` row by cloning a donor row and overwriting named fields.

```csharp
g.DefineSpEffect(paramdefs, new SpEffectDef
{
    Id             = 9000,   // unique row id (use 9000+ for mods)
    DonorId        = 110,    // clone from this existing row (benign vanilla effect)
    Duration       = 0f,     // 0 = instant; >0 = seconds
    HpRecoverPoint = 400,    // flat HP restored on application

    // Configure: any field not covered by named props
    Configure      = row => row["motionInterval"].Value = 0f,
});
```

`SpEffectDef` named fields: `Duration`, `HpRecoverPoint`, `HpRecoverRate`,
`StaminaRecoverPoint`, `MaxHpRate`, `PhysAtkPowerRate`, `MagicAtkPowerRate`,
`FireAtkPowerRate`, `ThunderAtkPowerRate`, `PhysDefRate`, `MagicDefRate`,
`FireDefRate`, `ThunderDefRate`. Use `Configure` for anything else.

### DefineGoods

Creates an `EquipParamGoods` row and writes name/description strings to every
locale's `item.msgbnd.dcx`. Idempotent — safe to call on every launch.

```csharp
// Consumable (SpEffectId set → auto-wires goodsType=0 + refCategory=1)
g.DefineGoods(paramdefs, new ItemDef
{
    Id            = 8100,
    DonorId       = 384,         // Estus Flask as base
    SpEffectId    = 9000,        // wires goodsType=consumable automatically
    Name          = "Goofy Draught",
    Description   = "Restores 400 HP.",
    LongDesc      = "A longer description shown in the inventory.",
    MaxCount      = 5,
    AllowQuickUse = true,        // enables D-pad cycling
});

// Key item (no use effect)
g.DefineGoods(paramdefs, new ItemDef
{
    Id           = 8101,
    Name         = "Stone Trinket",
    MaxCount     = 1,
    GoodsType    = 1,           // Event-type key item
    AllowQuickUse = false,
});
```

`ItemDef.SpEffectId = -1` (default) means no SpEffect — key item behaviour.  
`ItemDef.Configure` is an `Action<PARAM.Row>` called after the donor clone.

### DefineLot

Creates an `ItemLotParam` row for use with `AwardItemLot` (EMEVD) or
`PlaceTreasure` (MSB).

```csharp
// Infinite lot
g.DefineLot(paramdefs, new LotDef
{
    LotId        = 8600,
    ItemId       = 8100,
    Category     = LotCategory.Goods,
    Count        = 3,
    OnceOnlyFlag = -1,           // -1 = no restriction, re-awards every time
});

// Once-only lot (sets flag when obtained; skips on subsequent triggers)
g.DefineLot(paramdefs, new LotDef
{
    LotId        = 8601,
    ItemId       = 8101,
    Category     = LotCategory.Goods,
    Count        = 1,
    OnceOnlyFlag = 11819401,     // event flag set when item is obtained
});
```

`LotCategory` constants: `Goods`, `Weapon`, `Protector`, `Accessory`.

### PlaceTreasure

Adds a glowing `o0500` ground-pickup object and a `Treasure` event to the map.

```csharp
g.EditMsb("m18_01_00_00", msb => msb
    .PlaceTreasure(
        lotId:         8601,
        position:      new Vector3(52f, -2f, 103f),
        collisionName: null,   // auto — borrows from nearest existing o0500
        inChest:       false,
        entityId:      -1));
```

`collisionName` defaults to the collision of the nearest existing `o0500` object
in the map — safe when placing near an existing pickup. Override when placing in
an area with no existing pickups.

---

## 5. Item Use Detection

### DefineItemTrigger

Writes a `Restart` EMEVD event that bridges item use (SpEffect activation) to
an event flag pulse. Required for both the in-game EMEVD response and the C#
`hooks.ItemUsed` callback.

```csharp
g.DefineItemTrigger(
    mapId:         "m18_01_00_00",
    spEffectId:    9000,
    triggerFlagId: 11819400);
// eventId defaults to triggerFlagId; pass explicitly to override:
// g.DefineItemTrigger(..., eventId: 11819450);
```

The emitted event (internally, using `WhenCharacterHasSpEffect` /
`WhenCharacterLosesSpEffect`):

1. Wait until entity `10000` has SpEffect `9100` active
2. Set flag `11819400` **ON**
3. Wait until the SpEffect expires
4. Set flag `11819400` **OFF** → restart

### hooks.ItemUsed

Subscribe in `OnLoad`. Fires (within the 500ms poll loop) when the trigger
flag pulses ON.

```csharp
public override void OnLoad(IModContext ctx)
{
    ctx.Hooks.RegisterItemUsed(goodsId: 8100, triggerFlagId: 11819400);  // DraughtUseFlag
    ctx.Hooks.ItemUsed += OnItemUsed;
}

private void OnItemUsed(int goodsId)
{
    if (goodsId != 8100) return;
    Console.WriteLine("Player used the Goofy Draught.");

    var stats = ctx.Reader.GetPlayerStats();
    Console.WriteLine($"HP: {stats?.CurrentHp}/{stats?.MaxHp}");
}
```

### In-game EMEVD response vs. C# response

Both can coexist. The EMEVD response runs inside the game engine on the next
frame; the C# callback fires within ~500ms.

```csharp
// Patch(): EMEVD in-game response — reads the same flag written by DefineItemTrigger
g.EditEmevd(Map, emevd =>
    emevd.DefineEvent(11819405, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(11819400, FlagState.On)
        .DisplayMessage(6900760)            // shows text popup in-game
        .SetFlag(11819402, FlagState.On)    // permanent "used at least once" flag
        .WhenFlag(11819400, FlagState.Off)
        .Restart()));

// OnLoad(): C# response
ctx.Hooks.RegisterItemUsed(8100, 11819400);
ctx.Hooks.ItemUsed += id => Console.WriteLine($"used goods id {id}");
```

---

## Full ID table — demo mod ranges

| Range | Purpose |
|---|---|
| `8100–8101` | `EquipParamGoods` rows (Goofy Draught, Stone Trinket) |
| `9000` | `SpEffectParam` row (Goofy Draught effect) |
| `8600–8601` | `ItemLotParam` rows (infinite draught lot, once-only trinket lot) |
| `11819400` | EMEVD event + event flag — DraughtUseFlag / item trigger |
| `11819401` | Event flag — TrinketGetFlag (once-only obtained) |
| `11819402` | Event flag — FlagUsedDraught (permanent "used at least once") |
| `11819403` | Event flag — AI mood 0 (stomp act active) |
| `11819404` | Event flag — AI mood 1 (spin act active) |
| `11819405–11819409` | EMEVD event IDs (use response, AND demo, OR+boss demo, char control, raw) |
| `11819410` | Event flag — mid-fight reward given |
| `6900760–6900762` | `Event_text` FMG entries (on-use and status messages) |
