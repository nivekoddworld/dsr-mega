# DS1Mod.HpLogger

Polls the player's current HP on each tick (~twice a second) and logs every
change, including the delta and the session's lowest HP recorded.

Output goes to the console and `<game>/mods/HpLogger.log`.

## Pattern

This is the reference example for the **OnTick polling** pattern: read a
current value, compare it to the last seen value, and react to the delta.
For event-driven reactions (a boss dying, the player dying) use the
edge-triggered hooks on `IGameHooks` instead — they fire exactly once per
occurrence with no polling overhead.

## Install

Copy `DS1Mod.HpLogger.dll` into `<game>/mods/`. No other DLLs are needed
beyond what the host already provides.

## Revert

Delete `DS1Mod.HpLogger.dll` from `mods/`. The mod writes no game files.
