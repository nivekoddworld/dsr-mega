# decompiled_lua

Decompiled Lua AI sources for every map in Dark Souls Remastered, produced
by [DSLuaDecompiler](https://github.com/katalash/DSLuaDecompiler) from the
bytecode archives in [`../DSR_Lua_Scripts_Folder/script/`](../DSR_Lua_Scripts_Folder/script/).

## Layout

One subdirectory per map (`m10_00`, `m10_01`, …). Each contains `.lua` files
named after the AI entity they control (e.g. `223200_battle.lua` = Asylum
Demon, entity 223200).

## Usage

These are **read-only reference**. DSLuaDecompiler only goes one direction
(bytecode → source). To modify an AI:

1. Edit the decompiled source here as a starting point.
2. Compile to Lua 5.0 bytecode — see [`../../tools/ds1_ai_mods/`](../../tools/ds1_ai_mods/) for
   the Linux toolchain or use MeowScript on Windows.
3. Repack into the appropriate `*.luabnd.dcx` archive and drop it in the
   game's `script/` folder.

## Key files

| File | Entity | Notes |
|---|---|---|
| `m18_01/223200_battle.lua` | Asylum Demon (undead asylum) | Modified by `DS1Mod.AsylumSlam` and `DS1Mod.GoofyDemon` |
| `m18_01/223200_logic.lua` | Asylum Demon logic | Companion logic script |
| `m10_01/210000_battle.lua` | Bell Gargoyle | Boss fight logic |
| `common/aiCommon.lua` | Shared AI utilities | Included by most battle scripts |
