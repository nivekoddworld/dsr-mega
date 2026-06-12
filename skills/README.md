---
name: ds1-framework
description: project-local skills for building Dark Souls Remastered mods using the DS1Mod framework
---

# DS1Mod Framework Skills — Index

Project-local skills for building DSR mods with the DS1Mod framework (in-process C# mods hot-loaded from `<game>/mods/`).

**Token economy: load only the skill(s) the task needs.** Each file is self-contained; don't read all of them up front.

| Skill | Read it when the task involves… |
|-------|--------------------------------|
| `ds1mod-sdk.SKILL.md` | Mod structure, lifecycle (`ModBase`), hooks (incl. `EnemyDamaged`), `ModConfig` settings, Reader/Writer, ImGui overlays, ID allocation |
| `ds1mod-modding.SKILL.md` | Patching game files: items (`DefineGoods`/`DefineRing`/`PlaceWorldPickup`), EMEVD events, AI Lua, MSB placement, ESD dialog/bonfire menus, raw PARAM edits |
| `ds1mod-memory.SKILL.md` | Live game-object access: RTTI vftable scans, heap scanning, player HP writes, native HUD bar control, inventory signature — and the crash-avoidance rules |
| `ds1mod-examples.SKILL.md` | Choosing an architecture / starting from a proven pattern (one section per example mod) |
| `speffectparam.SKILL.md` | SpEffectParam field reference — buffs, DoTs, `stateInfo` values, stacking |

## Routing Hints

- **New mod from scratch** → sdk (structure) + examples (pick a pattern); add modding only if it patches files.
- **"Add an item/ring/pickup"** → modding §3 only.
- **"React to game events at runtime"** → sdk §5 (hooks). Per-hit damage / HP manipulation → memory.
- **Anything touching `GameMemory`, vftables, heap scans, HUD values** → memory skill is mandatory; its safety rules prevent in-process crashes.
- **Status-effect numbers** → speffectparam (reference; search by field name, don't read whole).

## Ground Rules (apply to every mod)

1. Never hardcode IDs — `ctx.AllocateId(space)`; allocations persist across runs.
2. Patch phase = files on disk before maps load; runtime = 500 ms tick + hooks. Crashing the mod crashes DSR — try/catch anything risky (the pump swallows exceptions silently).
3. After changing framework DLLs, deploy to BOTH `<game>/` root and `<game>/mods/` — a stale root copy wins type resolution and breaks mods at load.
4. Print a build tag in `OnLoad` while iterating — stale-DLL deploys are the #1 wasted debugging cycle.

Key source dirs: `DS1Mod/framework/DS1Mod.{SDK,Core,Modding}/`, example mods in `DS1Mod/mods/`.
