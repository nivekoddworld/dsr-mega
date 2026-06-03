# DS1Mod — Dark Souls Remastered Mod Framework

Two components that work alongside the DS1 Mega Randomizer.

## DS1Mod.Injector (C++ → `dinput8.dll`)

A minimal DLL loaded by DSR at startup via DLL sideloading.

**What it does:**
- Fixes the FFX and Lua heap sizes so DSR doesn't crash when `BossSfxMerger` merges all enemy effects into `FRPG_SfxBnd_CommonEffects.ffxbnd.dcx`
- Forwards `DirectInput8Create` to the real system DLL so game input still works

**Build (Windows, Visual Studio 2022):**
```
1. Open DS1Mod.Injector.vcxproj in Visual Studio
2. Set configuration: Release | x64
3. Build → outputs dinput8.dll
```
Or from a VS Developer Command Prompt:
```
msbuild DS1Mod.Injector.vcxproj /p:Configuration=Release /p:Platform=x64
```

**Install:**
Drop `dinput8.dll` into the DSR game folder (same directory as `DarkSoulsRemastered.exe`).

**Compatibility:** DSR 1.03.1 (Steam). If FromSoftware patches the exe, the byte patterns in `heapfix.cpp` may need updating — use x64dbg to find the new `mov ecx, 00013200` instruction.

---

## DS1Mod.Core (C# .NET 8)

An external-process library that reads and writes DSR game state at runtime.

**Build:**
```
dotnet build DS1Mod.Core.csproj
```

**Usage:**
```csharp
using DS1Mod.Core;

// Attach to running DSR process
var session = GameSession.TryCreate(seed: myRandomizerSeed);
if (session is null) return; // game not running

// Subscribe to events
session.Bosses.BossKilled   += kill => Console.WriteLine($"Killed: {kill.BossName}");
session.FogGates.GateOpened += gate => Console.WriteLine($"Opened: {gate.Name}");

// Start background polling (default 500 ms interval)
session.StartPolling();

// Progress is also written to:
//   %APPDATA%\DS1Mod\progress_{seed}.json
// so the randomizer UI can read it without attaching to the game.
```

**API surface:**

| Type | Purpose |
|---|---|
| `GameSession` | Top-level; owns process handle + all trackers |
| `EventFlags` | Read/write any event flag by integer ID |
| `BossTracker` | Fires `BossKilled` event for each of the 24 known bosses |
| `FogGateTracker` | Fires `GateOpened` when a fog gate is first passed |
| `PlayerStateReader` | Returns current X/Y/Z position and map ID |
| `ProgressLog` | Serializes session state to JSON sidecar |
| `IGameMode` | Plugin interface for custom game modes |

**Memory offsets** are in `Offsets.cs`. They target DSR 1.03.1; verify against [JohrnaJohrna/RemasterCETable](https://github.com/JohrnaJohrna/RemasterCETable) if the game is updated.

---

## Adding a Custom Game Mode

```csharp
public class SpeedrunMode : IGameMode
{
    public string Name => "Speedrun";
    private Stopwatch _sw = new();

    public void OnAttached(GameSession session) => _sw.Start();
    public void OnDetached() => _sw.Stop();

    public void OnBossKilled(BossKill kill)
        => Console.WriteLine($"{call.BossName} at {_sw.Elapsed}");

    public void OnFogGateOpened(FogGate gate) { }
    public void OnPlayerMoved(float x, float y, float z, string mapId) { }
}

// Register:
session.AddMode(new SpeedrunMode());
```
