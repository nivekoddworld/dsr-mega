# DS1 Mega Randomizer

A combined Dark Souls 1 Remastered randomizer that shuffles **fog gates** (area entrances), **items** (pickups, shops, starting gear), and **enemies** (placements and boss slots) in a single coordinated pass. A graph-based softlock checker re-rolls the seed (up to 10 times) until all required areas are reachable.

---

## Project Map

| Project | Purpose | Key files |
|---|---|---|
| **DS1MegaRando.UI** | WPF frontend — settings pages, progress overlay, spoiler viewer | `MainWindow.xaml`, `Pages/`, `ViewModels/` |
| **DS1MegaRando.Core** | Orchestrator only — coordinates all modules | `MegaRandomizer.cs` |
| **DS1MegaRando.Settings** | All user-configurable options | `MegaSettings.cs`, `GlobalSettings.cs`, `ItemSettings.cs`, `EnemySettings.cs`, `FogGateSettings.cs`, `SettingsSerializer.cs` |
| **DS1MegaRando.Annotations** | World metadata loaded from YAML (areas, entrances, key items) | `AnnotationData.cs`, `AnnotationLoader.cs` |
| **DS1MegaRando.Graph** | Directed graph of game world used for reachability analysis | `Graph.cs` (WorldGraph), `GraphConnector.cs`, `GraphChecker.cs`, `Expr.cs` |
| **DS1MegaRando.IO** | Reads/writes DSR game files via SoulsFormats | `GameFileReader.cs`, `GameFileWriter.cs`, `GameData.cs`, `BackupManager.cs` |
| **DS1MegaRando.FogGate** | Randomizes area entrance connections; writes MSB regions and EMEVD events | `FogGateRandomizer.cs`, `FogGateWriter.cs`, `FogGateResult.cs` |
| **DS1MegaRando.Items** | Randomizes item lot, shop, and starting loadout assignments | `ItemRandomizer.cs`, `KeyItemPlacer.cs`, `ItemPlacement.cs`, `LocationPool.cs`, `ItemPool.cs`, `ShopRandomizer.cs`, `StartingLoadoutRandomizer.cs` |
| **DS1MegaRando.Enemies** | Randomizes enemy and boss placements; patches EMEVD intro events | `EnemyRandomizer.cs`, `BossRandomizer.cs`, `EnemyPlacer.cs`, `EnemyPool.cs`, `EnemyScaler.cs`, `BossEmevdPatcher.cs`, `MimicRandomizer.cs` |
| **DS1MegaRando.Verification** | Checks the randomized world graph for softlocks | `SoftlockChecker.cs`, `ItemAccessibilityChecker.cs` |
| **DS1MegaRando.Spoiler** | Builds and serializes the spoiler log | `SpoilerLog.cs`, `SpoilerSerializer.cs` |
| **DS1MegaRando.Data** | Static game data — item IDs, enemy IDs, area definitions, embedded YAML/XML/ESD resources | `Areas/`, `Enemies/`, `Items/`, `Annotations/`, `Params/`, `ESDs/` |
| **SoulsFormats** | Binary format library for DSR files (BND3, PARAM, MSB1, EMEVD, DCX, …) | third-party, read-only |
| **SoulsIds** | Game ID utilities and event scripting helpers | third-party, read-only |

---

## DS1Mod — In-Process Mod Framework

A `dinput8.dll` sideloader that bootstraps a .NET 8 runtime inside the DSR process and hot-loads mods from `<game dir>/mods/`. Mods implement `IGameMod` (or extend `ModBase`) and receive game hooks, a memory reader/writer, and a 500 ms tick.

| Project | Purpose |
|---|---|
| **DS1Mod/DS1Mod.Injector** | C++ DLL (`dinput8.dll`): applies heap fix, loads hostfxr → .NET runtime |
| **DS1Mod/DS1Mod.Host** | .NET bootstrapper: scans `mods/`, loads each DLL in its own `AssemblyLoadContext`, drives the event pump |
| **DS1Mod/DS1Mod.Core** | In-process memory access, AOB pointer resolution, event hooks, game reader/writer |
| **DS1Mod/DS1Mod.SDK** | `ModBase` abstract class — the only dependency most mods need |
| **DS1Mod/DS1Mod.DemoMod** | SDK exercise — hits every API surface (bundled with the randomizer) |
| **DS1Mod/DS1Mod.FogLogger** | Logs every fog wall crossed (animation-based detection) |
| **DS1Mod/DS1Mod.HpLogger** | Polls and logs HP changes each tick |
| **DS1Mod/DS1Mod.DiscordRPC** | Discord Rich Presence: activity, deaths, last boss, session time |
| **DS1Mod/DS1Mod.AsylumSlam** | Asylum Demon slam-only AI (`IGamePatcher` — patches luabnd at launch) |
| **DS1Mod/DS1Mod.GoofyDemon** | Asylum Demon 10-mood gag mod with on-screen HUD and fart entrance |

See [`DS1Mod/README.md`](DS1Mod/README.md) for architecture details and the mod-writing guide.

---

## Reference Data

| Folder | Contents |
|---|---|
| **gamedata/DSR_Event_Folder/event/** | Extracted EMEVD bytecode (`.emevd.dcx`) — one per map; not modified by the randomizer |
| **gamedata/DSR_Lua_Scripts_Folder/script/** | Extracted Lua AI bytecode (`.luabnd.dcx`) — one per map; source for custom AI mods |
| **gamedata/decompiled_emevd/** | Human-readable decompilations of every EMEVD file; ground truth for event IDs and instruction args |
| **gamedata/decompiled_lua/** | Decompiled Lua AI sources (via DSLuaDecompiler); basis for hand-written AI mods |
| **tools/ds1_ai_mods/** | Hand-written AI scripts + fully Linux toolchain to compile and repack them |
| **tools/event_tools/** | EMEVD decompiler (`emevd_decompile`) + DS1 EMEDF definition (`ds1emedf.json`) |
| **reference/FogMod-master/** | Original C# fog gate randomizer; `dist/fog.txt` is ground truth for EntityIDs; `dist/DS1R/event/` EMEVD patches are used at runtime by `FogGateWriter` |
| **reference/Dark-Souls-Enemy-Randomizer-master/** | Original Python enemy randomizer; `method_names.py` maps EMEVD instruction names to Bank/ID |
| **reference/DarkSoulsItemRandomizer-master/** | Original Python item randomizer; historical reference only |
| **reference/DS1Randomizer/** | Archived Avalonia UI prototype; not part of the active solution |

---

## Dependency Order

```
Settings
  └─ Annotations
       └─ Graph
            ├─ IO  (+ SoulsFormats, Data)
            │    ├─ FogGate  (+ SoulsFormats, Data)
            │    │    ├─ Items   (+ Data)
            │    │    │    └─ Verification
            │    │    ├─ Enemies (+ SoulsFormats, Data)
            │    │    │    └─ Spoiler
            │    │    └─ (Verification, Spoiler also reference FogGate)
            └─ Core  (references all of the above)
                 └─ UI
```

No project imports from a downstream layer. This means you can edit (e.g.) `DS1MegaRando.Items` without loading any Enemies, FogGate, or Verification code.

---

## Randomization Flow

```
MegaRandomizer.Randomize(MegaSettings)
│
├─ GameFileReader.ReadAll()
│    └─ Reads GameParam.parambnd.dcx → PARAM tables (items, NPCs, shops, classes)
│    └─ Reads map\MapStudio\*.msb    → MSB1 per area (enemy placements, objects)
│
├─ AnnotationLoader.LoadFog()        → AnnotationData (areas, entrances, key items)
│
├─ [retry loop, max 10 attempts]
│    ├─ FogGateRandomizer.Randomize() → shuffles entrance connections (WorldGraph)
│    ├─ ItemRandomizer.Randomize()    → assigns items to lots/shops/loadouts
│    ├─ EnemyRandomizer.Randomize()   → swaps enemy models in MSB parts
│    └─ SoftlockChecker.Verify()      → throws if required area unreachable → retry
│
├─ BackupManager.BackupAll()         → .bak copies of original files
│
├─ GameFileWriter.WriteAll()
│    ├─ Writes GameParam.parambnd.dcx (item lots, shops, CharaInit)
│    ├─ FogGateWriter.Write()         → patches EMEVD events + MSB player spawns
│    ├─ BossEmevdPatcher.PatchAll()   → strips hardcoded anim IDs from boss events
│    └─ Writes *.msb files           → enemy model/param updates
│
└─ SpoilerSerializer.WriteText()     → spoiler_XXXXXXXX.txt
```

---

## Game File Formats

| File pattern | Format | Library | What we do |
|---|---|---|---|
| `param\GameParam\GameParam.parambnd.dcx` | BND3 + DCX | SoulsFormats | Read/write item lots, shop rows, class init params |
| `map\MapStudio\*.msb` / `*.msb.dcx` | MSB1 | SoulsFormats | Read enemy parts; write new models, regions, player spawns |
| `event\*.emevd.dcx` | EMEVD | SoulsFormats | Write fog-gate warp events; strip boss intro animations |
| `script\talk\*.talkesdbnd.dcx` | BND3 + ESD | SoulsFormats | Replace bonfire ESDs so Level Up is always available |
| `DS1MegaRando.Data\Annotations\ds1-fog.yaml` | YAML | YamlDotNet | Load area/entrance graph at runtime |
| `DS1MegaRando.Data\Params\*.xml` | PARAM layout XML | SoulsFormats | Apply paramdef so PARAM cells are writable by name |

---

## How to Edit Each Subsystem

| Goal | Go to |
|---|---|
| Change which options the user sees | `DS1MegaRando.UI/Pages/` |
| Add/change a setting field | `DS1MegaRando.Settings/` — add property then wire up in the relevant Page |
| Change how areas/entrances are defined | `DS1MegaRando.Data/Annotations/ds1-fog.yaml` + `AnnotationData.cs` |
| Change graph reachability logic | `DS1MegaRando.Graph/GraphChecker.cs` or `GraphConnector.cs` |
| Change how game files are read or written | `DS1MegaRando.IO/GameFileReader.cs` or `GameFileWriter.cs` |
| Add a new fog-gate behaviour | `DS1MegaRando.FogGate/FogGateRandomizer.cs` + `FogGateWriter.cs` |
| Change item placement / key item logic | `DS1MegaRando.Items/KeyItemPlacer.cs` or `ItemPlacement.cs` |
| Change enemy placement / boss logic | `DS1MegaRando.Enemies/EnemyRandomizer.cs` or `BossRandomizer.cs` |
| Add a new softlock check | `DS1MegaRando.Verification/SoftlockChecker.cs` |
| Change the spoiler log format | `DS1MegaRando.Spoiler/SpoilerLog.cs` |
| Add static game data (new item IDs, enemy IDs) | `DS1MegaRando.Data/Items/` or `Enemies/` |

---

## Build & Run

```
# Build all projects
dotnet build DS1MegaRando.slnx

# Run the UI
dotnet run --project DS1MegaRando.UI
```

Requires **.NET 8 SDK** on Windows. The app targets `net8.0-windows` (WPF).

The game directory must contain an unpacked (UXM-extracted) copy of Dark Souls Remastered. Set the path in the Global settings tab before randomizing.

---

## Boss Randomizer

### How it works

Every DSR boss is defined in `DS1MegaRando.Data/Enemies/BossIds.cs` as a `BossDef` record with:

| Field | Purpose |
|---|---|
| `MapId` | Which MSB file the boss lives in |
| `EntityId` | The unique integer the game uses to identify the enemy part in EMEVD scripts |
| `ModelId` | The character model (`c####`) |
| `Name` | Display name for the spoiler log |
| `CanReplace` | `false` → slot is frozen (never randomized); model also excluded from replacement pool |
| `EmevdPatches` | EMEVD instructions to strip from the boss intro event so the replacement model isn't locked in a model-specific state |

**EntityIDs are ground truth** — they must exactly match the value at `@ENTITYID` in `reference/FogMod-master/dist/fog.txt`. Wrong EntityIDs are the root cause of bosses not being randomized at all.

### Replacement pool rules

A boss model enters the replacement pool only if:
1. Its `BossDef.CanReplace` is `true`
2. Its model is found in `GameData.KnownModels` (the game has the file)
3. `EnemyIds.ByModelId(modelId).IsIgnored` is `false`

`IsIgnored = true` is used for bosses that are too environment-specific or too large to work in any foreign arena (Ceaseless Discharge is the main example).

### NpcParam and animation

- Replacement bosses always use **the replacement model's own NpcParam** (not the slot's vanilla NpcParam). This prevents T-pose attacks caused by animation ID mismatches.
- `InitAnimId` is always set to `-1` for boss replacements, which tells the game to use the model's default idle animation.

### EMEVD patching

The `BossEmevdPatcher` scans each boss slot's intro event and strips instructions that would break a replacement model:

| Instruction | Bank/ID | Why stripped |
|---|---|---|
| `ForceAnimationPlayback` | `(2003, 18)` | Plays a hardcoded animation ID that only exists on the vanilla boss |
| `WarpCharacter` | `(2004, 41)` | Teleports to a position only valid for the vanilla model |
| `SetImmortality` | `(2004, 12)` | Seath's crystal-prison phase makes any replacement immortal and unkillable |
| `CreateMultipartNpc` | `(2004, 22)` | Seath's invisible tail — kill condition checks tail HP, so replacement can never die |

---

## Boss Override Config (`boss_overrides.json`)

`boss_overrides.json` in the repo root (also deployed alongside the app executable) controls three optional overrides:

### `pinned` — force a specific replacement

```json
"pinned": {
  "Gaping Dragon": "Chaos Witch Quelaag"
}
```

### `blocked` — prevent certain replacements in a slot

```json
"blocked": {
  "Bell Gargoyles": ["Iron Golem", "Seath the Scaleless"]
}
```

The defaults in the file cover all known arena-size mismatches and crash combos.

### `positions` — override spawn coordinates

```json
"positions": {
  "Taurus Demon": { "x": 3.372, "y": 15.814, "z": -115.055, "rotY": -73.54 }
}
```

Any component omitted keeps the vanilla MSB value. The slot name (key) is what normally occupies that arena — the override applies to whatever boss is placed there, not to the Taurus Demon model specifically.

> **Important**: `boss_overrides.json` must be in the same directory as the application executable. The `.csproj` wires this up automatically via `Content/PreserveNewest`; if you run from a custom output path you may need to copy the file manually.

---

## VFX System

Enemy particle effects (hit sparks, magic auras, smoke trails) are stored in per-map SFX bundles: `sfx\FRPG_SfxBnd_m*.ffxbnd.dcx`. When a boss is moved to a foreign map the effects aren't present in that map's bundle and the boss appears without any VFX.

`DS1MegaRando.Core/BossSfxMerger` fixes this by:

1. Scanning all 26 map SFX bundles
2. Collecting every entry whose internal path name matches the enemy-effect ID range (10001–20000) for `.ffx`, `.flver`, and `.tpf` files
3. Adding any missing entries to `sfx\FRPG_SfxBnd_CommonEffects.ffxbnd.dcx`, which is loaded in every area

The CommonEffects bundle uses BND3 ID ranges: `< 100000` = FFX effects, `100000–199999` = TPF textures, `≥ 200000` = FLVER models. New entries are appended after the current maximum ID in each range.

---

## Boss-Specific Notes

| Boss | Notes |
|---|---|
| **Bell Gargoyles** | Three entity instances (1010800/01/02). Only the first is randomized; the other two are frozen (`CanReplace: false`) to avoid mid-fight phase spawns breaking. |
| **Ceaseless Discharge** | Randomizable slot, but `IsIgnored: true` prevents the c5250 model from appearing as a replacement elsewhere — the arena is too small. |
| **Seath the Scaleless** | EMEVD-patched to remove immortality lock and multipart tail creation. Without these patches, any replacement is unkillable. |
| **Dark Sun Gwyndolin** | `CanReplace: true`; `IsIgnored: true` so Gwyndolin never appears in other arenas. |
| **Moonlight Butterfly** | Same pattern as Gwyndolin. Replacement always spawned at `(180.717, 8.066, 29.612)` via position override. |
| **Stray Demon** | Treated as a regular non-replaceable enemy (CanReplace: false) because its EntityID belongs to the same sub-boss category. |

---

## Legacy Python Components

Three standalone Python randomizers live in the repo as historical reference. They are **not** called by the C# application and do not need to be run:

- `reference/DarkSoulsItemRandomizer-master/` — original Python item randomizer
- `reference/Dark-Souls-Enemy-Randomizer-master/` — original Python enemy randomizer  
- `reference/FogMod-master/` — original C# fog gate randomizer (WinForms UI); its compiled `dist/DS1R/event/` EMEVD files are still required at runtime by `FogGateWriter`
