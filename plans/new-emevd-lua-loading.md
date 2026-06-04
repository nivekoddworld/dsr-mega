# Loading completely new EMEVD / Lua without piggybacking on vanilla files

Research question: is it possible to run custom EMEVD event scripts or Lua AI
scripts in DSR without modifying any vanilla game file?

**Status: partially possible today; full independence requires one of two hard
things (new map loading, or a new in-process hook).**

---

## How DSR's file loading works (the key constraint)

DSR uses a **named-file-request model** — when the engine loads a map it asks
for specific filenames derived from the map ID:

```
event/m18_01_00_00.emevd.dcx    ← per-map event script
event/common.emevd.dcx          ← loaded and kept active in every map
script/m18_01_00_00.luabnd.dcx  ← Lua AI scripts for that map
```

It does **not** enumerate the `event/` or `script/` folders looking for new
files. ModEngine2 hooks `CreateFileW` at the kernel level and can serve any
file from a mod folder — but only when the engine has already generated the
request. If the engine never asks for `event/m99_00_00_00.emevd.dcx`, nothing
can inject it.

Source: [ModEngine2 `archive_file_overrides.cpp`](https://github.com/soulsmods/ModEngine2/blob/main/src/modengine/ext/mod_loader/archive_file_overrides.cpp) — `find_override_file()` only returns a path when `fs::exists(modDir / gamePath)` is true; it cannot generate new engine requests.

---

## Approach 1 — `common.emevd` as a global event host

**Effort: low. Works today with DS1Mod.Modding.**

`common.emevd.dcx` is loaded and stays active in every map. New events added
there fire everywhere with no per-map file involvement.

```csharp
g.EditEmevd("common", e => {
    e.DefineEvent(19000000, EMEVD.Event.RestBehaviorType.Default,
        Instr.IfEventFlag(true, 50009000),
        Instr.DisplayMessage(99000));
});
```

`DefineEvent` registers at the **top** of Event 0 (the constructor top rule —
Event 0 contains multiplayer SKIPs, so appending is not safe). `DS1Mod.Modding`
handles this automatically.

**What you're still touching:** `common.emevd.dcx` is a vanilla file. This is
the minimum possible footprint — one shared file rather than per-map files.

**Gotchas:**
- Flag-section allocation still applies. Use section 0 (`11810xxx`) or section 5
  (`11815xxx`) for m18_01 events, etc. Unallocated sections silently do nothing.
- Event IDs must not collide with vanilla. Safe range: derive from an 8-digit
  vanilla ID + offset.

---

## Approach 2 — New Lua scripts in an existing luabnd

**Effort: low. Well-established workflow (MeowScript, DS1Mod.AsylumSlam).**

A `luabnd.dcx` is a BND3 archive of `.lua` files keyed by a numeric script ID
that matches an NPC model ID. You can **add** new file entries for NPC IDs not
currently in that archive. When the engine needs AI for an entity with that
model ID, it looks up the script and runs it — including entirely new goal
functions you wrote.

Workflow:
1. Place a new enemy in the map via MSB (model ID `XXXX`).
2. Write a Lua script file `XXXX_battle.lua` implementing your goals.
3. Inject it into the map's luabnd via `DS1Mod.Modding`:
   ```csharp
   g.EditBnd3("script/m18_01_00_00.luabnd.dcx", bnd =>
       bnd.AddOrReplace("XXXX_battle.lua", compiledBytecode));
   ```
4. The engine finds and executes it.

**Constraint — goal IDs:** Goal functions are registered by numeric ID. The
`goal_list.lua` file shows these are a fixed, closed set of constants (0–5,
1000–2254, 6000–6806, 120000–540000…). Whether the engine executes a function
registered under a previously unknown goal ID is untested. **Safe practice:**
reuse a vanilla goal ID range and override the body, rather than inventing new
IDs.

**What you're still touching:** The map's `luabnd.dcx`. Same footprint argument
as above — minimum is one shared archive.

---

## Approach 3 — New map ID with its own EMEVD and luabnd

**Effort: very high. Mostly an unsolved problem for DSR.**

When DSR loads a map with a new ID (e.g. `m99_00_00_00`), it requests:
- `event/m99_00_00_00.emevd.dcx`
- `script/m99_00_00_00.luabnd.dcx`

ModEngine2 can serve both from a mod folder. The EMEVD and luabnd content is
entirely custom — no vanilla file touched.

**The blocker:** getting the map loaded at all.

- Collision uses a proprietary Havok format. Tools that produce DS1 PTDE / DS2
  collision don't generate DSR-compatible output — as of 2026 this hasn't been
  fully solved.
- Area transitions require changes to vanilla EMEVD (to warp the player in) or
  a code hook — so you still touch something.
- At least one custom map was injected into vanilla DS1 (reported by PC Gamer,
  2021), but it required deep reverse engineering and is not a reproducible
  off-the-shelf process for DSR.

**When this makes sense:** if a new-map workflow ever becomes viable (e.g.
someone solves DSR collision), the EMEVD / Lua side is already solved. Both
files would just be placed in the mod folder.

---

## Approach 4 — In-process hook into DSR's EMEVD loader

**Effort: medium-high. Requires AOB scan + new hook. Within reach given DS1Mod.**

DS1Mod already hooks D3D11 `Present` via `d3d_hook.cpp` using the same
pattern-scan + function-pointer replacement technique used in `modloader.cpp`.
The same approach applied to DSR's internal event-file reading function could
intercept the moment the engine parses an EMEVD binary and inject additional
events — effectively making the engine see a larger EMEVD than was on disk.

This is the only path to **zero vanilla file modification** for event logic on
an existing map. Nothing on disk changes; synthetic EMEVD data is produced
in-memory at load time.

**What's needed:**
1. Locate DSR's EMEVD parsing function (AOB scan — the EMEVD magic `EVD\0` and
   its known binary layout are good starting points).
2. Hook it (same pattern as `DS1Mod.Injector/d3d_hook.cpp`).
3. After the vanilla parse completes, append new events to the in-memory
   `EMEVD*` struct and re-run Event 0 registration.

**Risks / unknowns:**
- DSR may validate or checksum EMEVD data after loading.
- Event 0 runs once during load — hooking after the fact may require replaying
  the initialization sequence.
- In-memory struct layout for the parsed EMEVD needs to be reverse-engineered
  (not currently documented publicly).

---

## Decision matrix

| Want | Best approach today |
|---|---|
| Custom global logic, any map | Approach 1 — add to `common.emevd` |
| Custom AI for a new or modified enemy | Approach 2 — add script to luabnd |
| Fully fresh event script, no vanilla touched | Approach 3 (new map) or Approach 4 (hook) |
| Prototype speed, minimum R&D | Approach 1 + 2 via `DS1Mod.Modding` |
| Long-term: truly standalone mod DLL | Approach 4 — in-process EMEVD hook |

---

## Implementation priority suggestion

1. **Now (no new work):** use `common.emevd` injection and per-map luabnd
   injection via `DS1Mod.Modding`. Covers the vast majority of use cases.

2. **Near-term if needed:** prototype the in-process EMEVD hook (Approach 4).
   The DS1Mod framework (`modloader.cpp` + AOB infrastructure) already has
   everything needed. The main unknown is the in-memory EMEVD struct layout.

3. **Long-term:** monitor the DSR custom-map scene. If someone solves the
   Havok collision problem, Approach 3 becomes a clean solution for completely
   standalone mods.

---

## References

- [ModEngine2 `archive_file_overrides.cpp`](https://github.com/soulsmods/ModEngine2/blob/main/src/modengine/ext/mod_loader/archive_file_overrides.cpp) — source confirming file-request-model (not folder scan)
- [MeowScript](https://github.com/Meowmaritus/MeowScript) — established luabnd script injection
- [SoulsAI `goal_list.lua`](https://github.com/Grimrukh/SoulsAI/blob/master/ai_scripts/aiCommon_Funcs/goal_list.lua) — fixed goal ID constants
- [DSEventScriptTools EstusQuest mod](https://github.com/HotPocketRemix/DSEventScriptTools/blob/master/Mods/EstusQuest/DCX%20Version/common.emevd.dcx) — real example of adding new events to common.emevd
- [EMEVD — Souls Modding Wiki](http://soulsmodding.wikidot.com/format:emevd)
- `DS1Mod/framework/DS1Mod.Injector/d3d_hook.cpp` — existing in-process hook as a template for Approach 4
- `DS1Mod/framework/DS1Mod.Modding/` — the helper library that makes Approaches 1 and 2 easy
