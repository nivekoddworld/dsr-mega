# DSR Modding Notes

Field notes from modding **Dark Souls Remastered** — everything worked out in
this repo, with a hard rule: **all of it runs on Linux** (no Windows GUI tools),
using [SoulsFormats](../SoulsFormats) for the file formats.

These docs are practical and grounded in things that actually shipped here (the
`DS1Mod.GoofyDemon` mod), including the gotchas that cost real debugging time.

## The guides

| Guide | What it covers |
|---|---|
| [lua-ai-scripts.md](lua-ai-scripts.md) | Enemy/boss **AI** — decompile, edit, **compile Lua 5.0 on Linux**, repack the `luabnd` |
| [emevd-events.md](emevd-events.md) | **Event scripts** (EMEVD / "DarkScript") — decompile, edit, **add brand-new events**, on-screen text |
| [adding-items.md](adding-items.md) | **New items** — params, item text, drops/awards |

## The helper library

[`DS1Mod.Modding`](../DS1Mod/DS1Mod.Modding/) wraps all the patterns below (DCX round-trip, idempotent FMG/PARAM/EMEVD edits, the constructor and flag-section gotchas) into a small API, so a new data mod is ~50 lines. The guides here explain *what's going on underneath*; the library is the *how*.

## The three scripting systems (don't mix them up)

DSR has several independent systems. We touched three:

| Folder | Format | Controls | Editor |
|---|---|---|---|
| `script/*.luabnd.dcx` | **Lua 5.0** | enemy AI / combat behavior | DSLuaDecompiler + a Linux `luac` |
| `event/*.emevd.dcx` | **EMEVD** (bytecode) | map logic: boss intros, triggers, item awards, flags | SoulsFormats + EMEDF |
| `param/GameParam/*.parambnd.dcx` | **PARAM** | data tables: items, enemies, lots, shops | SoulsFormats + PARAMDEF |

Text for all of them lives in **FMG** files inside `msg/<lang>/*.msgbnd.dcx`.

## The universal toolchain

Everything is one library: **SoulsFormats** reads/writes `DCX` (compression),
`BND3/BND4` (archives), `PARAM`/`PARAMDEF`, `FMG`, and `EMEVD`. So every edit is
the same shape:

```
DCX.Decompress(path, out type)  ->  BND3.Read(bytes)  ->  edit a file inside
   ->  BND3.Write()  ->  DCX.Compress(bytes, type)  ->  write back
```

Preserve the original DCX type (DSR is `DCX_DFLT_10000_24_9`) and BND format.

## Delivering edits: the DS1Mod patcher pattern

The mods here implement `IGamePatcher`. Its `Patch()` runs **at the title
screen, before any map loads**, with file access to the game dir. So a mod
edits the real game files in place at launch. The rules we follow:

1. **Back up first** — `ctx.BackupFile(path)` writes `<path>.bak` once (vanilla).
2. **Edit surgically**, never overwrite whole files — that keeps you compatible
   with other mods *and the randomizer itself*, which also edits these files.
3. **Be idempotent** — `RemoveAll(...)` then `Add(...)`, and guard insertions, so
   re-running (every launch) never duplicates anything or self-heals old state.
4. **You can't playtest headless** — so **verify by round-tripping**: re-read /
   re-decompile the file you just wrote and assert the change is there and the
   file still parses.

## Two gotchas that bit us (read these)

- **Event-flag *sections* must be allocated.** Map flags look like
  `1<area><section><number>` (e.g. `11_181_5_700`). A map only allocates certain
  sections — m18_01 (Undead Asylum) uses **section 0 (`11810xxx`)** and
  **section 5 (`11815xxx`)** only. Flags in an unallocated section (we tried
  `11817xxx`) go nowhere — `SetEventFlag` writes into the void and nothing reads
  back. See [emevd-events.md](emevd-events.md#event-flags).
- **The map constructor (event 0) is not a flat list.** It has multiplayer
  `SKIP`s and an `END IF` partway through, so a new-event registration appended
  at the *end* may never run. Register new events at the **top**. See
  [emevd-events.md](emevd-events.md#adding-a-brand-new-event).

## Where the worked examples live

- `ds1_ai_mods/` — the goofy AI source + the Linux `luac` build scripts
- `decompiled_lua/` — every AI script, decompiled
- `decompiled_emevd/` + `event_tools/` — every event script, decompiled, + the decompiler
- `DS1Mod/DS1Mod.GoofyDemon/` — one mod that does AI + EMEVD + FMG + PARAM edits
