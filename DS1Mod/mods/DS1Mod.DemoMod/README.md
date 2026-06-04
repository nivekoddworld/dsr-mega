# DS1Mod.DemoMod

A fully-worked example mod that exercises every surface of the DS1Mod SDK.
Use it as a template or a reference when building your own mod.

## What it covers

| Surface | What it does |
|---|---|
| `IGamePatcher.Patch()` | Scans the game dir, backs up a file before any map loads |
| `IGameHooks.BossKilled` | Prints the boss name and a running kill count |
| `IGameHooks.FogGateEntered` | Prints the map ID at the moment of crossing |
| `IGameHooks.PlayerDied` | Prints a death message with the player's position |
| `IGameHooks.PlayerLeveledUp` | Prints new soul level |
| `IGameReader` | Reads HP, stamina, position, map ID, soul level, souls, event flags |
| `IGameWriter` | Round-trips an event flag: read → write → verify |
| `OnTick` | Periodic state snapshot (runs ~twice a second) |
| `OnUnload` | Session summary on shutdown |

Output goes to `<game>/mods/DemoMod.log` and stdout.

## Install

Build and copy both DLLs into `<game>/mods/`:

```
DS1Mod.DemoMod.dll
SoulsFormats.dll   (only if your mod uses SoulsFormats directly)
```

`DS1Mod.Core.dll` and `DS1Mod.SDK.dll` are provided by the host — do not copy them.

Launch via **▾ → Launch with Mod Framework** in the randomizer, or the **MODS** tab → **Install Mod…**.
