# Items API — How To

Everything needed to add a new item to DSR: define it, give it a use effect,
place it in the world, and react to the player using it — in C#, no raw param
or Lua editing required.

---

## 1 — Define the use effect (SpEffectDef)

```csharp
g.DefineSpEffect(paramdefs, new SpEffectDef
{
    Id             = 9000,       // unique SpEffectParam row id (use 9000+ range)
    Duration       = 0f,         // 0 = instant; >0 = lingering buff in seconds
    HpRecoverPoint = 400,        // restore 400 HP flat on use
});
```

Common fields:

| Field | Effect |
|---|---|
| `HpRecoverPoint` | Flat HP restored instantly |
| `HpRecoverRate` | HP/second over `Duration` |
| `StaminaRecoverPoint` | Flat stamina restored |
| `MaxHpRate` | Max HP multiplier (1.2 = +20%) |
| `PhysAtkPowerRate` | Physical attack multiplier |
| `PhysDefRate` | Physical defense multiplier |
| `Duration` | How long the effect lasts (0 = instant) |
| `Configure` | Raw callback for any param field not listed above |

---

## 2 — Create the item (ItemDef + DefineGoods)

```csharp
g.DefineGoods(paramdefs, new ItemDef
{
    Id          = 8000,       // unique EquipParamGoods row id (use 8000+ range)
    DonorId     = 384,        // clone from this existing goods row
    SpEffectId  = 9000,       // links the SpEffect; also sets goodsType = consumable
    Name        = "My Potion",
    Description = "Restores HP.",
    LongDesc    = "A long description shown in the inventory.",
    MaxCount    = 5,
});
```

- If `SpEffectId` is set, `goodsType` and `refId_default` are wired automatically.
- Leave `SpEffectId` at `-1` (default) for key items that have no use effect — set
  `goodsType = 4` via `Configure` instead.

---

## 3 — Create a lot (LotDef + DefineLot)

A lot is what the game actually awards. An EMEVD event calls `AwardItemLot(lotId)`;
a Treasure event in the MSB also points to a lot.

```csharp
g.DefineLot(paramdefs, new LotDef
{
    LotId        = 8500,      // unique ItemLotParam row id
    ItemId       = 8000,      // EquipParamGoods id from step 2
    Category     = LotCategory.Goods,
    Count        = 1,
    OnceOnlyFlag = 50009000,  // event flag — item won't drop twice. -1 = infinite.
});
```

---

## 4 — Place it in the world (EditMsb / PlaceTreasure)

Adds a glowing `o0500` ground-pickup object and a Treasure event to the map.

```csharp
g.EditMsb("m18_01_00_00", msb => msb
    .PlaceTreasure(lotId: 8500, position: new Vector3(52f, -2f, 103f)));
```

Optional parameters:

| Parameter | Default | Notes |
|---|---|---|
| `collisionName` | nearest existing pickup's collision | Override if placing far from other pickups |
| `inChest` | `false` | `true` for chest-style container |
| `entityId` | `-1` | Assign an entity ID to reference from EMEVD |

---

## 5 — Award via EMEVD (no world placement)

If you'd rather give the item through a script trigger than place it on the floor:

```csharp
g.EditEmevd("m18_01_00_00", emevd =>
    emevd.DefineEvent(11819100, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenFlag(16, FlagState.On)   // Asylum Demon dead
        .AwardItemLot(8500)
        .End()));
```

---

## 6 — React when the item is used

Two layers — pick one or both.

### Layer A: In-game engine response (EMEVD)

`DefineItemTrigger` writes an EMEVD event that pulses an event flag every time
the item's SpEffect activates. A second event watches that flag and does something.

```csharp
// In Patch():
g.DefineItemTrigger("m18_01_00_00", spEffectId: 9000, triggerFlagId: 11819200);

g.EditEmevd("m18_01_00_00", emevd =>
    emevd.DefineEvent(11819201, EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(11819200, FlagState.On)
        .DisplayMessage(6900750)            // show text
        .SetFlag(11819202, FlagState.On)    // set a permanent flag
        .WhenFlag(11819200, FlagState.Off)
        .Restart()));
```

Everything above runs inside the game engine at runtime — no .NET involved.

### Layer B: C# callback (in-process)

```csharp
// In Patch() — same DefineItemTrigger call as above
g.DefineItemTrigger("m18_01_00_00", spEffectId: 9000, triggerFlagId: 11819200);

// In OnLoad():
ctx.Hooks.RegisterItemUsed(goodsId: 8000, triggerFlagId: 11819200);
ctx.Hooks.ItemUsed += id => {
    if (id == 8000)
        Console.WriteLine("player used the item!");
};
```

Fires in the 500ms poll loop inside the DSR process. Use it for logging, counters,
or anything that doesn't need frame-accurate timing.

---

## Full example

```csharp
public void Patch(IPatchContext ctx)
{
    byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");
    var g = new GamePatch(ctx);

    g.DefineSpEffect(paramdefs, new SpEffectDef { Id=9000, HpRecoverPoint=400 });

    g.DefineGoods(paramdefs, new ItemDef
    {
        Id=8000, SpEffectId=9000,
        Name="Goofy Draught", Description="Restores 400 HP.", MaxCount=5,
    });

    g.DefineLot(paramdefs, new LotDef
    {
        LotId=8500, ItemId=8000, OnceOnlyFlag=11819400,
    });

    // Place on the floor
    g.EditMsb("m18_01_00_00", msb => msb
        .PlaceTreasure(8500, new Vector3(52f, -2f, 103f)));

    // Also award on boss death
    g.EditEmevd("m18_01_00_00", emevd => {
        emevd.DefineEvent(11819400, EMEVD.Event.RestBehaviorType.Default, ev => ev
            .WhenFlag(16, FlagState.On)
            .AwardItemLot(8500)
            .End());

        // Bridge: SpEffect active → flag pulse (used by both EMEVD response and C# hook)
        g.DefineItemTrigger("m18_01_00_00", spEffectId:9000, triggerFlagId:11819401);

        // In-game response
        emevd.DefineEvent(11819402, EMEVD.Event.RestBehaviorType.Restart, ev => ev
            .WhenFlag(11819401, FlagState.On)
            .DisplayMessage(6900750)
            .WhenFlag(11819401, FlagState.Off)
            .Restart());
    });
}

public override void OnLoad(IModContext ctx)
{
    ctx.Hooks.RegisterItemUsed(8000, 11819401);
    ctx.Hooks.ItemUsed += id => Console.WriteLine($"Used item {id}");
}
```

---

## ID ranges used by this demo

| Range | Purpose |
|---|---|
| `8100–8101` | EquipParamGoods (new goods rows) |
| `9100` | SpEffectParam |
| `8600–8601` | ItemLotParam |
| `11819400–11819404` | Event flags (m18_01 section 9, allocated) |
| `6900750` | Event_text FMG (on-use message) |
