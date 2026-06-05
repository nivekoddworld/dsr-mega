# ID Allocator System

A centralized system that prevents ID conflicts between mods by guaranteeing each mod receives unique, deterministic ID ranges.

## Problem

When multiple mods patch the same game files, they need unique IDs for:
- PARAM rows (goods, lots, speffects)
- Event flags (map-local game state)
- EMEVD events (event scripts)
- FMG entries (text strings)
- MSB entity IDs (map objects)
- And more...

**Without coordination, ID collisions happen:**
- Mod A uses goods ID 8000
- Mod B also uses goods ID 8000
- Second mod's write clobbers the first → corrupted game files

**Worse: Non-deterministic allocation breaks save games:**
- Run 1: Mod gets IDs [8000–8009] → player picks up item 8000
- Run 2: Mod gets IDs [8100–8109] → item now ID 8100
- Result: Save game corrupted (item ID mismatch)

## Solution

The **IdAllocator** system:
1. **Centrally allocates** ID ranges at patch time
2. **Persists allocations** to `allocations.json` in the game directory
3. **Guarantees determinism** — same mod always gets the same IDs
4. **Prevents conflicts** — impossible to collide because allocation is sequential

### How it Works

```csharp
public void Patch(IPatchContext ctx)
{
    // Request a contiguous block of 2 goods IDs
    int baseGoodsId = ctx.AllocateIds("EquipParamGoods", 2);
    
    // baseGoodsId is guaranteed unique across all mods
    // Use them like normal:
    g.DefineGoods(paramdefs, new ItemDef { Id = baseGoodsId, ... });
    g.DefineGoods(paramdefs, new ItemDef { Id = baseGoodsId + 1, ... });
}
```

**First run:**
- `allocations.json` doesn't exist
- Allocator creates it, assigns ranges, saves it
- Mod gets IDs [8000–8001]

**Second run:**
- `allocations.json` already exists
- Allocator loads it, sees mod already claimed [8000–8001]
- Mod gets the **same** IDs back
- Save games stay valid

### Persistent State

**allocations.json** (in game directory):
```json
{
  "allocations": {
    "EquipParamGoods": {
      "base": 8000,
      "claimed": {
        "DS1Mod.GoofyDemon": [8000, 8000],
        "DS1Mod.ItemsDemo": [8001, 8002]
      }
    },
    "EventFlags_m18_01": {
      "base": 11819000,
      "claimed": {
        "DS1Mod.GoofyDemon": [11819000, 11819009],
        "DS1Mod.ItemsDemo": [11819010, 11819024]
      }
    }
  }
}
```

## Supported ID Spaces

### PARAM Rows (Global)

| Space | Base | Type | Purpose |
|---|---|---|---|
| `EquipParamGoods` | 8000 | `int` | Consumable / key items |
| `ItemLotParam` | 8500 | `int` | Item drop lots |
| `SpEffectParam` | 9000 | `int` | Status effects, buffs |

Usage:
```csharp
int goodsId = ctx.AllocateId("EquipParamGoods");
int lotId = ctx.AllocateIds("ItemLotParam", 2);
int spEffect = ctx.AllocateId("SpEffectParam");
```

### Event Flags (Map-Local)

Format: `EventFlags_{mapId}` (e.g., `EventFlags_m18_01`)

| Space | Base | Purpose |
|---|---|---|
| `EventFlags_m18_01` | 11819000 | Asylum flags |
| `EventFlags_m10_01` | 11010900 | Undead Parish flags |
| `EventFlags_m14_01` | 11415000 | Demon Ruins flags |
| `EventFlags_m12_01` | 11215000 | Oolacile flags |

Usage:
```csharp
int flagBase = ctx.AllocateIds("EventFlags_m18_01", 5);
int flag1 = flagBase;
int flag2 = flagBase + 1;
```

### EMEVD Events (Map-Local)

Format: `EmevdEvents_{mapId}`

| Space | Base | Purpose |
|---|---|---|
| `EmevdEvents_m18_01` | 11819000 | Asylum events |
| `EmevdEvents_m10_01` | 11010900 | Undead Parish events |
| `EmevdEvents_m14_01` | 11415000 | Demon Ruins events |

Usage:
```csharp
long eventBase = ctx.AllocateIds("EmevdEvents_m18_01", 3);
g.EditEmevd("m18_01_00_00", emevd => emevd.DefineEvent(eventBase, ...));
g.EditEmevd("m18_01_00_00", emevd => emevd.DefineEvent(eventBase + 1, ...));
```

### FMG Entries (Global)

| Space | Base | Purpose |
|---|---|---|
| `EventText` | 6900000 | Event messages, HUD text |
| `ItemName` | 8000 | Item names (match goods ID) |
| `ItemDescription` | 8000 | Item descriptions |
| `ItemLongDesc` | 8000 | Long item descriptions |

Usage:
```csharp
int msgId = ctx.AllocateId("EventText");
int itemNameId = ctx.AllocateIds("ItemName", 2);
```

### MSB Entity IDs (Map-Local)

Format: `MsbEntities_{mapId}`

Usage:
```csharp
int entityId = ctx.AllocateId("MsbEntities_m18_01");
g.EditMsb("m18_01_00_00", msb => msb.PlaceTreasure(..., entityId: entityId));
```

### Item Obtained Flags (Global)

Dedicated range for "item was collected once" markers (distinct from local event flags).

| Space | Base | Purpose |
|---|---|---|
| `ItemObtainedFlags` | 50000000 | Once-only item drops |

Usage:
```csharp
int onceOnlyFlag = ctx.AllocateId("ItemObtainedFlags");
g.DefineLot(paramdefs, new LotDef { OnceOnlyFlag = onceOnlyFlag, ... });
```

## Migration Guide

### For New Mods

Use the allocator from the start:

```csharp
public void Patch(IPatchContext ctx)
{
    var g = new GamePatch(ctx);  // Use context-based constructor
    
    // Request all IDs you need
    int myGoodsId = ctx.AllocateId("EquipParamGoods");
    int myEventId = ctx.AllocateId("EmevdEvents_m18_01");
    
    // Use the allocated IDs
    g.DefineGoods(paramdefs, new ItemDef { Id = myGoodsId, ... });
}
```

### For Existing Mods (With Hardcoded IDs)

**Option 1: Switch to allocator**

If your mod currently has:
```csharp
private const int MyGoodsId = 8000;
private const int MyEventId = 11819000;
```

Convert to:
```csharp
private int MyGoodsId;
private int MyEventId;

public void Patch(IPatchContext ctx)
{
    MyGoodsId = ctx.AllocateId("EquipParamGoods");
    MyEventId = ctx.AllocateId("EmevdEvents_m18_01");
    // ... rest of patch code using MyGoodsId, MyEventId
}
```

First run: Your mod gets [8000] and [11819000] (you had those before).  
Second run: Allocator loads `allocations.json`, gives same IDs back.  
**Save games remain compatible.**

**Option 2: Keep hardcoded IDs**

If you don't want to refactor, you can let the allocator skip over your ranges by manually adding them to `allocations.json` **before** running:

```json
{
  "allocations": {
    "EquipParamGoods": {
      "base": 8000,
      "claimed": {
        "YourMod": [8000, 8009]
      }
    }
  }
}
```

The allocator will then assign other mods IDs starting at 8010.

## Determinism and Save Compatibility

**Critical:** The ID allocator must return the **same IDs every run** for the same mod.

This is why allocations persist to disk. Without persistence:
- Run 1: Mod A gets [8000–8099], Mod B gets [8100–8199]
- Run 2: Mod B loads first, gets [8000–8099], Mod A gets [8100–8199]
- Save games with items from Mod A break (ID shift)

With persistence:
- Run 1: Both mods load in order, allocations saved to file
- Run 2: File is loaded, mods get exact same IDs
- Save games remain valid

## Debugging

Check `allocations.json` in your game directory to see:
- Which mod owns which IDs
- What ranges are allocated in each space
- Whether there are conflicts (there shouldn't be!)

Example output:
```json
{
  "allocations": {
    "EquipParamGoods": {
      "base": 8000,
      "claimed": {
        "DS1Mod.GoofyDemon": [8000, 8000],
        "DS1Mod.ItemsDemo": [8001, 8002]
      }
    }
  }
}
```

You can also see allocation logs in the console when mods load:
```
[IdAllocator] Mod 'DS1Mod.GoofyDemon' allocated EquipParamGoods [8000–8000]
[IdAllocator] Mod 'DS1Mod.ItemsDemo' allocated EquipParamGoods [8001–8002]
```

## API Reference

### `IPatchContext` Methods

```csharp
/// <summary>Request a contiguous block of IDs.</summary>
int AllocateIds(string space, int count);

/// <summary>Request a single ID (shorthand for AllocateIds(space, 1)).</summary>
int AllocateId(string space);
```

### Implementation Details

- **Thread-safe**: Allocator uses locks during allocation
- **Idempotent**: Calling allocate multiple times for the same mod returns same IDs
- **Persistent**: All allocations saved to `allocations.json` immediately
- **No network**: Everything is file-based, works offline
- **No central authority**: Each game directory has its own `allocations.json`

## FAQ

**Q: What if I want to use a specific ID range?**  
A: You don't have a choice — the allocator picks. This is intentional. If every mod author picks manually, collisions happen. The allocator enforces coordination.

If you need **backwards compatibility** with existing saves that used a specific ID, migrate gradually:
1. First run: Let allocator assign new IDs
2. Release your mod
3. Users update, their saves use new IDs
4. Later runs: Always return same IDs via persistence

**Q: What if I only need IDs on certain maps?**  
A: Request them from the map-specific spaces:
```csharp
int flagA = ctx.AllocateId("EventFlags_m18_01");
int flagB = ctx.AllocateId("EventFlags_m10_01");  // Different space, different range
```

**Q: What if the allocations file gets corrupted?**  
A: Delete it — the allocator will rebuild it on next run. You'll lose backwards-compatibility with old saves for that session, but mods will load.

**Q: Can I reset allocations?**  
A: Delete `allocations.json` in your game directory before loading mods.

