# Editing enemy AI (Lua)

Enemy and boss behavior in DSR is **Lua 5.0**. This is the one system where the
"compile it back" half is non-obvious — but it works **entirely on Linux**, no
MeowScript and no Windows.

## Where it lives

- `script/m??_??_??_??.luabnd.dcx` — per-map archives of AI scripts.
- `script/aiCommon.luabnd.dcx` — the shared library: `goal_list` (defines every
  `GOAL_COMMON_*` and `GOAL_<Name>` id), plus helper functions every script calls.
- Inside each archive: `<id>_battle.lua` (combat), `<id>_logic.lua` (perception),
  plus `.luagnl` (global-name table) and `.luainfo` (goal metadata).

Decompiled copies of all of them are in [`../decompiled_lua/`](../decompiled_lua/).

## The bytecode

DSR ships **stock Lua 5.0.2** bytecode. Header of every script:

```
1B 4C 75 61 50 01 04 08 04 06 08
 \_"Lua"_/ 50  LE int sizt instr OP=6 num=8(double)
            ^version 5.0
```

x64, little-endian, 8-byte `double` numbers, 6-bit opcodes. Globals the scripts
call (`REGISTER_GOAL`, `GOAL_COMMON_*`, `ai:...`) are resolved at runtime by the
game — they're just string constants in the bytecode.

## Reading: decompile

[DSLuaDecompiler](https://github.com/katalash/DSLuaDecompiler) (the `Lua50`
frontend). Retarget its `.csproj`s from `net9.0` to `net8.0` and it builds/runs
on Linux. Local-variable names and constants survive (Lua 5.0 keeps debug info),
so output is genuinely readable.

## Writing: compile Lua 5.0 on Linux

The trick: DSR bytecode is just stock Lua 5.0.2, and the **only** mismatch on
Linux is the `Instruction` type — Lua typedefs it `unsigned long` (8 bytes on
Linux LP64, but **4** on DSR's Windows LLP64). Force it to 4 bytes:

```sh
# (see ds1_ai_mods/build/build_luac.sh for the full script)
curl -sSL https://www.lua.org/ftp/lua-5.0.2.tar.gz | tar xz
cd lua-5.0.2
sed -i 's/typedef unsigned long Instruction;/typedef unsigned int Instruction;/' src/llimits.h
gcc -O2 -I include -I src -I src/lib -c src/*.c src/lib/*.c   # (exclude src/lua.c)
gcc -O2 -DLUA_OPNAMES -I include -I src -c src/lopcodes.c -o lopcodes.o  # luac/print.c needs luaP_opnames
gcc -O2 -I include -I src -c src/luac/luac.c src/luac/print.c
gcc -O2 -o luac *.o -lm
./luac -o out.luac in.lua
```

The compiled header comes out **byte-identical** to the game's. **Always
round-trip verify:** decompile your `.luac` and check the logic matches.

## Repacking into the luabnd

Standard SoulsFormats:

```csharp
byte[] dec = DCX.Decompress(path, out DCX.Type dcx);
BND3 bnd = BND3.Read(dec);
foreach (var f in bnd.Files)
    if (Path.GetFileName(f.Name.Replace('\\','/')) == "223200_battle.lua")
        f.Bytes = File.ReadAllBytes("out.luac");
File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
```

For a like-for-like function swap you do **not** need to touch `.luagnl` /
`.luainfo` — the existing tables stay valid.

## How an AI script is structured

```lua
REGISTER_GOAL(GOAL_MiniGreaterDemon223200_Battle, "MiniGreaterDemon223200Battle")
REGISTER_GOAL_NO_UPDATE(GOAL_MiniGreaterDemon223200_Battle, 1)

function MiniGreaterDemon223200Battle_Activate(ai, goal) ... end   -- pick actions
function MiniGreaterDemon223200Battle_Update(ai, goal) return GOAL_RESULT_Continue end
function MiniGreaterDemon223200Battle_Terminate(ai, goal) end
function MiniGreaterDemon223200Battle_Interupt(ai, goal) return false end  -- react to hits
```

- `REGISTER_GOAL` **must** be present — it binds the goal id (predefined in
  `aiCommon/goal_list`) to these functions, which the engine calls by name.
  (MeowScript injects this from a `--@battle_goal` header; if you compile/repack
  yourself, keep it in the source.)
- `Activate` adds **sub-goals** (`goal:AddSubGoal(GOAL_COMMON_Attack, ...)`) that
  run in order. When they finish, the goal **re-activates** — so `Activate` is a
  loop. To make a behavior last longer, end it with a `GOAL_COMMON_Wait` so a
  fast action can't instantly re-roll.
- `ai:` methods read state: `GetDist`, `GetHpRate`, `GetRandam_Int`,
  `IsTargetGuard`, `SetEventFlag` (see below), etc.

**Guarantee valid animations:** copy each `AddSubGoal` call's *exact* signature
from a vanilla script for the same model — the attack/anim ids are model-specific
(a move id that model lacks = T-pose or no-op). Reference:
[Souls Modding Wiki — Lua AI function repository](https://soulsmodding.com/doku.php?id=common-refmat:lua_ai_common_function_repository)
and [Grimrukh/SoulsAI](https://github.com/Grimrukh/SoulsAI).

## Talking to the event system

`ai:SetEventFlag(id, true/false)` sets a normal game event flag that **EMEVD can
read** (`IF Event Flag`) — that's how AI drives map events (e.g. on-screen text).
Mind the **flag section** rule in [emevd-events.md](emevd-events.md#event-flags):
the flag must be in a section the map actually allocates.

## Identifying enemies

Use the enemy randomizer's `airef.csv` as ground truth. Watch the easy mixups:
**Asylum Demon = entity `223200`** ("MiniGreaterDemon"); **Stray Demon = `223000`**
("GreaterDemon") — same model, different fight.

## The Windows alternative

[MeowScript](https://github.com/Meowmaritus/MeowScript) bundles a Lua 5.0
compiler (`luac50.exe`) and handles `luagnl`/`luainfo` + live-reload. Use it if
you're on Windows; the Linux `luac` above is the headless equivalent.
