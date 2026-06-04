# DS1 AI Mods

Hand-written AI scripts for Dark Souls Remastered, built from the decompiled
sources in [`../decompiled_lua/`](../decompiled_lua/), plus a **fully Linux**
toolchain to compile and repack them into the game — no MeowScript, no Windows.

## Scripts

| File | Target | Effect |
|---|---|---|
| `asylum_demon_slam.lua` | Asylum Demon (223200) | Slam attacks only: leap/body slam (3007) from range, butt slam (3008) up close. |
| `goofy_demon.lua` | Asylum Demon (223200) | Goofy: 10 random moods (shimmy, breakdance, zoomies, hokey pokey, stage fright, victory lap, existential crisis, flee, rare attacks). Broadcasts current mood over event flags 15105610-19 for the mod's console readout. |

## How the toolchain works

The [DSLuaDecompiler](https://github.com/katalash/DSLuaDecompiler) only goes one
way (bytecode → source). To put a script *back* in the game you need a Lua 5.0
compiler that emits DSR-format bytecode, then you repack the `luabnd` archive.
MeowScript bundles a Windows compiler for this — but DSR's bytecode is just
stock **Lua 5.0.2**, x64 little-endian, 8-byte `double` numbers, 6-bit opcodes.
The only mismatch on Linux is `Instruction`, which Lua typedefs as
`unsigned long` (8 bytes on Linux LP64, but 4 on DSR's Windows LLP64). Force it
to `unsigned int` and a Linux-built `luac` emits a byte-identical header
(`1b 4c 75 61 50 01 04 08 04 06 08 ...`).

```
build/
  build_luac.sh   downloads Lua 5.0.2, patches Instruction, builds ./luac
  repack/         dotnet tool: swaps one .luac into a luabnd.dcx via SoulsFormats
  build.sh        end-to-end: asylum_demon_slam.lua -> dist/m18_01_00_00.luabnd.dcx
dist/
  m18_01_00_00.luabnd.dcx   prebuilt, ready to install
```

### Rebuild it yourself

```sh
# Needs the original archive (not in git — copy from your DSR install or
# DSR_Lua_Scripts_Folder/script/).
ds1_ai_mods/build/build.sh /path/to/original/m18_01_00_00.luabnd.dcx
```

## Install

1. **Back up** `<DSR>/script/m18_01_00_00.luabnd.dcx`.
2. Copy `dist/m18_01_00_00.luabnd.dcx` over it (UXM-extracted install).
3. Launch and enter the Northern Undead Asylum — the Asylum Demon will only slam.

To revert, restore your backup.

> Built and verified on Linux: the script compiles to Lua 5.0 bytecode,
> round-trips cleanly back through the decompiler, and the repacked archive
> preserves the original DCX/BND format (`DCX_DFLT_10000_24_9`, BND3).
> It has **not** been tested in a running game (no DSR/Windows in the build env) —
> if the boss fails to load, restore the backup and check the goal registration.

## Alternative: MeowScript

You can also build via [MeowScript](https://github.com/Meowmaritus/MeowScript):
drop the `--@package` / `--@battle_goal` header comments back on the script
(removing the explicit `REGISTER_GOAL` lines, which MeowScript injects) and drag
it onto `MeowScript_Build.exe`. MeowScript adds live-reload (trigger any loading
screen to apply without relaunching).
