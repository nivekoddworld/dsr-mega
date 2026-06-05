# gamedata

Extracted and decompiled game files from a UXM-unpacked Dark Souls Remastered
install. All folders here are **read-only reference** — the randomizer and mod
patchers read from (and write to) the live game directory, not from here.

| Folder | Contents |
|---|---|
| **DSR_Event_Folder/** | Raw EMEVD bytecode archives (`.emevd.dcx`), one per map |
| **DSR_Lua_Scripts_Folder/** | Raw Lua AI bytecode archives (`.luabnd.dcx`), one per map |
| **decompiled_emevd/** | Human-readable EMEVD decompilations; see `decompiled_emevd/README.md` |
| **decompiled_lua/** | Decompiled Lua AI sources; see `decompiled_lua/README.md` |

To rebuild the decompiled files, see [`../tools/event_tools/README.md`](../tools/event_tools/README.md)
(EMEVD) and [`../tools/ds1_ai_mods/README.md`](../tools/ds1_ai_mods/README.md) (Lua).
