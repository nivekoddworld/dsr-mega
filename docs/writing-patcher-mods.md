# Writing a Patcher Mod

A **patcher mod** modifies Dark Souls Remastered game files during the load phase (before the game boots). It implements `IGamePatcher` and uses `GamePatch` to modify PARAM, EMEVD, Lua, MSB, and other files.

## Quick Start

```csharp
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;

public class MyMod : ModBase, IGamePatcher
{
    public override string Name    => "My Cool Mod";
    public override string Version => "1.0.0";
    public override string Author  => "You";

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx);
        
        // Request IDs to prevent conflicts with other mods
        int myGoodsId = ctx.AllocateId("EquipParamGoods");
        int myFlagId  = ctx.AllocateId("EventFlags_m18_01");
        
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");
        
        // Patch PARAM
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id = myGoodsId,
            DonorId = 384,
            Name = "My Item",
            Description = "Cool stuff",
            LongDesc = "Even cooler explanation",
            MaxCount = 5
        });
        
        // Patch EMEVD
        g.EditEmevd("m18_01_00_00", emevd => emevd.DefineEvent(11819000,
            EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenFlag(myFlagId, FlagState.On)
                .DisplayMessage(100)
                .End()));
    }
}
```

## ID Allocation (Critical!)

**Always use the allocator to request IDs.** Never hardcode them.

```csharp
public void Patch(IPatchContext ctx)
{
    // Request IDs (guaranteed unique, persistent, deterministic)
    int goodsId  = ctx.AllocateId("EquipParamGoods");
    int lotId    = ctx.AllocateIds("ItemLotParam", 2);  // 2 consecutive
    int flagId   = ctx.AllocateIds("EventFlags_m18_01", 5);  // 5 flags
    int eventId  = ctx.AllocateId("EmevdEvents_m18_01");
    int entityId = ctx.AllocateId("MsbEntities_m18_01");
    
    // Use allocated IDs (never hardcoded)
    g.DefineGoods(paramdefs, new ItemDef { Id = goodsId, ... });
    g.DefineLot(paramdefs, new LotDef { LotId = lotId, ... });
    // etc.
}
```

**Why?** Two mods using the same ID → file corruption. The allocator prevents this by assigning unique, sequential ranges. See [id-allocator.md](id-allocator.md).

## File Patching

### PARAM (GameParam.parambnd.dcx)

Create new items, lots, and special effects:

```csharp
byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

g.DefineGoods(paramdefs, new ItemDef
{
    Id = myGoodsId,
    DonorId = 384,           // Clone from Estus Flask
    SpEffectId = 9000,       // Apply SpEffect when used
    Name = "Super Potion",
    Description = "Heals a ton",
    LongDesc = "A magical potion that heals a lot.",
    MaxCount = 99,
    Configure = row => row["weight"].Value = 0.5f
});

g.DefineLot(paramdefs, new LotDef
{
    LotId = myLotId,
    ItemId = myGoodsId,
    Category = LotCategory.Goods,
    Count = 1,
    OnceOnlyFlag = -1        // -1 = infinite; ≥0 = once-only flag
});

g.DefineSpEffect(paramdefs, new SpEffectDef
{
    Id = mySpEffectId,
    DonorId = 110,
    Duration = 10f,          // 10 seconds
    HpRecoverPoint = 200,    // Restore 200 HP
    MaxHpRate = 1.1f,        // 110% of max HP
    Configure = row => row["statueStateChangeDisable"].Value = 1
});
```

All edits are **idempotent** — re-running is safe.

### FMG (msg/*.msgbnd.dcx)

Set text strings in all locales automatically:

```csharp
int msgId = ctx.AllocateId("EventText");

g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
{
    Texts.Set(bnd, Texts.EventText, msgId, "On-screen popup");
});

g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
{
    Texts.Set(bnd, Texts.GoodsName, myGoodsId, "Super Potion");
    Texts.Set(bnd, Texts.GoodsDescription, myGoodsId, "Heals a ton");
    Texts.Set(bnd, Texts.GoodsLongDesc, myGoodsId, "A magical potion...");
});
```

English, Japanese, French, German, Spanish, Italian locales all handled automatically.

### EMEVD (event/*.emevd.dcx)

Define event scripts with the fluent `EventBuilder` API:

```csharp
int eventId = ctx.AllocateId("EmevdEvents_m18_01");

g.EditEmevd("m18_01_00_00", emevd => emevd.DefineEvent(eventId,
    EMEVD.Event.RestBehaviorType.Restart, ev => ev
        .WhenFlag(flagId, FlagState.On)
        .DisplayMessage(msgId)
        .SetFlag(anotherFlag, FlagState.Off)
        .Restart()));
```

**Conditions** (block until true):
- `WhenFlag(flagId, state)`
- `WhenDead(entityId)` / `WhenAlive(entityId)`
- `WhenHpBelow(entityId, ratio)`
- `WhenCharacterHasSpEffect(entityId, spEffectId)`
- `WhenCharacterLosesSpEffect(entityId, spEffectId)`
- `WhenInsideArea(entityId, areaId)` / `WhenOutsideArea(...)`
- `WhenActionButton(entityType, entityId, ...)`
- `WhenAllOf(and => ...)` / `WhenAnyOf(or => ...)`

**Actions** (execute):
- `SetFlag(flagId, state)`
- `AwardItemLot(lotId)`
- `DisplayMessage(msgId)`
- `DisplayStatusMessage(msgId)`
- `DisplayBanner(type)` — 1=Victory, 2=You Died
- `DisplayBossHealthBar(entityId, enabled)`
- `ForceAnimation(entityId, animId)`
- `SetCharacterEnabled(entityId, state)`
- `SetObjectEnabled(entityId, state)`
- `KillCharacter(entityId, awardSouls)`
- `SetCharacterAI(entityId, state)`
- `SetCharacterHome(entityId, regionId)`
- `SetCharacterImmortal(entityId, state)`
- `SetCharacterInvincible(entityId, state)`
- `WarpCharacter(entityId, destEntityId)`
- `HandleBossDefeat(entityId)`
- `End()` / `Restart()`
- `Raw(bank, id, args)` — for unmapped instructions

### MSB (map/MapStudio/*.msb)

Add objects, areas, and entities to maps:

```csharp
int entityId = ctx.AllocateId("MsbEntities_m18_01");

g.EditMsb("m18_01_00_00", msb => msb
    .PlaceTreasure(
        lotId: myLotId,
        position: new Vector3(100f, 200f, 300f),
        entityId: entityId));
```

MSB edits are additive — multiple mods add to the same map safely (if using different entity IDs).

### Lua AI (script/*.luabnd.dcx)

Inject compiled AI scripts:

```csharp
g.EditAi("m18_01_00_00", "223200", ai => ai
    .Goal("Battle", goal => goal
        .Act(70, q => q
            .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 10)
            .Attack(animId: 3008, cancelTime: 5))
        .Act(30, q => q
            .SpinStep(cancelTime: 8)
            .LeaveTarget(Target.Enemy0, Dist.Far, cancelTime: 10))
        .OnInterrupt(_ => true)),
    luaId: "MyAI");
```

Compiles to Lua 5.0, injects into the luabnd.

## Embedding Files

Store required files (paramdefs, Lua, etc.) in your mod assembly:

**.csproj:**
```xml
<ItemGroup>
  <EmbeddedResource Include="paramdef.paramdefbnd.dcx" />
  <EmbeddedResource Include="223200_battle.luac" />
</ItemGroup>
```

**Code:**
```csharp
private static byte[] GetEmbeddedResource(string name)
{
    var asm = typeof(MyMod).Assembly;
    using var stream = asm.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Resource not found: {name}");
    using var ms = new MemoryStream();
    stream.CopyTo(ms);
    return ms.ToArray();
}
```

## Best Practices

1. **Use context-based GamePatch constructor**
   ```csharp
   var g = new GamePatch(ctx);  // Wires conflict detection
   ```

2. **Allocate all IDs upfront in Patch()**
   ```csharp
   int id1 = ctx.AllocateId("EquipParamGoods");
   int id2 = ctx.AllocateIds("EventFlags_m18_01", 10);
   // ... use throughout
   ```

3. **Make patches idempotent** — safe to run multiple times

4. **Log important actions**
   ```csharp
   ctx.Log("Added super potion (goods ID 8000)");
   ctx.Log("Defined 10 event flags (11819000–11819009)");
   ```

5. **Use file backup** — `GamePatch` auto-creates `.bak` files

## Example: Complete Mod

```csharp
using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;

public class MyMod : ModBase, IGamePatcher
{
    public override string Name    => "Super Items";
    public override string Version => "1.0.0";
    public override string Author  => "You";

    private const string Map = "m18_01_00_00";
    private const int Player = 10000;

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx);
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

        // Allocate IDs
        int goodsId = ctx.AllocateId("EquipParamGoods");
        int lotId = ctx.AllocateId("ItemLotParam");
        int spEffectId = ctx.AllocateId("SpEffectParam");
        int msgId = ctx.AllocateId("EventText");
        int eventId = ctx.AllocateId("EmevdEvents_m18_01");
        int entityId = ctx.AllocateId("MsbEntities_m18_01");

        // SpEffect
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id = spEffectId,
            DonorId = 110,
            Duration = 0f,
            HpRecoverPoint = 500
        });

        // Goods & Lot
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id = goodsId,
            DonorId = 384,
            SpEffectId = spEffectId,
            Name = "Super Potion",
            Description = "Restores 500 HP",
            LongDesc = "A powerful potion that restores 500 HP.",
            MaxCount = 5
        });

        g.DefineLot(paramdefs, new LotDef
        {
            LotId = lotId,
            ItemId = goodsId,
            Category = LotCategory.Goods,
            Count = 1,
            OnceOnlyFlag = -1
        });

        // FMG
        g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.GoodsName, goodsId, "Super Potion");
            Texts.Set(bnd, Texts.GoodsDescription, goodsId, "Restores 500 HP");
            Texts.Set(bnd, Texts.GoodsLongDesc, goodsId, "A powerful potion that restores 500 HP.");
        });

        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.EventText, msgId, "Found Super Potion!");
        });

        // MSB
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(lotId: lotId, position: new Vector3(0, 100, 0), entityId: entityId));

        // EMEVD
        g.EditEmevd(Map, emevd => emevd.DefineEvent(eventId,
            EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenInsideArea(Player, 1812000)
                .DisplayMessage(msgId)
                .AwardItemLot(lotId)
                .End()));

        ctx.Log($"Patched! Goods={goodsId}, Lot={lotId}, Event={eventId}");
    }

    private static byte[] GetEmbeddedResource(string name)
    {
        var asm = typeof(MyMod).Assembly;
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Resource not found: {name}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
```

## Troubleshooting

| Problem | Solution |
|---|---|
| "Resource not found: paramdef..." | Add `<EmbeddedResource>` to .csproj, rebuild |
| "emevd not found: m18_01_00_00" | Map doesn't exist or game dir is wrong |
| CONFLICT warnings | Use allocator instead of hardcoding IDs |
| Save games break | IDs shifted — use allocator for deterministic allocation |
| Lua injection fails | Ensure compiled `.luac` matches game version |

See [id-allocator.md](id-allocator.md) for more on the allocation system.

