# Dark Souls Mod Loader

A cross-platform mod loader UI for Dark Souls Remastered, built with Qt for C#.

## Project Structure

```
DarkSoulsModLoader/
├── src/
│   ├── Program.cs              # Application entry point
│   ├── Views/                  # QML view implementations
│   ├── ViewModels/             # C# ViewModels (MVVM pattern)
│   ├── Services/               # Core services
│   │   ├── ModService.cs       # Mod discovery, loading, config I/O
│   │   ├── GameLaunchService.cs # DSR launch + injection
│   │   └── ConfigService.cs    # Config file management
│   └── Models/                 # Data models
├── qml/
│   └── main.qml                # Main UI window + tab navigation
├── resources/                  # Icons, assets, etc.
└── DarkSoulsModLoader.csproj   # Project file
```

## Key Features (MVP)

1. **Mod Browser** — List installed mods from `<game>/mods/`
2. **Mod Configuration** — Load/save configs from `mods/settings/{ModName}.json`
3. **Game Launch** — Launch DSR with mods injected
4. **Randomizer Integration** — Configure EnemyRandomizer settings
5. **Settings** — Game directory picker, backup/restore, clear cache

## Architecture

- **Framework:** Qt for C# (Qt Bridges)
- **.NET:** 8.0-windows
- **Language:** C# + QML
- **Pattern:** MVVM (C# ViewModels + QML Views)

## Dependencies

- `DS1Mod.SDK` — mod interface and metadata
- `DS1Mod.Modding` — game file patching utilities
- `DS1Mod.Core` — game memory/pointer access
- Qt for C# (Qt Bridges, public beta)

## Building

```sh
dotnet build DarkSoulsModLoader.csproj
dotnet run --project DarkSoulsModLoader
```

## Mod Configuration

Mods expose configuration as JSON files in `<game>/mods/settings/`:

```
mods/
├── settings/
│   ├── EnemyRandomizer.json
│   ├── HpLogger.json
│   └── FogLogger.json
├── EnemyRandomizer.dll
├── HpLogger.dll
└── FogLogger.dll
```

The mod loader reads these configs, displays UI controls, and saves changes back to the same location.

## Next Steps

- [ ] Wire Qt for C# dependencies
- [ ] Implement ModService + GameLaunchService
- [ ] Build mod browser QML view
- [ ] Add randomizer config editor
- [ ] Test end-to-end (load mod → launch game)
