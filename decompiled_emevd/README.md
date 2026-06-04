# Decompiled EMEVD (event scripts)

Human-readable decompilations of every DSR event script in
[`../DSR_Event_Folder/event/`](../DSR_Event_Folder/event/). One `.evd.txt` per
`.emevd.dcx`.

## What these are

EMEVD is DSR's **event scripting** system — a compiled bytecode (separate from
the enemy AI Lua in `../decompiled_lua/`). It drives map logic: boss intros, fog
gates, item pickups, triggers, cutscenes, flag bookkeeping. Each event is:

```
Event <id>  (rest=<RestBehavior>) {
    <Instruction Name>(Arg Name=value, ...)
    ...
}
```

Instruction names, argument names/types, and enum labels come from the **EMEDF**
(`../event_tools/ds1emedf.json`, the DS1 definition shipped with DarkScript3 /
soulsmods). Parameterized arguments — values injected by the event that
*initializes* this one — are shown as `X<sourceByte>_<byteCount>`.

## How they were produced

`../event_tools/emevd_decompile/` parses each archive with **SoulsFormats**
(`EMEVD.Read` + `Instruction.UnpackArgs`) and renders it using the EMEDF. To
regenerate:

```sh
dotnet run --project event_tools/emevd_decompile -- \
    event_tools/ds1emedf.json  DSR_Event_Folder/event  decompiled_emevd
```

## Pointers

- `m18_01_00_00.emevd` — Undead Asylum. The **Asylum Demon entrance** is event
  `11810310`: on area entry it sets a landing SpEffect (4160), force-plays the
  jump-down animation (`9060`), and spawns impact SFX (`1811991`). Boss
  health-bar/name is entity `1810800`, name ID `2232`.
- `common.emevd` — shared logic initialized by every map.

> Read-only reference. These `.txt` files are not recompiled; edits to events go
> through SoulsFormats EMEVD on the binary (see the mod patchers).
