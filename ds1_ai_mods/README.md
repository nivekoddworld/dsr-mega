# DS1 AI Mods

Hand-written AI scripts for Dark Souls Remastered, built from the decompiled
sources in [`../decompiled_lua/`](../decompiled_lua/).

## Toolchain

These are **source** `.lua` files. To get them into the game you compile +
insert them with **[MeowScript](https://github.com/Meowmaritus/MeowScript)**,
which bundles the Lua 5.0 compiler (`luac50.exe`) and repacks the `luabnd`
archive (keeping the `LUAGNL`/`LUAINFO` tables consistent). The
[DSLuaDecompiler](https://github.com/katalash/DSLuaDecompiler) we used to read
the AI is decompile-only — it cannot package a script back into the game.

Flow: **DSLuaDecompiler** (read the AI) -> edit `.lua` -> **MeowScript** (compile + insert).

## Build steps (Windows)

1. Install MeowScript and point `MeowScript_Config.ini` at your UXM-extracted
   DSR folder with `IsDarkSoulsRemastered=1`.
2. Drag the `.lua` onto `MeowScript_Build.exe` (or `MeowScript_Build <file>`).
3. In-game, trigger any loading screen to hot-reload AI — no relaunch needed.

The `--@package` / `--@battle_goal` header comments tell MeowScript which
archive and goal to insert into; don't remove them.

## Scripts

| File | Target | Effect |
|---|---|---|
| `asylum_demon_slam.lua` | Asylum Demon (223200) | Only slam attacks: leap/body slam (3007) from range, butt slam (3008) up close. |
