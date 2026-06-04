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

---

## Mods in this Repo

| Mod | Type | What it does |
|---|---|---|
| **DS1Mod.DemoMod** | `IGameMod` + `IGamePatcher` | Exercises every SDK surface — all hooks, reader/writer, tick, unload. Good starting template. |
| **DS1Mod.FogLogger** | `IGameMod` | Logs every fog wall crossed (animation-based, catches all fogs not just boss fogs). Writes to console + `FogLogger.log`. |
| **DS1Mod.HpLogger** | `IGameMod` | Polls player HP on each tick; logs changes with delta and session low. Pattern reference for `OnTick` polling. |
| **DS1Mod.DiscordRPC** | `IGameMod` | Shows current activity, deaths, last boss killed, and session time in Discord Rich Presence. Requires a Discord application with `ds1_bonfire` / `ds1_skull` art assets registered. |
| **DS1Mod.AsylumSlam** | `IGamePatcher` | Locks Asylum Demon to slam attacks only. Patches `m18_01_00_00.luabnd.dcx` at launch with precompiled Lua 5.0 bytecode. |
| **DS1Mod.GoofyDemon** | `IGamePatcher` + `IGameMod` | Asylum Demon gets 10 random moods, an on-screen HUD showing the current mood, a console readout, and a `*farts*` message on landing. Patches luabnd + EMEVD + FMG at launch. See [`DS1Mod.GoofyDemon/README.md`](DS1Mod.GoofyDemon/README.md). |

> `DS1Mod.AsylumSlam` and `DS1Mod.GoofyDemon` both patch the same Asylum Demon
> AI — do not load both at the same time. GoofyDemon supersedes AsylumSlam and
> includes the fart entrance that was previously a separate mod.
