# DS1Mod — Dark Souls Remastered Mod Framework

`dinput8.dll` sideloads into DSR at startup. It applies the heap fix, then bootstraps
the .NET 8 runtime in-process via `hostfxr` and loads every `*.dll` found in
`<game dir>/mods/`.

## Architecture

```
DarkSoulsRemastered.exe
└── dinput8.dll  (C++, DS1Mod.Injector)
    ├── ApplyHeapFix()      — prevents crash on large heaps
    └── InitModLoader()     — loads hostfxr → .NET runtime
        └── DS1Mod.Host.dll (DS1Mod.Host)
            └── ModLifecycleManager
                ├── scans mods/*.dll
                ├── loads each into its own AssemblyLoadContext
                ├── calls IGameMod.OnLoad(IModContext)
                └── EventPump (500 ms tick)
                    ├── polls event flags (boss kills, fog gates, deaths, levels)
                    └── calls IGameMod.OnTick() on each mod
```

## Writing a Mod

Reference `DS1Mod.SDK` (or `DS1Mod.Core`) and implement `IGameMod`:

```csharp
using DS1Mod.SDK;      // ModBase — easier than implementing IGameMod directly
using DS1Mod.Core;

public class DeathCounter : ModBase
{
    public override string Name    => "Death Counter";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    private int _deaths;

    public override void OnLoad(IModContext ctx)
    {
        ctx.Hooks.PlayerDied += () =>
            Console.WriteLine($"Deaths: {++_deaths}");
    }
}
```

Build as a class library targeting `net8.0-windows`. Drop the DLL into
`<game dir>/mods/`. Launch via **▾ → Launch with Mod Framework**.

## Key Types

| Type | Role |
|---|---|
| `IGameMod` | Mod entry point — `OnLoad`, `OnUnload`, `OnTick` |
| `ModBase` | Abstract base class; all methods are virtual no-ops |
| `IModContext` | Passed to `OnLoad`; provides hooks, reader, writer, mods dir |
| `IGameHooks` | Events: `BossKilled`, `FogGateEntered`, `PlayerDied`, `PlayerLeveledUp` |
| `IGameReader` | Read player state, stats, souls, soul level, event flags |
| `IGameWriter` | Write event flags |
| `EventFlags` | Direct in-process bit-array access (flag get/set) |
| `GameMemory` | Unsafe in-process pointer reads/writes |

## Memory Model

Mods run **inside** the DSR process. `GameMemory.Read<T>` is a direct pointer
dereference — no `ReadProcessMemory`, no inter-process overhead.

`GameMemory.Initialize()` is called once by `DS1Mod.Host` at startup before any
mod's `OnLoad` is invoked, so mods can call `GameMemory.Read` safely from `OnLoad`.

## Installing via the Randomizer

The DS1 Mega Randomizer UI has a **MODS** tab. Click **Install Mod…** to copy a
DLL into the mods folder, or **Remove** to uninstall it.
