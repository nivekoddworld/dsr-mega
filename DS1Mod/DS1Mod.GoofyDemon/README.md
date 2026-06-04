# DS1Mod.GoofyDemon

The Asylum Demon (entity 223200) has given up on being a boss. Every time it
picks an action it re-rolls its **mood** (now 10 of them):

| # | Roll | Mood | What it does |
|---|---|---|---|
| 0 | 1–18  | 💃 The Shimmy | side-steps back and forth like it's at a wedding |
| 1 | 19–34 | 🕺 The Breakdance | spin-steps in place, repeatedly, for no reason |
| 2 | 35–48 | 😱 The Coward | sprints to the far wall in abject terror |
| 3 | 49–60 | 🤔 Existential Crisis | stands still, slowly turns, stands still again |
| 4 | 61–66 | 😈 Surprise! | a sudden flying body slam (3007) — then immediately flees |
| 5 | 67–72 | 👊 Fine. Fight. | the rare butt slam (3008) where it acts like a boss |
| 6 | 73–82 | 🌀 The Zoomies | frantic shimmy/spin combo, like a cat at 3am |
| 7 | 83–90 | 🦵 Hokey Pokey | step in, step back, step in, shake it all about |
| 8 | 91–95 | 😶 Stage Fright | freezes (forgot its lines), then a tiny embarrassed turn |
| 9 | 96–100| 🏆 Victory Lap | celebrates prematurely, running a circle around you |

It also `return false`s on interrupt, so it keeps dancing even while being hit.

## Mood readout (console)

The enemy AI can't draw player-facing text on its own, so each cycle the demon
**broadcasts its mood** over an unused event-flag block (`11817000..11817009`,
m18_01 local flags in a high unused range): it clears all ten
and sets exactly one. The mod's `OnTick()` polls that block and writes the live
mood to BOTH the console window and a log file (`<ModsDir>/GoofyDemon.log`), e.g.:

```
[GoofyDemon] mood → 🌀 The Zoomies
[GoofyDemon] mood → 😱 The Coward (fleeing)
```

`OnUnload()` clears the flags so they don't linger in your save.

> ⚠️ Don't run alongside FogMod's region-flag remapping — it reuses the same
> flag base.

## In-game HUD pop-up — not wired yet

Showing the mood as text *over the fight* needs an EMEVD event + an FMG string
(the AI sets the flag → an event watches it → `display_*` draws the text). That
part isn't built: it requires the game's `event/` and `msg/` files, which
weren't available in the build environment, and editing them blind risks
breaking the Undead Asylum. See the repo chat / `ds1_ai_mods/` for how to
finish it against real files.

## How the patch works

`IGamePatcher.Patch()` backs up and swaps the `223200_battle.lua` entry inside
`script/m18_01_00_00.luabnd.dcx` for the embedded precompiled Lua 5.0 bytecode
(`223200_battle.luac`), repacking via SoulsFormats. Every sub-goal uses a call
signature taken verbatim from the vanilla Asylum/Stray Demon AI, so the
animations exist on this model.

## Install

`dotnet build -c Release`, then copy both DLLs into `<game>/mods/`:

- `DS1Mod.GoofyDemon.dll`
- `SoulsFormats.dll`

Don't run this and `DS1Mod.AsylumSlam` together — they patch the same file.

## Revert

Remove the DLLs and restore `m18_01_00_00.luabnd.dcx` from the `.bak`.

## Status

Built and unit-exercised on Linux: compiles, round-trips, `Patch()` backs up +
swaps + repacks, and the mood readout was simulated end-to-end (correct mood
per flag, de-dupes repeats, clears on unload). **Not tested in a live game.**

## Don't see any output?

All messages also go to `<game>/mods/GoofyDemon.log`. Check there first.

- **No log file at all** → the mod didn't load. Confirm `DS1Mod.GoofyDemon.dll`
  **and** `SoulsFormats.dll` are in `<game>/mods/`, and that you launched via the
  mod framework (not a plain game launch).
- **Log shows `Flag self-test ... MISMATCH`** → the chosen flag IDs aren't
  readable on your build; tell me and I'll pick another range.
- **Heartbeats but never a `mood →`** → the AI isn't setting the flags: either
  the `luabnd` wasn't patched (check the `Patch:` line) or you're not in the
  Asylum Demon fight yet.
- **No heartbeats** → `OnTick` isn't running — you're not in a loaded map, or
  the mod isn't loaded.
