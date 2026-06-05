# DSR_Lua_Scripts_Folder

Raw extracted Lua AI bytecode archives from a UXM-unpacked Dark Souls Remastered
install. One `.luabnd.dcx` per map, each containing the compiled Lua 5.0
bytecode for every AI entity in that map.

These files are **read-only reference** — the mod patchers (`DS1Mod.AsylumSlam`,
`DS1Mod.GoofyDemon`) read from the live game directory, not from here.

Human-readable decompilations live in [`../decompiled_lua/`](../decompiled_lua/).
The custom hand-written AI scripts and Linux build toolchain live in
[`../../tools/ds1_ai_mods/`](../../tools/ds1_ai_mods/).
