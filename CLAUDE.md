# DS1 Mega Randomizer — Developer Context

## Repository

- Repo: `nivekoddworld/dsr-mega`
- Active branch: `claude/brave-wozniak-ceEJ1`

## Project Overview

WPF app (.NET 8, Windows only) that randomizes Dark Souls Remastered. Four independent systems:

- **Fog Gate** — shuffles where fog doors lead
- **Item** — randomizes pickups, shops, starting gear
- **Enemy** — randomizes regular enemies, minibosses, and all boss slots
- **Mimic** — swaps mimic positions

Entry point: `DS1MegaRando.UI` → `MegaRandomizer.cs` orchestrates everything.

## Key Files

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

## Ground Truth References

- **Entity IDs**: `FogMod-master/dist/fog.txt` — format: `- cXXXX_YYYY (Name). NPC NNNNNN @ENTITYID`
  Every EntityID in BossIds.cs must match the `@ENTITYID` value here. Wrong EntityIDs = boss slot not randomized.
- **EMEVD events**: `Dark-Souls-Enemy-Randomizer-master/eventscripts/Remastered/`
- **EMEVD instruction names → Bank/ID**: `Dark-Souls-Enemy-Randomizer-master/method_names.py`

## Critical Design Decisions

1. **`NewInitAnimId = -1` for all boss replacements** — do not use `DefaultInitAnim` here. `-1` lets the game pick the model's natural idle. Anything else causes sleeping/frozen bosses before fights.

2. **Boss replacements use the replacement's own NpcParam** — not the slot's vanilla NpcParam. Original NpcParam encodes attack animation IDs for the vanilla model → T-pose on attack for any replacement. See `BossRandomizer.cs`.

3. **`EnemyIds.IsIgnored = true`** excludes a model from BOTH boss and regular enemy replacement pools. Used for Ceaseless Discharge (too large for foreign arenas) and Gwyndolin/Butterfly (environment-specific).

4. **`BossDef.CanReplace = false`** freezes a slot (never randomized). Bell Gargoyle 2+3, Stray Demon, etc. The model can still be excluded from the replacement pool separately via IsIgnored.

5. **`boss_overrides.json` must be in the app's output directory** — `AppContext.BaseDirectory`, not the repo root. The `.csproj` Content/PreserveNewest entry handles this automatically.

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
dotnet build DS1MegaRando.slnx
dotnet run --project DS1MegaRando.UI
```

Requires .NET 8 SDK on Windows. Game directory must be UXM-extracted DSR.
