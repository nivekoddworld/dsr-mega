# DS1 Mega Randomizer UI Redesign — Plan Rundown

## Current State (as of 2026-06-11)

**Recent work (8 commits pushed to `claude/sharp-sagan-mTLFK`):**
1. Added `DS1Mod.Core.MapIds` — named constants for all DSR map IDs
2. Added framework enemy-randomization APIs to `DS1Mod.Modding`:
   - `EnemyResult` / `EnemyPlacement` — data model for placements, stat mods, spoiler logs
   - `EnemyPatcher` — MSB1 helpers for enemy part editing
   - `EmevdExtensions` — instruction-stripping for boss intro patching
3. Created new **`DS1Mod.EnemyRandomizer` mod project** — self-contained `IGamePatcher` mod that randomizes enemies/bosses at game launch (not just from the UI)
4. Gutted `HpLoggerMod` to an empty stub (superseded by the new project)
5. Added `AGENTS.md` — developer-context guide
6. Vendored dearxan source under `tools/arxan_patcher/`
7. Untracked and gitignored build artifacts (MSVC intermediates, daemon.pid, Claude state)

**Key insight:** The DS1MegaRando has evolved from a pure randomizer into a **mod loader framework**. The UI needs to reflect this.

---

## The Problem

The current `DS1MegaRando.UI` (Windows Forms / WPF) is:
- **Dated** — shows its age, doesn't fit the mod-loader paradigm
- **Limited scope** — designed as "randomizer config + run", not "mod manager + randomizer + launcher"
- **Not scalable** — adding mod management, live logs, config editing feels grafted on

---

## The Plan

### Scope: Build a new `DS1MegaRando.UI.WinUI3` project

**Replace the old UI with a modern, clean WinUI 3 app** that serves the **mod loader + randomizer** dual purpose.

**Key features (MVP):**
1. **Mod Browser & Manager**
   - List installed mods from `<game>/mods/`
   - Load/enable/disable mods without restart (eventually)
   - Show mod metadata (Name, Version, Author)
   - Toggle mod load order (if applicable)

2. **Randomization Config**
   - Load/save randomization presets
   - Expose the EnemyRandomizer mod config in the UI (enemy_config.json)
   - Expose boss_overrides.json for placement/stat tweaks
   - Show randomizer options (fog gates, items, enemies, mimics) if the WPF randomizer logic gets ported

3. **Game Launch**
   - Pick game directory
   - Launch DSR with mods + randomization injected
   - Show mod + randomizer status before launch

4. **Live Logs / Debug Output**
   - Tail mod logs in real-time (HpLogger, FogLogger, EnemyRandomizer output)
   - Debug console for in-game events (boss kills, level ups, etc.)

5. **Settings**
   - Game directory picker
   - Backup/restore game files
   - Clear caches, reset config to defaults

### Tech Stack

- **Framework:** WinUI 3 (Microsoft's modern Windows UI framework)
- **.NET:** 8.0-windows (same as DS1Mod framework)
- **Language:** C# (unified with existing codebase)
- **Platform:** Windows-only (DSR only runs on Windows)
- **Architecture:** MVVM (WinUI 3 best practice)

**Why WinUI 3 over WPF/WinForms/Qt Bridges:**
- Modern, native Windows 11 design language out of the box
- XAML-based (familiar if you know WPF)
- Tight Windows integration (file dialogs, process launching)
- Mature and production-proven
- Qt Bridges C# is public beta (3 weeks old, too risky for ship)

### New Project Structure

```
DS1MegaRando.UI.WinUI3/
├── DS1MegaRando.UI.WinUI3.csproj
├── Views/
│   ├── MainWindow.xaml
│   ├── ModsPage.xaml
│   ├── RandomizerPage.xaml
│   ├── LaunchPage.xaml
│   └── SettingsPage.xaml
├── ViewModels/
│   ├── ModManagerViewModel.cs
│   ├── RandomizerViewModel.cs
│   ├── LaunchViewModel.cs
│   └── SettingsViewModel.cs
├── Services/
│   ├── ModService.cs (load mods, list, manage)
│   ├── GameLaunchService.cs (spawn DSR + inject mods)
│   ├── ConfigService.cs (load/save randomizer config)
│   └── LogService.cs (tail logs, expose events)
└── App.xaml
```

### Integration Points

- **DS1Mod.SDK** — reference to load/enumerate mods
- **DS1Mod.Modding** — call into GamePatch for config edits
- **DS1Mod.EnemyRandomizer** — expose its config UI
- **SoulsFormats** — if we keep WPF randomizer logic, we can port it to the mod
- **Existing randomizer logic** — can stay in the WPF app (or port selectively)

---

## Non-Goals

- **Cross-platform** — Windows-only (but architecture is clean enough to port later if needed)
- **Replace the WPF randomizer entirely yet** — the new UI can coexist; old one can be deprecated gradually
- **Full feature parity day 1** — MVP is mod loader + EnemyRandomizer config; the old randomizer features can be added/ported incrementally

---

## Next Steps

1. **Scaffold WinUI 3 project** (`DS1MegaRando.UI.WinUI3.csproj`)
2. **Create MVVM ViewModels** for mods, randomizer, launch, settings
3. **Build mod browser view** (list, enable/disable, metadata)
4. **Build mod launcher service** (game dir picker, spawn DSR + inject)
5. **Wire randomizer config editor** (load enemy_config.json, show UI controls)
6. **Add live log viewer** (tail from mod output streams)
7. **Test end-to-end** (load a mod, configure randomizer, launch game)

---

## Success Criteria

- UI loads mods from game directory
- Randomizer config loads/saves without errors
- Game launches with mods + randomization active
- Live logs stream to the UI
- No dependencies on the old WPF UI (can be left behind, deprecated)
