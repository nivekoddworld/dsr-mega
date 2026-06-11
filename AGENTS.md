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
| `src/DS1MegaRando.Data/Enemies/BossIds.cs` | All 32 boss slot definitions (EntityID, ModelID, EMEVD patches) |
| `src/DS1MegaRando.Enemies/BossRandomizer.cs` | Boss assignment, NpcParam selection, InitAnimId |
| `src/DS1MegaRando.Enemies/BossEmevdPatcher.cs` | Strips model-specific EMEVD instructions from boss intro events |
| `src/DS1MegaRando.Core/BossSfxMerger.cs` | Merges enemy effect entries into CommonEffects bundle (VFX fix) |
| `src/DS1MegaRando.Core/GameFileWriter.cs` | Write pipeline — calls SFX merge after MSB write |
| `src/DS1MegaRando.IO/BackupManager.cs` | .bak backups of all modified game files |
| `boss_overrides.json` | User config: pinned replacements, blocked combos, spawn positions |
| `src/DS1MegaRando.UI/DS1MegaRando.UI.csproj` | Includes boss_overrides.json as Content/PreserveNewest |

## Key Files — DS1Mod Framework

| File | Role |
|---|---|
| `DS1Mod/framework/DS1Mod.Injector/modloader.cpp` | C++ DLL entry; disables Arxan DRM via dearxan FFI, applies heap fix, bootstraps .NET runtime via hostfxr |
| `DS1Mod/framework/DS1Mod.Host/ModLifecycleManager.cs` | Scans `mods/`, loads each DLL into its own `AssemblyLoadContext`, drives the tick loop |
| `DS1Mod/framework/DS1Mod.Core/GameMemory.cs` | Direct in-process pointer reads/writes (no ReadProcessMemory) |
| `DS1Mod/framework/DS1Mod.Core/GamePointers.cs` | AOB scan to resolve DSR version-specific base pointers |
| `DS1Mod/framework/DS1Mod.Core/EventPump.cs` | 500 ms poll loop; fires `BossKilled`, `FogGateEntered`, `PlayerDied`, `PlayerLeveledUp` |
| `DS1Mod/framework/DS1Mod.Core/IGuiMod.cs` | Interface for ImGui overlay mods — `OnGui()` called every frame on the render thread |
| `DS1Mod/framework/DS1Mod.SDK/ModBase.cs` | Abstract base class for mods — implement `Name/Version/Author` and override hooks |
| `DS1Mod/framework/DS1Mod.Rendering/ImGuiRenderer.cs` | Bridges C++ Present hook → managed `IGuiMod` mods |
| `DS1Mod/framework/DS1Mod.Modding/GamePatch.cs` | DCX round-trip + backup wrapper for PARAM/FMG/EMEVD/Lua/ESD edits |
| `DS1Mod/framework/DS1Mod.Modding/EsdEditor.cs` | Fluent editor for talk ESD (dialog/bonfire menus) with verified condition functions |
| `DS1Mod/framework/DS1Mod.Modding/ActionEsd.cs` | Fluent editor for action ESD (player/enemy animation states) with verified function IDs |

## Bundled Mods

| Mod | Purpose |
|---|---|
| `DS1Mod/mods/DS1Mod.DemoMod` | SDK exercise — hits every surface: patcher, all hooks, reader, writer, tick, unload |
| `DS1Mod/mods/DS1Mod.FogLogger` | Logs every fog wall crossed (animation-based detection, not flag-based) |
| `DS1Mod/mods/DS1Mod.HpLogger` | Polls player HP each tick; logs changes with delta and session minimum |
| `DS1Mod/mods/DS1Mod.DiscordRPC` | Discord Rich Presence — shows current activity, deaths, last boss, session time |
| `DS1Mod/mods/DS1Mod.AsylumSlam` | Asylum Demon slam-only AI (implements `IGamePatcher`, swaps the luabnd at load) |
| `DS1Mod/mods/DS1Mod.GoofyDemon` | Asylum Demon with 10 random moods + on-screen HUD + fart entrance + trinket drop (v1.3) |
| `DS1Mod/mods/DS1Mod.ImGuiDemo` | Minimal ImGui overlay mod — HP stats panel + debug window, template for `IGuiMod` |

## Ground Truth References

- **Entity IDs**: `reference/FogMod-master/dist/fog.txt` — format: `- cXXXX_YYYY (Name). NPC NNNNNN @ENTITYID`
  Every EntityID in BossIds.cs must match the `@ENTITYID` value here. Wrong EntityIDs = boss slot not randomized.
- **EMEVD events**: `reference/Dark-Souls-Enemy-Randomizer-master/eventscripts/Remastered/`
- **EMEVD instruction names → Bank/ID**: `reference/Dark-Souls-Enemy-Randomizer-master/method_names.py`
- **EMEVD instruction definitions**: `tools/event_tools/ds1emedf.json` (DS1 EMEDF, used by the decompile tool and for cross-referencing Bank/ID pairs)
- **Human-readable event scripts**: `gamedata/decompiled_emevd/` — one `.evd.txt` per map, produced by `tools/event_tools/emevd_decompile`
- **Human-readable AI Lua**: `gamedata/decompiled_lua/` — decompiled via DSLuaDecompiler from `gamedata/DSR_Lua_Scripts_Folder/`

## ESD Modding Patterns

Modders can use the ESD editing framework to build:

1. **Dialog & progression mods** — unlock NPCs/shops conditionally, create branching conversations, gate questlines behind flags
2. **Bonfire menu gating** — unlock/lock existing bonfire menu items (Level Up, Homeward Bone, Leave) by modifying their flag gates. NOTE: The bonfire UI is hard-coded with a fixed menu layout; you cannot add entirely new menu items. See `SetTalkListGateFlag()` in `EsdEditor.cs` and `PatchBonfireEsds()` pattern in `GameFileWriter.cs` for the bulk-replace approach.
3. **Boss AI rebalancing** — tune attack timing, combo routing, spell gating via action ESD state transitions
4. **Player action mods** — gate rolling/attacking/casting behind stamina/stun conditions, modify animation durations, control cancellation windows
5. **Randomizer integration** — gate randomized items/enemies behind custom dialog unlocks, replace binary blob patches with programmatic edits

## Critical Design Decisions

1. **`NewInitAnimId = -1` for all boss replacements** — do not use `DefaultInitAnim` here. `-1` lets the game pick the model's natural idle. Anything else causes sleeping/frozen bosses before fights.

2. **Boss replacements use the replacement's own NpcParam** — not the slot's vanilla NpcParam. Original NpcParam encodes attack animation IDs for the vanilla model → T-pose on attack for any replacement. See `BossRandomizer.cs`.

3. **`EnemyIds.IsIgnored = true`** excludes a model from BOTH boss and regular enemy replacement pools. Used for Ceaseless Discharge (too large for foreign arenas) and Gwyndolin/Butterfly (environment-specific).

4. **`BossDef.CanReplace = false`** freezes a slot (never randomized). Bell Gargoyle 2+3, Stray Demon, etc. The model can still be excluded from the replacement pool separately via IsIgnored.

5. **`boss_overrides.json` must be in the app's output directory** — `AppContext.BaseDirectory`, not the repo root. The `.csproj` Content/PreserveNewest entry handles this automatically.

6. **Mods run inside the DSR process** — `GameMemory.Read<T>` is a direct pointer dereference. There is no inter-process overhead, but any unhandled exception in mod code will crash DSR. `ModLifecycleManager` wraps each mod call in try/catch.

7. **GoofyDemon mood flags use m18_01 section-5 range (`11815700..09`)** — section 7 is not allocated by the game, which is why an earlier build showed no HUD text. Always use flags from an allocated section for EMEVD bridging.

8. **Arxan DRM is disabled at game launch** — `modloader.cpp` calls `dearxan_neuter_arxan()` (FFI to dearxan crate) before game logic runs. This disables all Arxan integrity checks. Requires linking against `dearxan.lib` (pre-compiled or built from https://github.com/tremwil/dearxan). See `DS1Mod/framework/DS1Mod.Injector/ARXAN_SETUP.md` for build instructions.

## ESD (EZState) Editing Framework

ESD is FromSoft's graph-based state machine scripting used for NPC dialog, bonfire menus, and player/enemy animations. The DS1Mod modding framework provides fluent C# APIs for editing both Talk ESD (dialog) and Action ESD (animation states).

### Talk ESD — NPC Dialog & Bonfire Menus

**Entry point**: `GamePatch.EditEsd(relPath, esdName, edit)` or `GamePatch.EditEsdBySize(relDir, vanillaSize, edit)`

**Verified condition functions** (confirmed via binary analysis + game file extraction):
- `GetEventFlag(flagId)` — check flag state
- `GetMenuSelection()` — which menu item is highlighted
- `GetDialogButtonResult()` — which button player pressed
- `IsGenericDialogOpen(personId)` — dialog open check
- `GetTimeInState()` — elapsed time in current state (idle timeout)
- `DialogClosedWithButton(button)` — "dialog just closed with answer X" (vanilla warp pattern)

**Verified commands** (Bank 1 talk commands):
- `SetEventFlag(flagId, on)` — set flag ON/OFF (3187 uses in corpus)
- `OpenGenericDialog(type, msgId, btnType, numBtns, unk)` — show yes/no dialog
- `AddTalkListData(listIdx, talkId, gateFlag=-1)` — bonfire/shop menu item (gateFlag=-1 always show)
- `AddTalkListDataIf(condition, listIdx, talkId, unk)` — conditional menu item (FogMod warp style)
- `ClearTalkListData()` — clear the menu list before repopulating with `AddTalkListData`
- `ShowShopMessage(a=0, b=0, c=0)` — shop/wares message (3 int args; vanilla always passes `(0,0,0)`)

Note: there is no Talk ESD command for setting the player's respawn bonfire — that's an
EMEVD-level operation (`SetPlayerRespawnPoint`). Verified by walking all 357 Talk ESDs in
the DSR corpus with `SoulsFormats.ESD` and tabulating `(bank, commandId) → argCount`;
Bank 1 command 101 ("UpdateRespawnPoint") never appears.

**Key use case**: Bonfire menu gating via `EsdEditor.SetTalkListGateFlag()` — unlock/lock bonfire menu items (they are predefined in the ESD; you are only changing visibility flags):
```csharp
// Unlock the Level Up menu item by removing its flag gate
g.EditEsdBySize("script/talk", 23012, esd =>
    esd.SetTalkListGateFlag(1, 4, 15000100, -1));  // -1 = always visible

// Or gate it behind a flag:
g.EditEsdBySize("script/talk", 23012, esd =>
    esd.SetTalkListGateFlag(1, 4, 15000100, 11810000));  // flag 11810000 must be ON
```
Bonfire UI layout is fixed; adding entirely new menu items is not supported.

### Action ESD — Player & Enemy Animation States

**Entry point**: `GamePatch.EditActionEsd(esd, edit)` (esd = "c0000" for player or "enemyCommon" for enemies)

**Verified condition functions** (extracted from c0000.esd and enemyCommon.esd):
- `Fn0()` (398×) — default/always-true check
- `Fn112()` (240×) — attack animation/combo gating
- `Fn109()` (236×) — button release or state routing
- `Fn2()` (223×) — world state (airborne, stamina, animation)
- `Fn3()` (219×) — complex checks (stun, equipment, buffs)
- `Fn116()` (216×) — spell/item/ability gating
- `Fn111()` (204×) — dodge/roll/backstab timing
- `Fn115()` (204×) — movement logic
- `Fn104()` (195×) — inventory/stance/sync
- Enemy-specific: `EnemyFn107()`, `EnemyFn118()`, `EnemyFn120()`

**Commands**: `SetUpperBodyAnimation(animId, duration)`, `SetLowerBodyAnimation(animId, duration)`, `CancelAnimation()`, `SetItemInUse(active)`, `SyncAnimationAtInit(active)`, or `RawCommand(bank, cmdId, args)` for discovery.

**Key use case**: Rebalance combat by gating actions or changing animation timing:
```csharp
g.EditActionEsd("c0000", esd =>
{
    var idle = esd.GetOrAddState(0, 0);
    // Prevent attacking while stunned
    idle.InsertTransition(0, 0, 0, ActionEsdBytecode.Not(ActionEsdBytecode.Fn3()), 0);
});
```

**Bytecode VM** (both contexts): 32-bit stack VM with push, call, compare, logic ops. All helpers return complete expressions ending with `0xA1`. Composition helpers (`And`, `Or`, `Not`) strip terminators before merging, so nesting is safe.

**Utility**: `ActionEsdBytecode.VerifyFunctionId(esd)` dumps all condition functions in an ESD with frequency ranking, for calibrating unknown functions.

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
dotnet run --project src/DS1MegaRando.UI

# Full production build (randomizer + mod framework + C++ injector)
build.bat
```

Requires .NET 8 SDK (randomizer) and .NET 9 SDK (`DS1MegaRando.slnx`). The C++ injector (`DS1Mod.Injector`) requires MSVC. Game directory must be UXM-extracted DSR.

`DS1MegaRando.Test` targets `net9.0-windows` (uses newer APIs); all other projects target `net8.0-windows`.
