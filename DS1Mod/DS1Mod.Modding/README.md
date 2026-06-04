# DS1Mod.Modding

A small helper library for writing DSR **game-data modification** mods on top of
[SoulsFormats](../../SoulsFormats). It captures the patterns (and the *gotchas*)
worked out building `DS1Mod.GoofyDemon`: DCX round-tripping, idempotent FMG /
PARAM / EMEVD edits, and the two traps that cost real debugging — registering
new events at the **top** of the constructor, and event-flag **sections**.

It's framework-agnostic (only depends on SoulsFormats), so it works inside a
DS1Mod `IGamePatcher` or any standalone tool.

## The shape of every edit

`GamePatch` wraps "resolve a path under the game dir → back it up → decompress →
hand you the parsed object → recompress and write back (same DCX type)":

```csharp
var g = new GamePatch(ctx.GameDir, ctx.BackupFile, Log);   // from a DS1Mod IPatchContext

g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>        // one archive
    bnd.SetFileContaining("223200_battle.lua", myLuaBytes));

g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>            // every language
    Texts.Set(bnd, Texts.EventText, 6900690, "*farts*"));

g.EditEmevd("m18_01_00_00", e => { /* events */ });        // a map's event script
g.EditParams(embeddedParamdefBytes, repo => { /* rows */ });// GameParam
```

## Text (FMG)

```csharp
Texts.Set(bnd, Texts.EventText, msgId, "hello");   // idempotent; both DSR copies
Texts.Set(bnd, Texts.GoodsName, 8000, "My Item");
string? s = Texts.Get(bnd, Texts.GoodsName, 8000);
```

## Params

`AddClone` copies a vanilla donor row (so every field is valid), adds it
idempotently, and lets you tweak fields:

```csharp
g.EditParams(paramdefs, repo => {
    repo.Edit("EquipParamGoods", p =>
        ParamRepository.AddClone(p, donorId: 384, newId: 8000, "My Item",
            r => r["maxNum"].Value = (ushort)1));
    repo.Edit("ItemLotParam", p =>
        ParamRepository.AddClone(p, 1000, 8500, "My drop", r => {
            r["lotItemId01"].Value   = 8000;
            r["lotItemCategory01"].Value = LotCategory.Goods;
            r["getItemFlagId"].Value = 50009000;   // -> once-only
        }));
});
```

The paramdefbnd (layout) isn't shipped by the game — **embed it** in your mod and
pass the bytes (`GamePatch.EditParams` calls `ParamRepository.LoadDefs`).

## Events (EMEVD)

`Instr` builds instructions by name; `EmevdEditor` does the high-level edits with
the gotchas baked in:

```csharp
g.EditEmevd("m18_01_00_00", e => {
    // inject into an existing event (idempotent via the alreadyPresent matcher)
    e.InsertAfter(11810310, Instr.IsForceAnimation(1810800, 9060),
        Instr.DisplayMessage(6900690), alreadyPresent: Instr.IsDisplayMessage(6900690));

    // define a brand-new looping event AND register it (at the constructor TOP)
    e.DefineEvent(11819000, EMEVD.Event.RestBehaviorType.Restart,
        Instr.IfEventFlag(true,  11815700),    // wait for flag on
        Instr.DisplayMessage(6900700),         // show text
        Instr.IfEventFlag(false, 11815700));   // wait for flag off, loop

    // a run-once award on boss death
    e.DefineEvent(11819100, EMEVD.Event.RestBehaviorType.Default,
        Instr.IfEventFlag(true, 16), Instr.AwardItemLot(8500));
});
```

`Instr` covers the common verbs: `InitializeEvent`, `IfEventFlag`, `SetEventFlag`,
`DisplayMessage` / `DisplayStatusMessage` / `DisplayBanner`, `AwardItemLot`,
`ForceAnimation`, `SpawnOneshotSfx`, `PlaySound`, `CameraVibration`, and `Raw` for
anything else.

## Flags — the section guard

```csharp
Flags.Section(11815700);                 // 5
Flags.IsSectionAllocated(evd, 11817700); // false on m18_01 -> writes would vanish
```

A map only allocates *some* flag sections; writing to an unallocated one silently
does nothing. Validate the flags you broadcast/watch against the map's EMEVD.

## What it replaced

In `DS1Mod.GoofyDemon`, the patch logic — an AI swap + HUD/fart FMG + an item
across params/FMG + 12 new EMEVD events — went from ~150 lines of hand-rolled
SoulsFormats boilerplate across 6 methods to ~50 lines of intent, with the
idempotency and the constructor/flag gotchas handled for you. Output is
byte-for-byte the same (verified by re-decompiling).
