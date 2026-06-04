# DS1Mod.GoofyDemon

The Asylum Demon (entity 223200) has given up on being a boss. Every time it
picks an action it re-rolls its **mood**:

| Roll | Mood | What it does |
|---|---|---|
| 1–30  | The Shimmy | side-steps back and forth like it's at a wedding |
| 31–50 | The Breakdance | spin-steps in place, repeatedly, for no reason |
| 51–68 | The Coward | sprints to the far wall in abject terror |
| 69–84 | Existential Crisis | stands still, slowly turns, stands still again |
| 85–93 | Surprise! | a sudden flying body slam (3007) — then immediately flees |
| 94–100| Fine. Fight. | the rare butt slam (3008) where it acts like a boss |

It also `return false`s on interrupt, so it keeps dancing even while being hit.

## How it works

Identical mechanism to [`../DS1Mod.AsylumSlam`](../DS1Mod.AsylumSlam): an
`IGamePatcher` that at launch backs up and swaps the `223200_battle.lua` entry
in `script/m18_01_00_00.luabnd.dcx` for the embedded slam-only... er,
*dance-mostly* AI, repacking via SoulsFormats. The replacement is precompiled
Lua 5.0 bytecode (`223200_battle.luac`); every sub-goal uses a call signature
taken verbatim from the vanilla Asylum/Stray Demon AI, so the animations are
guaranteed to exist on this model.

## Install

`dotnet build -c Release`, then copy both DLLs into `<game>/mods/`:

- `DS1Mod.GoofyDemon.dll`
- `SoulsFormats.dll`

Don't run this and `DS1Mod.AsylumSlam` at the same time — they patch the same
file; whichever patches last wins.

## Revert

Remove the DLLs and restore `m18_01_00_00.luabnd.dcx` from the `.bak`.

## Status

Built and unit-exercised on Linux (compiles, round-trips, `Patch()` backs
up + swaps + repacks, backup confirmed vanilla). **Not tested in a live game.**
If the boss misbehaves, restore the backup.
