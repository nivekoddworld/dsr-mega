# Writing a game-data patcher mod

A **patcher mod** modifies DSR's game files at the title screen before any map
loads. This guide walks through building one with `DS1Mod.Modding` — the helper
library that handles the SoulsFormats boilerplate for you.

If you want to understand the mechanics at the SoulsFormats level first, read
[adding-items.md](adding-items.md) and [emevd-events.md](emevd-events.md). This
guide focuses on the *how-to-ship-a-mod* path.

The full worked example is `DS1Mod.GoofyDemon` — it patches Lua AI, EMEVD,
FMG text, and PARAM rows and is ~50 lines of intent-level code.

---

## 1. Project setup

Create a class library targeting `net8.0` (not `net8.0-windows` — patchers run at
load time and don't need WPF):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>MyMod</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\framework\DS1Mod.SDK\DS1Mod.SDK.csproj" />
    <ProjectReference Include="..\..\framework\DS1Mod.Modding\DS1Mod.Modding.csproj" />
  </ItemGroup>
</Project>
```

If you also need in-game events (hooks, tick, overlay) add `DS1Mod.Core` and
implement `IGameMod` alongside `IGamePatcher`.

---

## 2. The mod entry point

```csharp
using DS1Mod.SDK;
using DS1Mod.Modding;

public class MyMod : ModBase, IGamePatcher
{
    public override string Name    => "My Mod";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx.GameDir, ctx.BackupFile, Console.WriteLine);

        // all edits go here
        PatchText(g);
        PatchParams(g);
        PatchEmevd(g);
        PatchLua(g);
    }
}
```

`IGamePatcher.Patch()` is called **once, at the title screen**, before any map
loads. After `Patch()` returns the game continues loading normally.

`ctx.BackupFile(path)` writes `<path>.bak` the first time it's called for a
given path and is a no-op afterwards — it preserves the vanilla file.

---

## 3. Editing text (FMG)

On-screen text and item names live in FMG files inside per-language message
bundles. `Texts.Set` handles both DSR language copies (the game ships two
overlapping `msgbnd.dcx` trees):

```csharp
void PatchText(GamePatch g)
{
    // Event_text FMG — used by Display Message / Display Status Message EMEVD instructions
    g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        Texts.Set(bnd, Texts.EventText, 6900690, "*farts*"));

    // Item name / description FMGs
    g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd => {
        Texts.Set(bnd, Texts.GoodsName,        8000, "Demon's Dignity (lost)");
        Texts.Set(bnd, Texts.GoodsDescription, 8000, "All that remains of a demon's self-respect.");
        Texts.Set(bnd, Texts.GoodsLongDesc,    8000, "...");
    });
}
```

`EditBnd3Glob(folder, filename, action)` iterates every file in the game dir
matching that folder/filename pair and applies your action to each. That covers
all language variants without you having to enumerate them.

Common `Texts.*` constants: `EventText`, `GoodsName`, `GoodsDescription`,
`GoodsLongDesc`, `WeaponName`, `ArmorName`, `AccessoryName`, `MagicName`.

---

## 4. Editing params

Params are DSR's data tables — items, enemies, item lots, shops. You need
**paramdefs** (the layout schema) to read field names; the game doesn't ship them,
so **embed the paramdefbnd** in your mod as a resource:

```xml
<ItemGroup>
  <EmbeddedResource Include="paramdef.paramdefbnd.dcx">
    <LogicalName>paramdef.paramdefbnd.dcx</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

Get `paramdef.paramdefbnd.dcx` from the Paramdef Bank
([soulsmods/paramdex](https://github.com/soulsmods/paramdex)). `DS1Mod.GoofyDemon`
has a copy you can reuse — it's in `mods/DS1Mod.GoofyDemon/paramdef.paramdefbnd.dcx`.

```csharp
void PatchParams(GamePatch g)
{
    byte[] paramdefs = GetType().Assembly
        .GetManifestResourceStream("paramdef.paramdefbnd.dcx")!
        .ToArray();                                 // load from embedded resource

    g.EditParams(paramdefs, repo => {

        // Add a key-item row (clone donor 384 — a vanilla key item)
        repo.Edit("EquipParamGoods", p =>
            ParamRepository.AddClone(p, donorId: 384, newId: 8000, "Demon's Dignity (lost)",
                r => r["maxNum"].Value = (ushort)1));

        // Add an item-lot row (what the player actually receives)
        repo.Edit("ItemLotParam", p =>
            ParamRepository.AddClone(p, donorId: 1000, newId: 8500, "Demon's Dignity drop", r => {
                r["lotItemId01"].Value       = 8000;
                r["lotItemCategory01"].Value = LotCategory.Goods;  // 0x40000000
                r["lotItemNum01"].Value      = (byte)1;
                r["lotItemBasePoint01"].Value = 100;                // 100% weight
                r["getItemFlagId"].Value     = 50009000;            // once-only obtained flag
            }));
    });
}
```

`AddClone` copies every cell from the donor row, adds the new row idempotently
(removes any existing row with the same ID first), and calls your mutator for
the fields you want to change. All other fields get valid vanilla values.

---

## 5. Editing EMEVD (event scripts)

```csharp
void PatchEmevd(GamePatch g)
{
    g.EditEmevd("m18_01_00_00", e => {

        // Inject into an existing event — idempotent via the alreadyPresent guard
        e.InsertAfter(
            eventId:       11810310,
            after:         Instr.IsForceAnimation(1810800, 9060),
            toInsert:      Instr.DisplayMessage(6900690),
            alreadyPresent: Instr.IsDisplayMessage(6900690));

        // Brand-new looping event (registered at the TOP of the constructor)
        e.DefineEvent(11819000, EMEVD.Event.RestBehaviorType.Restart,
            Instr.IfEventFlag(true,  11815700),
            Instr.DisplayMessage(6900700),
            Instr.IfEventFlag(false, 11815700));

        // Run-once event: award item on boss death
        e.DefineEvent(11819100, EMEVD.Event.RestBehaviorType.Default,
            Instr.IfEventFlag(true, 16),           // 16 = Asylum Demon killed
            Instr.AwardItemLot(8500));
    });
}
```

`DefineEvent` adds the event to the EMEVD **and** registers it at the top of
the map constructor (event 0) — the GOTCHA that cost a full debug cycle is baked
in for you. See [emevd-events.md](emevd-events.md#adding-a-brand-new-event) for
the full explanation.

Flag IDs for `IfEventFlag` / `SetEventFlag`: use an **allocated section** of the
map. For m18_01 that means section 0 (`11810xxx`) or section 5 (`11815xxx`).
Section 7 and others are not allocated and writes silently vanish.

---

## 6. Editing Lua AI

```csharp
void PatchLua(GamePatch g)
{
    byte[] bytecode = GetType().Assembly
        .GetManifestResourceStream("223200_battle.luac")!
        .ToArray();

    g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>
        bnd.SetFileContaining("223200_battle", bytecode));
}
```

The Lua source + Linux compile toolchain live in
[`../tools/ds1_ai_mods/`](../tools/ds1_ai_mods/). See
[lua-ai-scripts.md](lua-ai-scripts.md) for the full compile workflow.

---

## 7. Idempotency — why it matters

`Patch()` runs **every time the game launches** (title screen). If it's not
idempotent, you'll get duplicate FMG entries, doubled EMEVD events, or corrupted
PARAM rows after the second launch. `DS1Mod.Modding` handles this for you:

- `Texts.Set` — does `RemoveAll(e => e.ID == id)` before adding.
- `ParamRepository.AddClone` — removes any existing row with the same ID first.
- `EmevdEditor.InsertAfter` — takes an `alreadyPresent` predicate; skips insert
  if a matching instruction is already there.
- `EmevdEditor.DefineEvent` — skips both the event and the registration if an
  event with that ID already exists.

If you write raw SoulsFormats, follow the same pattern manually.

---

## 8. Verifying without playtesting

You can't boot DSR headlessly to test. Verify by round-tripping:

```csharp
// After writing, re-read and assert
var bnd2 = BND3.Read(DCX.Decompress(path));
var fmg  = FMG.Read(bnd2.Files.First(f => f.Name.Contains("Item_name")).Bytes);
Debug.Assert(fmg.Entries.Any(e => e.ID == 8000 && e.Text == "Demon's Dignity (lost)"));
```

`DS1Mod.GoofyDemon` has a `#if DEBUG` verification pass after every edit that
does exactly this. If the file fails to parse, the game fails to boot — keep
`.bak` and it's safe to iterate.

---

## 9. Shipping

Build the class library and drop the DLL (and any native dependencies) into
`<game dir>/mods/`. The randomizer's **MODS** tab → **Install Mod…** handles
this automatically.

If your mod depends on `DS1Mod.Modding.dll` or `SoulsFormats.dll` beyond what
ships with the framework, include them alongside your DLL in `mods/` (the
`AssemblyLoadContext` resolves from the same directory).
