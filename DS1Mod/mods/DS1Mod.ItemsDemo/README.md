# Items & SpEffect API — What Was Added Today

Everything in this document was added on 2026-06-05. It covers all new
patch-time and runtime APIs for creating items, placing them in the world,
defining what happens on use, and reacting to item use in C# and in-game.

---

## New patch-time APIs (DS1Mod.Modding)

### DefineSpEffect

Creates a `SpEffectParam` row — the engine mechanism behind all triggered
effects (HP restore, buffs, status infliction).

```csharp
g.DefineSpEffect(paramdefs, new SpEffectDef
{
    Id             = 9100,   // unique SpEffectParam row id
    DonorId        = 7000,   // clone from this existing row
    Duration       = 0f,     // 0 = instant; seconds otherwise
    HpRecoverPoint = 400,    // flat HP restored on application
});
```

**`SpEffectDef` fields:**

| Field | Effect |
|---|---|
| `Duration` | How long the effect lasts. `0` = instant (fires once). |
| `HpRecoverPoint` | Flat HP restored instantly |
| `HpRecoverRate` | HP/second over `Duration` |
| `StaminaRecoverPoint` | Flat stamina restored |
| `MaxHpRate` | Max HP multiplier (`1.2` = +20%) |
| `PhysAtkPowerRate` | Physical attack multiplier |
| `MagicAtkPowerRate` | Magic attack multiplier |
| `FireAtkPowerRate` | Fire attack multiplier |
| `ThunderAtkPowerRate` | Lightning attack multiplier |
| `PhysDefRate` | Physical defense multiplier |
| `MagicDefRate` | Magic defense multiplier |
| `FireDefRate` | Fire defense multiplier |
| `ThunderDefRate` | Lightning defense multiplier |
| `Configure` | Raw `Action<PARAM.Row>` callback for any field not listed above |

---

### DefineGoods

Creates a row in `EquipParamGoods` and writes name/description strings to every
locale's `item.msgbnd.dcx`. Idempotent — safe to call every launch.

```csharp
g.DefineGoods(paramdefs, new ItemDef
{
    Id          = 8100,
    DonorId     = 384,          // clone from this existing goods row
    SpEffectId  = 9100,         // links SpEffect; sets goodsType=consumable automatically
    Name        = "Goofy Draught",
    Description = "Restores 400 HP.",
    LongDesc    = "A longer description shown in the inventory.",
    MaxCount    = 5,
});
```

**`ItemDef` fields:**

| Field | Default | Notes |
|---|---|---|
| `Id` | required | Unique `EquipParamGoods` row id |
| `DonorId` | `384` | Row to clone (copies all param fields as a base) |
| `SpEffectId` | `-1` | When set: wires `goodsType=0` (consumable) + `refId_default` automatically |
| `Name` | `"Unnamed Item"` | Shown in inventory |
| `Description` | `""` | Short description |
| `LongDesc` | `""` | Long description |
| `MaxCount` | `1` | Stack size |
| `Configure` | `null` | Raw `Action<PARAM.Row>` for anything not listed above |

**Key item (no use effect)** — set `goodsType=4` via `Configure`:

```csharp
g.DefineGoods(paramdefs, new ItemDef
{
    Id        = 8101,
    Name      = "Stone Trinket",
    MaxCount  = 1,
    Configure = row => row["goodsType"].Value = (byte)4,
});
```

---

### DefineLot

Creates an `ItemLotParam` row. Used by `AwardItemLot` in EMEVD events and by
`PlaceTreasure` in MSB Treasure events.

```csharp
// Once-only lot (won't drop again after flag is set)
g.DefineLot(paramdefs, new LotDef
{
    LotId        = 8601,
    ItemId       = 8101,               // EquipParamGoods id
    Category     = LotCategory.Goods,  // Goods / Weapon / Protector / Accessory
    Count        = 1,
    OnceOnlyFlag = 11819402,           // event flag id — set when obtained
});

// Infinite lot
g.DefineLot(paramdefs, new LotDef
{
    LotId        = 8600,
    ItemId       = 8100,
    Count        = 3,
    OnceOnlyFlag = -1,                 // -1 = no restriction, infinite
});
```

---

### EditMsb / PlaceTreasure

Edits a map's `.msb` file in place. `PlaceTreasure` adds a glowing `o0500`
ground-pickup object and a `Treasure` event pointing to a lot.

```csharp
g.EditMsb("m18_01_00_00", msb => msb
    .PlaceTreasure(lotId: 8601, position: new Vector3(52f, -2f, 103f)));
```

**`PlaceTreasure` parameters:**

| Parameter | Default | Notes |
|---|---|---|
| `lotId` | required | `ItemLotParam` row to link |
| `position` | required | World XYZ position |
| `collisionName` | nearest existing pickup | Override the collision mesh |
| `inChest` | `false` | `true` for a chest container |
| `entityId` | `-1` | Assign an entity id for EMEVD reference |

---

### DefineItemTrigger

Writes a `Restart` EMEVD event that bridges item use (SpEffect activation) to
an event flag pulse. This is what connects an item use to both in-game EMEVD
responses and the C# `ItemUsed` hook.

```csharp
g.DefineItemTrigger("m18_01_00_00", spEffectId: 9100, triggerFlagId: 11819401);
```

The emitted event:
1. Waits until entity `10000` (the player) has SpEffect `9100` active
2. Sets flag `11819401` **ON**
3. Waits until the SpEffect expires
4. Sets flag `11819401` **OFF** → restarts — ready for next use

`eventId` defaults to `triggerFlagId`. Pass it explicitly to override:

```csharp
g.DefineItemTrigger(Map, spEffectId: 9100, triggerFlagId: 11819401, eventId: 11819450);
```

---

### WhenCharacterHasSpEffect / WhenCharacterLosesSpEffect

Two new `EventBuilder` methods for writing the SpEffect condition manually,
if you need it in your own events rather than via `DefineItemTrigger`.

```csharp
emevd.DefineEvent(11819450, EMEVD.Event.RestBehaviorType.Restart, ev => ev
    .WhenCharacterHasSpEffect(10000, 9100)  // wait until player has SpEffect active
    .SetFlag(11819401, FlagState.On)
    .WhenCharacterLosesSpEffect(10000, 9100) // wait until it expires
    .SetFlag(11819401, FlagState.Off)
    .Restart());
```

These map to EMEVD instruction `4:5 IF Character Has SpEffect`.

---

## New runtime APIs (DS1Mod.Core)

### hooks.RegisterItemUsed + hooks.ItemUsed

Register an item to watch and subscribe to the C# event. Fires in the 500ms
poll loop when the trigger flag pulses ON.

```csharp
// In OnLoad():
ctx.Hooks.RegisterItemUsed(goodsId: 8100, triggerFlagId: 11819401);
ctx.Hooks.ItemUsed += OnItemUsed;

private void OnItemUsed(int goodsId)
{
    if (goodsId != 8100) return;
    Console.WriteLine("player used the draught!");

    var stats = ctx.Reader.GetPlayerStats();
    Console.WriteLine($"HP after use: {stats?.CurrentHp}/{stats?.MaxHp}");
}
```

`triggerFlagId` must match the flag written by `DefineItemTrigger` (or your
own manual EMEVD event).

---

## In-game engine response vs. C# response

Both reactions can coexist. Pick the right tool:

| | In-game EMEVD event | C# `hooks.ItemUsed` |
|---|---|---|
| Runs inside | Game engine | .NET / DS1Mod poll loop |
| Can trigger | Animations, messages, flags, item awards | Anything in C# |
| Timing | Next game frame after SpEffect activates | Within ~500ms |
| Setup | `DefineItemTrigger` + `DefineEvent` | `RegisterItemUsed` |

```csharp
// Patch(): both together
g.DefineItemTrigger(Map, spEffectId: 9100, triggerFlagId: 11819401);

g.EditEmevd(Map, emevd =>
    emevd.DefineEvent(11819404, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(11819401, FlagState.On)
        .DisplayMessage(6900750)           // in-game text popup
        .SetFlag(11819405, FlagState.On)   // permanent "used" flag
        .WhenFlag(11819401, FlagState.Off)
        .Restart()));

// OnLoad(): C# side
ctx.Hooks.RegisterItemUsed(8100, 11819401);
ctx.Hooks.ItemUsed += id => Console.WriteLine($"used goods id {id}");
```

---

## Full example — item from scratch to use

```csharp
public void Patch(IPatchContext ctx)
{
    byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");
    var g = new GamePatch(ctx);

    // 1. Define what happens on use
    g.DefineSpEffect(paramdefs, new SpEffectDef { Id=9100, HpRecoverPoint=400 });

    // 2. Create the item (consumable, linked to the SpEffect)
    g.DefineGoods(paramdefs, new ItemDef
    {
        Id=8100, SpEffectId=9100, Name="Goofy Draught", MaxCount=5,
    });

    // 3. Create the lot
    g.DefineLot(paramdefs, new LotDef { LotId=8600, ItemId=8100, Count=3 });

    // 4. Place it on the floor
    g.EditMsb("m18_01_00_00", msb => msb
        .PlaceTreasure(8600, new Vector3(52f, -2f, 103f)));

    // 5. Also award it via EMEVD on boss death
    g.EditEmevd("m18_01_00_00", emevd => {
        emevd.DefineEvent(11819400, EMEVD.Event.RestBehaviorType.Default, ev => ev
            .WhenFlag(16, FlagState.On)
            .AwardItemLot(8600)
            .End());

        // 6a. EMEVD bridge (SpEffect → flag) — required for both response layers
        // 6b. In-game response: show message when item is used
        emevd.DefineEvent(11819404, EMEVD.Event.RestBehaviorType.Restart, ev => ev
            .WhenFlag(11819401, FlagState.On)
            .DisplayMessage(6900750)
            .WhenFlag(11819401, FlagState.Off)
            .Restart());
    });

    g.DefineItemTrigger("m18_01_00_00", spEffectId:9100, triggerFlagId:11819401);
}

public override void OnLoad(IModContext ctx)
{
    // 6c. C# runtime response
    ctx.Hooks.RegisterItemUsed(8100, 11819401);
    ctx.Hooks.ItemUsed += id => Console.WriteLine($"used {id}");
}
```

---

## ID ranges used by the demo mod

| Range | Purpose |
|---|---|
| `8100–8102` | `EquipParamGoods` new rows |
| `9100` | `SpEffectParam` new row |
| `8600–8602` | `ItemLotParam` new rows |
| `11819401–11819405` | Event flags (m18_01, section 9) |
| `6900750` | `Event_text` FMG entry (on-use message) |
