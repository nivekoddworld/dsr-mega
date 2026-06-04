# DS1Mod.FogLogger

Logs every fog wall the player passes through, with a running count, the
player's position, and soul level at the moment of crossing.

Detection is **animation-based** — it watches for the "walking through fog"
animation rather than checking event flags. This means it catches every fog
wall in the game, not just the boss fogs that set flags.

Output goes to the console and `<game>/mods/FogLogger.log`.

## Pattern

This mod demonstrates the `OnTick` polling pattern for detecting an animation
state change. For one-shot reactions to boss kills, deaths, or level-ups, use
the edge-triggered hooks on `IGameHooks` instead.

## Install

Copy `DS1Mod.FogLogger.dll` into `<game>/mods/`. `DS1Mod.Core.dll` and
`DS1Mod.SDK.dll` are provided by the host.

## Revert

Delete `DS1Mod.FogLogger.dll` from `mods/`. The mod writes no game files,
so no backup restoration is needed.
