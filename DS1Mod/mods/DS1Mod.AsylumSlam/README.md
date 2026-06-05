# DS1Mod.AsylumSlam

A DS1Mod-framework mod that locks the **Asylum Demon** (entity 223200) to slam
attacks only:

- **3007** — flying body slam, from range (he leaps and crashes down).
- **3008** — point-blank butt slam, up close (tracks you via `AttackTunableSpin`).

Far → he leaps to close the gap; once close → butt slam; roll away → he leaps
again.

## How it works

Implements `IGamePatcher`. At launch (title screen, before any map loads) its
`Patch()`:

1. Backs up `<game>/script/m18_01_00_00.luabnd.dcx` (`.bak`, first run only).
2. Opens the archive, replaces the `223200_battle.lua` entry with our slam-only
   AI, repacks (preserving the original DCX/BND format), and writes it back.

The replacement is **precompiled Lua 5.0 bytecode** embedded in the DLL
(`223200_battle.luac`), built with a DSR-compatible `luac` (see
`../../ds1_ai_mods/`). No Lua compiler or native dependency is needed at
runtime — only SoulsFormats, which ships alongside the mod.

It is idempotent: re-running `Patch()` re-applies the same swap and never
overwrites the vanilla backup.

## Install

Build (`dotnet build -c Release`) and copy both DLLs into `<game>/mods/`:

- `DS1Mod.AsylumSlam.dll`
- `SoulsFormats.dll`

`DS1Mod.Core.dll` / `DS1Mod.SDK.dll` are provided by the host — don't copy them.
Launch via **▾ → Launch with Mod Framework** (or the randomizer's **MODS** tab →
**Install Mod…**).

## Revert

Remove the DLLs from `mods/` and restore `m18_01_00_00.luabnd.dcx` from the
`.bak` the mod created.

## Status

Built and unit-exercised on Linux: `Patch()` backs up, swaps, and re-packs; the
patched entry round-trips back to the expected slam logic and the backup is
confirmed vanilla. **Not yet tested in a live game** (no DSR/Windows in the
build env).
