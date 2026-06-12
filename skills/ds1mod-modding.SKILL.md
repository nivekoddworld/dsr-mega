---
name: ds1-patching
description: Comprehensive skill for modifying DSR game files at launch using `GamePatch` and the modding helper library (`DS1Mod.Modding`).
---

# DS1Mod Modding — File Patching, Items, EMEVD, AI, MSB & ESD

Comprehensive skill for modifying DSR game files at launch using `GamePatch` and the modding helper library (`DS1Mod.Modding`). Every mod that patches files must implement `IGamePatcher` and use `GamePatch` inside `Patch(IPatchContext)`.

**Requires** `DS1Mod.Modding` and `SoulsFormats` project references:
```xml
<ProjectReference Include="..\..\framework\DS1Mod.Modding\DS1Mod.Modding.csproj" />
<ProjectReference Include="..\..\..\lib\SoulsFormats\SoulsFormats\SoulsFormats.csproj" />
```

---

## 1. GamePatch — The Central File Editor

```csharp
public void Patch(IPatchContext ctx)
{
    var g = new GamePatch(ctx);  // Preferred — wires backup + conflict detection

    // Or low-level (no conflict detection):
    var g = new GamePatch(ctx.GameDir, ctx.BackupFile, ctx.Log);
}
```

All edits are idempotent. Files are backed up (`.bak`) on first access. Edits are decompressed, modified, and re-compressed with the original DCX type.

Patch-phase plumbing logs (`[DEBUG] DCX decompressed...`) are off by default; set `ModdingLog.Verbose = true` to see them when debugging the framework itself.

| Method | Purpose |
|--------|---------|
| `EditBnd3(relPath, edit)` | Edit a single DCX-wrapped BND3 archive |
| `EditBnd3Glob(relDir, fileName, edit)` | Edit ALL matching files recursively (e.g. all `menu.msgbnd.dcx`) |
| `EditEmevd(mapId, edit)` | Edit a map's EMEVD event script |
| `EditEsd(relPath, esdName, edit)` | Edit a named ESD inside an esdbnd |
| `EditEsdBySize(relDir, vanillaSize, edit)` | Edit all ESDs of a given vanilla size (bonfire bulk patch) |
| `EditActionEsd(esd, edit)` | Edit player (`c0000`) or enemy (`enemyCommon`) action ESD |
| `EditAi(mapId, npcFileId, build)` | Compile and inject a Lua AI script |
| `EditMsb(mapId, edit)` | Edit a map's MSB (place pickups, enemies) |
| `EditParams(paramdefBnd, edit)` | Edit `GameParam.parambnd.dcx` (item/effect rows) |
| `DefineGoods(paramdefBnd, def)` | Create a new goods item (EquipParamGoods + FMG text) |
| `DefineRing(paramdefBnd, def)` | Create a new ring (EquipParamAccessory + FMG text) |
| `DefineLot(paramdefBnd, def)` | Create a new item lot (drop table) |
| `PlaceWorldPickup(paramdefBnd, def)` | Lot + ground treasure + hide-after-pickup event, one call |
| `DefineSpEffect(paramdefBnd, def)` | Create a new status effect (SpEffectParam) |
| `DefineItemTrigger(mapId, spEffectId, triggerFlagId)` | Bridge SpEffect → event flag → C# callback |
| `EditBonfireEsd(edit)` | Edit shared bonfire ESD (multi-mod cooperative) |
| `AddBonfireMenuItem(talkId, gateFlag, flagId)` | Add a bonfire menu item |
| `AddBonfireMenuItemIf(condition, talkId)` | Add a conditional bonfire menu item |

---

## 2. ID Allocation — Always Use It

**Never hardcode IDs.** Use `IPatchContext.AllocateId()` and `IPatchContext.AllocateIds()`:

```csharp
public void Patch(IPatchContext ctx)
{
    // Single IDs
    int myGoodsId   = ctx.AllocateId(IdSpaces.EquipParamGoods);
    int mySpEffect  = ctx.AllocateId(IdSpaces.SpEffectParam);
    int myLotId     = ctx.AllocateId(IdSpaces.ItemLotParam);
    int myEventFlag = ctx.AllocateId(IdSpaces.EventFlags(MapIds.UndeadAsylum));
    int myEmevdEvt  = ctx.AllocateId(IdSpaces.EmevdEvents(MapIds.UndeadAsylum));
    int myEntityId  = ctx.AllocateId(IdSpaces.MsbEntities(MapIds.UndeadAsylum));
    int myFmgMsgId  = ctx.AllocateId(IdSpaces.EventText);
    int myGetFlag   = ctx.AllocateId(IdSpaces.ItemObtainedFlags);

    // Contiguous blocks
    int flagBase = ctx.AllocateIds(IdSpaces.EventFlags(MapIds.UndeadAsylum), 5);
    int evtBase  = ctx.AllocateIds(IdSpaces.EmevdEvents(MapIds.UndeadAsylum), 3);
    int msgBase  = ctx.AllocateIds(IdSpaces.EventText, 3);
}
```

Allocations are persistent — same mod always gets the same IDs across runs (save-game compatible).

---

## 3. Creating Items

### 3a. SpEffect (Status Effect)

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id             = mySpEffectId,
    DonorId        = 110,            // Benign vanilla effect to clone from
    Duration       = 8f,             // 0 = instant, >0 = lingering
    HpRecoverPoint = 400,            // Flat HP restored on application
    MaxHpRate      = 1.0f,          // Stat multipliers (1.0 = unchanged)
    PhysAtkPowerRate = 1.5f,        // +50% physical attack
    Configure      = row => row["motionInterval"].Value = 0f, // Raw field override
});
```

### 3b. Goods (Consumable / Key Item)

```csharp
g.DefineGoods(paramdefBnd, new ItemDef
{
    Id           = myGoodsId,
    DonorId      = 384,             // Estus Flask as base (healing consumable)
    Name         = "My Healing Herb",
    Description  = "Restores 400 HP. Tastes grassy.",
    LongDesc     = "A rare herb found only in the Undead Asylum.",
    MaxCount     = 5,               // Stack size
    SpEffectId   = mySpEffectId,    // Effect applied on use (>0 = consumable)
    AllowQuickUse = true,           // Can be assigned to D-pad quick slots
    IsConsume    = true,            // Removed on use
    IsDeposit    = true,            // Can be stored in bottomless box
    IsDrop       = true,            // Can be dropped
    GoodsType    = null,            // null = auto (0 for consumable, 1 for non-SpEffect)
    Configure    = null,            // Raw field escape hatch
});

// Key item (no use effect, non-consumable):
g.DefineGoods(paramdefBnd, new ItemDef
{
    Id          = myKeyItemId,
    DonorId     = 384,
    Name        = "Mysterious Key",
    Description = "Opens something. Probably.",
    LongDesc    = "A key of unknown origin.",
    MaxCount    = 1,
    GoodsType   = 1,               // Event item (non-usable)
    AllowQuickUse = false,
});
```

### 3c. Lot (Drop Table)

```csharp
// Infinite lot (re-awards every time):
g.DefineLot(paramdefBnd, new LotDef
{
    LotId        = myLotId,
    ItemId       = myGoodsId,
    Category     = LotCategory.Goods,  // 0x40000000 for goods
    Count        = 3,                  // 3 items per drop
    OnceOnlyFlag = -1,                 // -1 = repeatable
    EnableLuck   = true,
});

// Once-only lot (sets flag after first acquisition):
g.DefineLot(paramdefBnd, new LotDef
{
    LotId        = myOnceLotId,
    ItemId       = myKeyItemId,
    Category     = LotCategory.Goods,
    Count        = 1,
    OnceOnlyFlag = myGetFlag,          // Flag set permanently after pickup
});

// Multi-slot lot (up to 8 entries):
g.DefineLot(paramdefBnd, new LotDef
{
    LotId = myMultiLotId,
    Entries = new List<LotEntry>
    {
        new() { ItemId = 101, Category = LotCategory.Weapon,  Count = 1, Weight = 50 },
        new() { ItemId = 201, Category = LotCategory.Goods,   Count = 3, Weight = 50 },
    },
});
```

### 3d. Ring (Accessory) + World Pickup — One-Call Recipes

```csharp
int ringId   = ctx.AllocateId("EquipParamAccessory");
int lotId    = ctx.AllocateId(IdSpaces.ItemLotParam);
int getFlag  = ctx.AllocateId(IdSpaces.ItemObtainedFlags);
int entityId = ctx.AllocateId(IdSpaces.MsbEntities(MapIds.UndeadAsylum));
int hideEvt  = ctx.AllocateId(IdSpaces.EmevdEvents(MapIds.UndeadAsylum));

g.DefineRing(paramdefs, new RingDef
{
    Id = ringId,                 // DonorId default 100 = Havel's Ring
    Name = "My Ring",
    Description = "Short text.",
    LongDesc = "Lore text.",
    SpEffectId = -1,             // -1 = effect implemented in C# at runtime
});

// Lot + MSB treasure + EMEVD hide-after-pickup, all at once:
g.PlaceWorldPickup(paramdefs, new WorldPickupDef
{
    LotId = lotId, ItemId = ringId, Category = LotCategory.Accessory,
    ObtainedFlag = getFlag, Map = MapIds.UndeadAsylum,
    Position = new Vector3(-13.8f, 190.2f, 15.8f),
    EntityId = entityId, HideEventId = hideEvt,
});
```

Both update rows in place on redeploy (no `.bak` restore needed between iterations). Detect "is my ring equipped" at runtime via the inventory heap signature — see `RingEquipDetector` in `DS1Mod.ChrumburGoofyRings` (ring slots at signature−0x318/−0x314).

### 3e. ItemTrigger (EMEVD → C# Bridge)

Writes an EMEVD event that sets a flag while the player has a SpEffect active, then clears it when the SpEffect expires. Your C# code can react via `hooks.ItemUsed`:

```csharp
// In Patch():
g.DefineItemTrigger(MapIds.UndeadAsylum,
    spEffectId:    mySpEffectId,
    triggerFlagId: myUseFlag,
    eventId:       myTriggerEvent);

// In OnLoad():
ctx.Hooks.RegisterItemUsed(myGoodsId, myUseFlag);
ctx.Hooks.ItemUsed += goodsId =>
{
    if (goodsId == myGoodsId)
        Console.WriteLine("Player used my item!");
};
```

---

## 4. Editing EMEVD (Event Scripts)

### 4a. EventBuilder — Fluent API

```csharp
g.EditEmevd(MapIds.UndeadAsylum, emevd =>
{
    // Simple event: when flag is ON, show message, then wait for OFF, restart
    emevd.DefineEvent(myEventId, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(myFlag, FlagState.On)
        .DisplayMessage(myMsgId)
        .WhenFlag(myFlag, FlagState.Off)
        .Restart());

    // One-shot: when boss HP < 50%, award item lot
    emevd.DefineEvent(myMidfightEvent, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenHpBelow(bossEntityId, 0.5f)
        .AwardItemLot(myLotId)
        .DisplayStatusMessage(myMsgId)
        .End());

    // Compound condition: OR group
    emevd.DefineEvent(myOrEvent, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenAnyOf(or => or
            .Flag(flagA, FlagState.On)
            .Flag(flagB, FlagState.On))
        .DisplayMessage(myMsgId)
        .End());

    // Character control showcase
    emevd.DefineEvent(myCtrlEvent, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenFlag(someFlag, FlagState.On)
        .SetCharacterEnabled(entityId, EnabledState.Enabled)
        .SetCharacterAI(entityId, EnabledState.Enabled)
        .SetCharacterImmortal(entityId, EnabledState.Disabled)
        .SetCharacterInvincible(entityId, EnabledState.Disabled)
        .SetCharacterHome(entityId, regionEntityId: 1810100)
        .SetSpEffect(entityId, mySpEffectId)
        .End());

    // Area-based: inside/outside trigger
    emevd.DefineEvent(myAreaEvent, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenInsideArea(PlayerEntity, areaEntityId: 1812100)
        .Raw(2006, 3, 1, demonEntity, 220, 5090)  // SpawnOneshotSfx
        .WhenOutsideArea(PlayerEntity, areaEntityId: 1812100)
        .Restart());
});
```

### 4b. EMEVD Condition Reference

| EventBuilder Method | EMEVD Instruction | Purpose |
|---|---|---|
| `.WhenFlag(id, state)` | 3:0 | Block until flag reaches state |
| `.WhenCharacterHasSpEffect(entity, spEffectId)` | 4:5 | Block until entity has SpEffect active |
| `.WhenCharacterLosesSpEffect(entity, spEffectId)` | 4:5 (inverted) | Block until entity loses SpEffect |
| `.WhenDead(entity)` | 4:0 | Block until entity is dead |
| `.WhenAlive(entity)` | 4:0 (inverted) | Block until entity is alive |
| `.WhenHpBelow(entity, ratio)` | 4:2 | Block until entity HP < ratio |
| `.WhenHpAbove(entity, ratio)` | 4:2 (inverted) | Block until entity HP > ratio |
| `.WhenInsideArea(entity, areaEntityId)` | 3:2 | Block until entity inside area |
| `.WhenOutsideArea(entity, areaEntityId)` | 3:2 (inverted) | Block until entity outside area |
| `.WhenAllOf(mainCond, builder)` | 0:0 | AND compound condition |
| `.WhenAnyOf(builder)` | 0:0 | OR compound condition |
| `.IfRandomPercent(condGroup, chance)` | 3:8 | Random percentage check |

### 4c. EMEVD Action Reference

| EventBuilder Method | EMEVD Instruction | Purpose |
|---|---|---|
| `.SetFlag(id, state)` | 2003:2 | Set event flag |
| `.AwardItemLot(lotId)` | 2003:4 | Give player an item lot |
| `.DisplayMessage(msgId)` | 2007:4 | Pop centered on-screen message |
| `.DisplayStatusMessage(msgId)` | 2007:3 | Show status/explanation text |
| `.DisplayBanner(bannerType)` | 2007:2 | Show boss banner |
| `.DisplayBossHealthBar(entity, state)` | 2003:11 | Show/hide boss HP bar |
| `.ForceAnimation(entity, animId)` | 2003:18 | Force-play animation |
| `.SetCharacterEnabled(entity, state)` | 2004:5 | Enable/disable character |
| `.SetCharacterAI(entity, state)` | 2004:1 | Enable/disable AI |
| `.SetCharacterHome(entity, regionId)` | 2004:13 | Set home region |
| `.SetCharacterImmortal(entity, state)` | 2004:12 | Toggle immortality |
| `.SetCharacterInvincible(entity, state)` | 2004:15 | Toggle invincibility |
| `.SetSpEffect(entity, spEffectId)` | 2004:8 | Apply SpEffect to character |
| `.WarpCharacter(entity, destEntityId)` | 2004:41 | Teleport character |
| `.KillCharacter(entity, awardSouls)` | 2004:4 | Force character death |
| `.HandleBossDefeat(entity)` | 2003:12 | Trigger boss death sequence |
| `.SetObjectEnabled(entity, state)` | 2005:3 | Enable/disable object |
| `.SetPlayerRespawnPoint(respawnId)` | 2004:7 | Set player's bonfire respawn |
| `.End()` | 1000:4 | Terminate event |
| `.Restart()` | 1000:4 | Restart event loop |
| `.Raw(bank, id, args...)` | Any | Escape hatch for any instruction |

### 4d. InsertAfter Pattern

Insert an instruction after a specific instruction match (e.g., after a ForceAnimation call):

```csharp
g.EditEmevd(MapIds.UndeadAsylum, emevd =>
{
    emevd.InsertAfter(
        eventId: 11810310,
        after: Instr.IsForceAnimation(entityId, animId),
        toInsert: Instr.DisplayMessage(fartMsgId),
        alreadyPresent: Instr.IsDisplayMessage(fartMsgId)  // idempotency guard
    );
});
```

### 4e. Boss EMEVD Patching

Strip model-specific instructions from boss intro events when replacing a boss model:

```csharp
using DS1Mod.Modding; // for EmevdExtensions

g.EditEmevd(mapId, editor =>
{
    // Strip ForceAnimationPlayback and WarpCharacter from boss intro
    editor.RemoveInstructions(eventId, (2003, 18), (2004, 41));

    // Or apply pre-defined patches:
    editor.ApplyBossPatches(boss.EmevdPatches);
});
```

---

## 5. FMG Text Editing

```csharp
g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
{
    Texts.Set(bnd, Texts.EventText, myMsgId, "Display Message Text");
});

g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
{
    Texts.Set(bnd, Texts.GoodsName,        myGoodsId, "Item Name");
    Texts.Set(bnd, Texts.GoodsDescription, myGoodsId, "Short description");
    Texts.Set(bnd, Texts.GoodsLongDesc,    myGoodsId, "Long lore description");
});
```

### FMG Name Constants

```csharp
Texts.EventText         // "Event_text"       — menu.msgbnd — on-screen messages
Texts.GoodsName         // "Item_name"        — item.msgbnd
Texts.GoodsDescription  // "Item_description" — item.msgbnd
Texts.GoodsLongDesc     // "Item_long_desc"   — item.msgbnd
Texts.WeaponName        // "Weapon_name"
Texts.ArmorName         // "Armor_name"
Texts.RingName          // "Accessory_name"
Texts.SpellName         // "Magic_name"
```

---

## 6. MSB Editing (Map Studio Binary)

### 6a. Place Treasure (Ground Pickup)

```csharp
g.EditMsb(MapIds.UndeadAsylum, msb => msb
    .PlaceTreasure(
        lotId:    myLotId,            // ItemLotParam row ID
        position: new Vector3(52f, -2f, 103f),  // World position
        collisionName: null,          // Auto-detect nearest collision
        inChest: false,               // True = loot appears inside a chest
        entityId: myEntityId          // For EMEVD reference (hide on collect)
    ));
```

### 6b. Place Enemy (MSB Part)

```csharp
g.EditMsb(MapIds.UndeadAsylum, msb => msb
    .PlaceEnemy(
        entityId:     myEntityId,
        modelName:    "c1201",         // Enemy model ID (e.g., "c1201" = Small Rat)
        npcParamId:   120100,          // NPC stats param ID
        thinkParamId: 120100,          // AI think param ID
        position:     new Vector3(48f, -62.5f, 103f),
        collisionName: "h0014B0"       // Optional collision mesh
    ));
```

### 6c. EnemyPatcher — Batch MSB Enemy Updates

```csharp
// Update a single enemy by entity ID:
EnemyPatcher.UpdateEnemyByEntityId(gameDir, mapId, entityId, enemy =>
{
    enemy.ModelName = "c5270";   // Ornstein
    enemy.NPCParamID = 527000;
});

// Batch update multiple enemies in one pass:
EnemyPatcher.BatchUpdateEnemies(gameDir, mapId, new()
{
    [1010700] = e => e.ModelName = "c2250",  // Taurus Demon slot → Capra Demon
    [1010750] = e => e.ModelName = "c2240",  // Capra Demon slot → Taurus Demon
});
```

---

## 7. AI Editing (Lua AI Scripts)

### 7a. AiBuilder — Fluent C# AI DSL

Generates Lua 5.0 source, compiles it via `luac50`, and injects the bytecode into `script/<mapId>.luabnd.dcx`.

```csharp
g.EditAi(MapIds.UndeadAsylum, "223200", ai => ai
    .Goal("Battle", goal => goal
        // Weighted act selection (weights are relative):
        .Act(70, q => q                                    // 70% chance
            .ApproachTarget(Target.Enemy0, distMeters: 8f, cancelTime: 10)
            .Attack(animId: 3007, cancelTime: 10)
            .Wait(2))
        .Act(30, q => q                                    // 30% chance
            .SpinStep(cancelTime: 5)
            .LeaveTarget(Target.Enemy0, distMeters: 10f, cancelTime: 5)
            .WaitRandom(minTime: 1.0f, maxTime: 2.0f))
        .OnInterrupt(_ => true)));          // Allow pre-emption
```

### 7b. AiBuilder API

| Method | Purpose |
|--------|---------|
| `ApproachTarget(target, distMeters, cancelTime)` | Move toward target until within distance |
| `Attack(animId, cancelTime)` | Play attack animation |
| `ComboAttack(animId, cancelTime)` | Chain combo attack |
| `ComboFinal(animId, cancelTime)` | Final combo hit |
| `TunableSpinAttack(animId, cancelTime)` | Spin/slam attack |
| `DashAttack(animId, cancelTime)` | Dash-into-attack |
| `SpinStep(animId, cancelTime)` | Evasion sidestep |
| `Guard(cancelTime)` | Block posture |
| `Wait(cancelTime)` | Idle for fixed duration |
| `WaitRandom(minTime, maxTime)` | Idle for random duration |
| `Turn(target, cancelTime)` | Face target |
| `BackToHome(cancelTime)` | Return to spawn |
| `SidewayMove(target, direction, cancelTime)` | Strafe sideways |
| `LeaveTarget(target, distMeters, cancelTime)` | Back away |
| `SetEventFlag(flagId, on)` | Set flag from AI |
| `SetActiveFlagInRange(baseFlag, count, active)` | Set one flag, clear others |
| `Raw(luaLine)` | Raw Lua escape hatch |

### 7c. OnActivate (Deterministic, No Weighted Selection)

```csharp
.Goal("Battle", goal => goal
    .OnActivate(q => q
        .ApproachTarget(Target.Enemy0, distMeters: 8f, cancelTime: 10)
        .Attack(animId: 3008, cancelTime: 5))
    .OnInterrupt(_ => true))
```

### 7d. Helper Functions

```csharp
g.EditAi(MapIds.UndeadAsylum, "223200", ai => ai
    .Goal("Battle", goal => goal
        .Helper("ClearMoods", "for i=0,9 do ai:SetEventFlag(11815700+i,false) end")
        .OnActivate(q => q
            .Raw("ClearMoods(ai, goal)")
            .ApproachTarget(Target.Enemy0, distMeters: 8f, cancelTime: 10)
            .Attack(animId: 3007, cancelTime: 10))
        .OnInterrupt(_ => true)));
```

### 7e. Luac50 Setup

The AI compiler requires a Lua 5.0 compiler binary (`luac50.exe` on Windows). Place it at:

- `<app>/tools/luac50.exe` (auto-detected)
- Or on `PATH`
- Or configure manually: `Luac50.Configure(@"C:\path\to\luac50.exe")`

---

## 8. ESD Editing (Dialog & Animation State Machines)

**ESD** (EZState) is FromSoft's graph-based state machine system. The framework provides two editors:

### 8a. Talk ESD — NPC Dialog & Bonfire Menus

Files: `script/talk/t{npcId}.talkesdbnd.dcx`

```csharp
// Edit a single talk ESD:
g.EditEsd("script/talk/t200000.talkesdbnd.dcx", "200000.esd", esd =>
{
    // Add a transition
    esd.AddTransition(groupId: 1, fromState: 1, toState: 10,
        evaluator: EsdBytecode.Always());

    // Add entry commands
    esd.AddEntryCommand(1, 10, TalkCmd.SetEventFlag(11810400, true));
});

// Bulk-edit all bonfire ESDs by size:
g.EditEsdBySize("script/talk", vanillaSize: 23012, esd =>
{
    esd.SetTalkListGateFlag(1, 4, 15000100, -1); // Unlock Level Up
});
```

### 8b. Talk ESD Condition Functions

| Helper | Underlying | Purpose |
|--------|-----------|---------|
| `EsdBytecode.GetEventFlag(flagId)` | Fn15 | Check flag state |
| `EsdBytecode.GetMenuSelection()` | Fn23 | Current bonfire menu selection |
| `EsdBytecode.GetDialogButtonResult()` | Fn22 | Button pressed in dialog |
| `EsdBytecode.IsGenericDialogOpen(personId)` | Fn58 | Dialog open check |
| `EsdBytecode.GetTimeInState()` | Fn103 | Seconds in current state |
| `EsdBytecode.DialogClosedWithButton(button)` | Fn58+Fn22 | Dialog just closed with answer |
| `EsdBytecode.SelectedItem(listIndex)` | Fn23+Eq | Player selected menu item X |
| `EsdBytecode.Always()` | Push 1 | Always passes |
| `EsdBytecode.Never()` | Push 0 | Never passes |

### 8c. Talk ESD Commands

| Helper | Bank:ID | Purpose |
|--------|---------|---------|
| `TalkCmd.SetEventFlag(id, on)` | 1:11 | Set flag (3187× use — most common) |
| `TalkCmd.OpenGenericDialog(type, msgId, btnType, numBtns, unk)` | 1:17 | Show yes/no dialog |
| `TalkCmd.AddTalkListData(idx, talkId, gateFlag=-1)` | 1:19 | Add bonfire menu item |
| `TalkCmd.AddTalkListDataIf(condition, idx, talkId)` | 5:19 | Conditional menu item |
| `TalkCmd.ClearTalkListData()` | 1:20 | Clear menu list |
| `TalkCmd.ShowShopMessage(a, b, c)` | 1:10 | Wares message |

### 8d. Bytecode Composition

```csharp
// AND, OR, NOT — all safe to nest:
var condition = EsdBytecode.And(
    EsdBytecode.GetEventFlag(11810000),         // Flag is ON
    EsdBytecode.Not(EsdBytecode.Fn3()));         // AND not stunned

// Comparisons:
EsdBytecode.Eq(a, b)    // a == b
EsdBytecode.Ne(a, b)    // a != b
EsdBytecode.Ge(a, b)    // a >= b

// Raw hex:
EsdBytecode.FromHex("4F 82 01 00 00 00 85 41 95 A1");
```

### 8e. Action ESD — Animation States

Files: `chr/c0000.esd.dcx` (player), `chr/enemyCommon.esd.dcx` (enemies)

```csharp
g.EditActionEsd("c0000", esd =>
{
    // Prevent attacking while stunned (insert at highest priority):
    esd.InsertTransition(0, 0, 0,
        ActionEsdBytecode.Not(ActionEsdBytecode.Fn3()), index: 0);
});
```

### 8f. Action ESD Condition Functions

| Helper | Freq | Inferred Purpose |
|--------|------|-----------------|
| `Fn0()` | 398× | Always-true / default |
| `Fn112()` | 240× | Attack animation / combo gating |
| `Fn109()` | 236× | Button release / state routing |
| `Fn2()` | 223× | World state (airborne, stamina, animation) |
| `Fn3()` | 219× | Stun / equipment / buff checks |
| `Fn116()` | 216× | Spell / item / ability gating |
| `Fn111()` | 204× | Dodge / roll / backstab timing |
| `Fn115()` | 204× | Movement logic |
| `Fn104()` | 195× | Inventory / stance / animation sync |
| `EnemyFn107()` | 148× | Enemy AI behavior routing |
| `EnemyFn118()` | 146× | Enemy AI behavior |
| `EnemyFn120()` | 109× | Enemy AI decision |

### 8g. Action ESD Commands (⚠️ Limited Coverage)

```csharp
ActionCmd.SetUpperBodyAnimation(animId, duration)
ActionCmd.SetLowerBodyAnimation(animId, duration)
ActionCmd.CancelAnimation()
ActionCmd.SetItemInUse(active)
ActionCmd.SyncAnimationAtInit(active)
ActionCmd.RawCommand(bank, cmdId, args...)  // Escape hatch
```

---

## 9. Bonfire Menu Customization

### 9a. Unlock Vanilla Items

```csharp
g.EditBonfireEsd(esd =>
{
    esd.SetTalkListGateFlag(1, 4, 15000100, -1);  // Level Up always visible
    esd.SetTalkListGateFlag(1, 4, 15000170, -1);  // Reverse Hollowing always visible
    esd.SetTalkListGateFlag(1, 4, 15000270, -1);  // Leave always visible
});
```

### 9b. Add Custom Menu Items

```csharp
// Simple: always shows "Spawn Rat" → sets flag 90 when selected
g.AddBonfireMenuItem(talkId: 15001200, gateFlag: -1, flagId: 90);

// Conditional: shows only when flag 11815700 is OFF
g.AddBonfireMenuItemIf(
    EsdBytecode.Not(EsdBytecode.GetEventFlag(11815700)),
    talkId: 15001200);
```

The bonfire UI has a fixed layout. Custom items use allocated menu slots (starting at 13+). Selecting the item sets the `flagId` so EMEVD can listen for it.

---

## 10. Direct BND3 Editing

For operations not covered by the high-level APIs:

```csharp
g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>
{
    // Replace an entry's bytes by filename
    bnd.SetFileContaining("223200_battle.lua", replacementBytes);

    // Or iterate and modify entries directly
    foreach (var file in bnd.Files)
    {
        if (file.Name.EndsWith("c0000.esd"))
            file.Bytes = modifiedBytes;
    }
});
```

---

## 11. Parameter Editing (Raw)

For direct PARAM manipulation beyond `DefineGoods`/`DefineLot`/`DefineSpEffect`:

```csharp
byte[] paramdefBnd = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

g.EditParams(paramdefBnd, repo =>
{
    repo.Edit("EquipParamGoods", p =>
    {
        // Read existing row
        var row = p[384]; // Estus Flask
        if (row != null)
        {
            row["maxNum"].Value = (ushort)99;  // 99 Estus
            row["isDeposit"].Value = (byte)0;   // Can't bottomless box
        }

        // Clone a donor
        ParamRepository.AddClone(p, donorId: 384, newId: 8000,
            name: "Custom Estus", row =>
            {
                row["maxNum"].Value = (ushort)5;
                row["refId"].Value = mySpEffectId;
            });
    });

    repo.Edit("ItemLotParam", p =>
    {
        ParamRepository.AddClone(p, donorId: 1000, newId: myLotId,
            name: "my_lot", row =>
            {
                row["lotItemId01"].Value = myGoodsId;
                row["lotItemNum01"].Value = (byte)1;
                row["getItemFlagId"].Value = myGetFlag;
            });
    });
});
```

### LotCategory Bitfield Values

```csharp
LotCategory.Weapon    // 0x00000000
LotCategory.Protector // 0x10000000  (armor)
LotCategory.Accessory // 0x20000000  (rings)
LotCategory.Goods     // 0x40000000  (consumables / key items)
```

⚠️ Verified against DarkSoulsItemRandomizer. A wrong category = ghost pickup that awards nothing.

---

## 12. Embedded Resources

Your mod needs `paramdef.paramdefbnd.dcx` (DS1 paramdefs — the game doesn't ship them) and optionally pre-compiled Lua bytecode. Embed them in the `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="paramdef.paramdefbnd.dcx">
    <LogicalName>paramdef.paramdefbnd.dcx</LogicalName>
  </EmbeddedResource>
  <EmbeddedResource Include="223200_battle.luac">
    <LogicalName>223200_battle.luac</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

Load at runtime:

```csharp
private static byte[] GetEmbeddedResource(string name)
{
    using Stream? s = typeof(MyMod).Assembly.GetManifestResourceStream(name);
    if (s is null) return [];
    using var ms = new MemoryStream();
    s.CopyTo(ms);
    return ms.ToArray();
}
```

---

## 13. Complete Patch Phase Template

```csharp
public void Patch(IPatchContext ctx)
{
    var g = new GamePatch(ctx);
    byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

    // 1. Allocate all IDs FIRST
    int spEffectId  = ctx.AllocateId(IdSpaces.SpEffectParam);
    int goodsId     = ctx.AllocateId(IdSpaces.EquipParamGoods);
    int lotId       = ctx.AllocateId(IdSpaces.ItemLotParam);
    int flagBase    = ctx.AllocateIds(IdSpaces.EventFlags(MapIds.UndeadAsylum), 3);
    int evtBase     = ctx.AllocateIds(IdSpaces.EmevdEvents(MapIds.UndeadAsylum), 3);
    int entityId    = ctx.AllocateId(IdSpaces.MsbEntities(MapIds.UndeadAsylum));
    int msgBase     = ctx.AllocateIds(IdSpaces.EventText, 2);

    // 2. Define SpEffect
    g.DefineSpEffect(paramdefs, new SpEffectDef { ... });

    // 3. Define goods + lots
    g.DefineGoods(paramdefs, new ItemDef { ... });
    g.DefineLot(paramdefs, new LotDef { ... });

    // 4. Write FMG text
    g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        Texts.Set(bnd, Texts.EventText, msgBase, "My Message"));

    // 5. Edit EMEVD
    g.EditEmevd(MapIds.UndeadAsylum, emevd => { ... });

    // 6. Edit MSB
    g.EditMsb(MapIds.UndeadAsylum, msb => { ... });

    // 7. Edit AI (optional)
    g.EditAi(MapIds.UndeadAsylum, npcId, ai => { ... });

    // 8. Define item trigger (for C# hook)
    g.DefineItemTrigger(MapIds.UndeadAsylum, spEffectId, useFlag, triggerEvent);

    ctx.Log("Patch complete!");
}
```
