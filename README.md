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

## Legacy Python Components

Three standalone Python randomizers live in the repo as historical reference. They are **not** called by the C# application and do not need to be run:

- `DarkSoulsItemRandomizer-master/` — original Python item randomizer
- `Dark-Souls-Enemy-Randomizer-master/` — original Python enemy randomizer  
- `FogMod-master/` — original C# fog gate randomizer (WinForms UI); its compiled `dist/DS1R/event/` EMEVD files are still required at runtime by `FogGateWriter`
