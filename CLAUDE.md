# DS1 Mega Randomizer — Developer Context

## Repository

- Repo: `nivekoddworld/dsr-mega`
- Primary branch: `main`

## Project Overview

Two parallel systems live in this repo:

**DS1 Mega Randomizer** — WPF app (.NET 8, Windows only) that randomizes Dark Souls Remastered. Four independent systems:

- **Fog Gate** — shuffles where fog doors lead
- **Item** — randomizes pickups, shops, starting gear
- **Enemy** — randomizes regular enemies, minibosses, and all boss slots
- **Mimic** — swaps mimic positions

Entry point: `DS1MegaRando.UI` → `MegaRandomizer.cs` orchestrates everything.

**DS1Mod Framework** — a `dinput8.dll` sideloader that runs .NET 8 code inside the DSR process. Mods implement `IGameMod` and are hot-loaded from `<game>/mods/` at startup. The randomizer UI has a MODS tab for installing/removing mods.

## Key Files — Randomizer

| File | Role |
|---|---|
| `DS1MegaRando.Data/Enemies/BossIds.cs` | All 32 boss slot definitions (EntityID, ModelID, EMEVD patches) |
| `DS1MegaRando.Enemies/BossRandomizer.cs` | Boss assignment, NpcParam selection, InitAnimId |
| `DS1MegaRando.Enemies/BossEmevdPatcher.cs` | Strips model-specific EMEVD instructions from boss intro events |
| `DS1MegaRando.Core/BossSfxMerger.cs` | Merges enemy effect entries into CommonEffects bundle (VFX fix) |
| `DS1MegaRando.Core/GameFileWriter.cs` | Write pipeline — calls SFX merge after MSB write |
| `DS1MegaRando.IO/BackupManager.cs` | .bak backups of all modified game files |
| `boss_overrides.json` | User config: pinned replacements, blocked combos, spawn positions |
| `DS1MegaRando.UI/DS1MegaRando.UI.csproj` | Includes boss_overrides.json as Content/PreserveNewest |

## Key Files — DS1Mod Framework

| File | Role |
|---|---|
| `DS1Mod/DS1Mod.Injector/modloader.cpp` | C++ DLL entry; applies heap fix, bootstraps .NET runtime via hostfxr |
| `DS1Mod/DS1Mod.Host/ModLifecycleManager.cs` | Scans `mods/`, loads each DLL into its own `AssemblyLoadContext`, drives the tick loop |
| `DS1Mod/DS1Mod.Core/GameMemory.cs` | Direct in-process pointer reads/writes (no ReadProcessMemory) |
| `DS1Mod/DS1Mod.Core/GamePointers.cs` | AOB scan to resolve DSR version-specific base pointers |
| `DS1Mod/DS1Mod.Core/EventPump.cs` | 500 ms poll loop; fires `BossKilled`, `FogGateEntered`, `PlayerDied`, `PlayerLeveledUp` |
| `DS1Mod/DS1Mod.SDK/ModBase.cs` | Abstract base class for mods — implement `Name/Version/Author` and override hooks |

## Bundled Mods

| Mod | Purpose |
|---|---|
| `DS1Mod/DS1Mod.DemoMod` | SDK exercise — hits every surface: patcher, all hooks, reader, writer, tick, unload |
| `DS1Mod/DS1Mod.FogLogger` | Logs every fog wall crossed (animation-based detection, not flag-based) |
| `DS1Mod/DS1Mod.HpLogger` | Polls player HP each tick; logs changes with delta and session minimum |
| `DS1Mod/DS1Mod.DiscordRPC` | Discord Rich Presence — shows current activity, deaths, last boss, session time |
| `DS1Mod/DS1Mod.AsylumSlam` | Asylum Demon slam-only AI (implements `IGamePatcher`, swaps the luabnd at load) |
| `DS1Mod/DS1Mod.GoofyDemon` | Asylum Demon with 10 random moods + on-screen HUD + fart entrance (v1.1.x) |

## Ground Truth References

- **Entity IDs**: `reference/FogMod-master/dist/fog.txt` — format: `- cXXXX_YYYY (Name). NPC NNNNNN @ENTITYID`
  Every EntityID in BossIds.cs must match the `@ENTITYID` value here. Wrong EntityIDs = boss slot not randomized.
- **EMEVD events**: `reference/Dark-Souls-Enemy-Randomizer-master/eventscripts/Remastered/`
- **EMEVD instruction names → Bank/ID**: `reference/Dark-Souls-Enemy-Randomizer-master/method_names.py`
- **EMEVD instruction definitions**: `tools/event_tools/ds1emedf.json` (DS1 EMEDF, used by the decompile tool and for cross-referencing Bank/ID pairs)
- **Human-readable event scripts**: `gamedata/decompiled_emevd/` — one `.evd.txt` per map, produced by `tools/event_tools/emevd_decompile`
- **Human-readable AI Lua**: `gamedata/decompiled_lua/` — decompiled via DSLuaDecompiler from `gamedata/DSR_Lua_Scripts_Folder/`

## Critical Design Decisions

1. **`NewInitAnimId = -1` for all boss replacements** — do not use `DefaultInitAnim` here. `-1` lets the game pick the model's natural idle. Anything else causes sleeping/frozen bosses before fights.

2. **Boss replacements use the replacement's own NpcParam** — not the slot's vanilla NpcParam. Original NpcParam encodes attack animation IDs for the vanilla model → T-pose on attack for any replacement. See `BossRandomizer.cs`.

3. **`EnemyIds.IsIgnored = true`** excludes a model from BOTH boss and regular enemy replacement pools. Used for Ceaseless Discharge (too large for foreign arenas) and Gwyndolin/Butterfly (environment-specific).

4. **`BossDef.CanReplace = false`** freezes a slot (never randomized). Bell Gargoyle 2+3, Stray Demon, etc. The model can still be excluded from the replacement pool separately via IsIgnored.

5. **`boss_overrides.json` must be in the app's output directory** — `AppContext.BaseDirectory`, not the repo root. The `.csproj` Content/PreserveNewest entry handles this automatically.

6. **Mods run inside the DSR process** — `GameMemory.Read<T>` is a direct pointer dereference. There is no inter-process overhead, but any unhandled exception in mod code will crash DSR. `ModLifecycleManager` wraps each mod call in try/catch.

7. **GoofyDemon mood flags use m18_01 section-5 range (`11815700..09`)** — section 7 is not allocated by the game, which is why an earlier build showed no HUD text. Always use flags from an allocated section for EMEVD bridging.

## EMEVD Instruction Reference

| Instruction | Bank | ID | Notes |
|---|---|---|---|
| ForceAnimationPlayback | 2003 | 18 | Hardcodes an animation ID — strip from boss intros |
| WarpCharacter | 2004 | 41 | Teleports to model-specific position — strip from boss intros |
| SetImmortality | 2004 | 12 | Seath crystal-prison phase — makes replacement unkillable |
| CreateMultipartNpc | 2004 | 22 | Seath tail — kill condition checks tail HP |

## VFX System

Enemy effects (IDs 10001–20000) live in per-map `sfx\FRPG_SfxBnd_m*.ffxbnd.dcx`.
`BossSfxMerger.MergeEnemyEffectsIntoCommon()` copies all of them into the globally-loaded `FRPG_SfxBnd_CommonEffects.ffxbnd.dcx`.
BND3 ID ranges: FFX `< 100000`, TPF `100000–199999`, FLVER `≥ 200000`.

## Bell Gargoyle Footnote

`fog.txt` lists THREE gargoyle entities in m10_01: `@1010800`, `@1010801`, `@1010802`.
All three are defined in BossIds.cs. Only 1010800 has `CanReplace: true`; the other two are frozen.
If you ever see a gargoyle still randomizing, check that all three EntityIDs are still present and correct.

## Seath Footnote

Two EMEVD patches required for Seath's slot (event IDs 11705396 and 11705397):
- Strip `(2004, 12)` — removes immortality applied during crystal-prison phase
- Strip `(2004, 22)` — removes invisible tail part whose HP is the actual kill condition

Without both patches, every Seath replacement is permanently unkillable.

## Build

```sh
# Build everything (requires .NET 9 for .slnx format support)
dotnet build DS1MegaRando.slnx

# Or build just the randomizer (works with .NET 8)
dotnet build DS1MegaRando.sln

# Run the UI
dotnet run --project DS1MegaRando.UI

# Full production build (randomizer + mod framework + C++ injector)
build.bat
```

Requires .NET 8 SDK (randomizer) and .NET 9 SDK (`DS1MegaRando.slnx`). The C++ injector (`DS1Mod.Injector`) requires MSVC. Game directory must be UXM-extracted DSR.

`DS1MegaRando.Test` targets `net9.0-windows` (uses newer APIs); all other projects target `net8.0-windows`.
