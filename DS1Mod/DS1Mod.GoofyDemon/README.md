# DS1Mod.GoofyDemon

The Asylum Demon has given up on being a boss. **One mod, everything:**

- **Goofy AI** — 10 random "moods" (swaps `script/m18_01_00_00.luabnd.dcx`).
- **On-screen mood HUD** — the current mood pops up on the game HUD.
- **Console readout** — the same mood, also printed to the mod console + log.
- **Fart entrance** — a big `*farts*` message the instant he lands his intro.

## Moods (longer-lasting)

| # | Roll | Mood (console) | HUD text | What he does |
|---|---|---|---|---|
| 0 | 1–18  | 💃 The Shimmy | the demon shimmies | side-steps like he's at a wedding |
| 1 | 19–34 | 🕺 The Breakdance | the demon breakdances | spin-steps in place |
| 2 | 35–48 | 😱 The Coward | the demon flees in terror | sprints to the far wall |
| 3 | 49–60 | 🤔 Existential Crisis | the demon questions its existence | stands, turns, stands |
| 4 | 61–66 | 😈 Surprise! | SURPRISE ATTACK | leap slam, then flees |
| 5 | 67–72 | 👊 Fine. Fight. | the demon remembers it is a boss | a real butt slam |
| 6 | 73–82 | 🌀 Zoomies | the demon has the zoomies | frantic shimmy/spin combo |
| 7 | 83–90 | 🦵 Hokey Pokey | the demon does the hokey pokey | in, out, in, shake it about |
| 8 | 91–95 | 😶 Stage Fright | the demon has stage fright | freezes, embarrassed turn |
| 9 | 96–100| 🏆 Victory Lap | premature victory lap | circles you celebrating |

Each mood now ends with a 2–3.5 s dwell, so a quick action (a flee, a single
spin) can't finish instantly and snap to the next mood.

## How the mood HUD works

The AI can't draw text itself, so each cycle it broadcasts its mood over event
flags `11817000..09` (clears all ten, sets one). Three readers consume that:

1. **EMEVD** — `Patch()` adds 10 new events (`11819000..09`) to
   `event/m18_01_00_00.emevd.dcx` and registers them in the map constructor
   (event 0). Each waits for its flag, shows its message, waits for the flag to
   clear. Mood text is added to the `Event_text` FMG in every
   `msg/<lang>/menu.msgbnd.dcx`.
2. **Console / log** — `OnTick()` polls the flags and prints to the console and
   `mods/GoofyDemon.log`.

> The mood events are registered at the **top** of the map constructor
> (event 0), before its multiplayer `SKIP`/`END IF` control flow — otherwise
> registrations appended at the end can be skipped and never run. The patcher
> self-heals older installs that registered them at the end.

The fart works the same way: a `Display Message` inserted right after the
`9060` landing animation in the entrance event (`11810310`), with `*farts*`
added to the FMG.

All edits (luabnd, emevd, every menu.msgbnd) are surgical, **idempotent**, and
backed up via SoulsFormats.

## Install

`dotnet build -c Release`, then copy both DLLs into `<game>/mods/`:

- `DS1Mod.GoofyDemon.dll`
- `SoulsFormats.dll`

(The old standalone `FartEntrance` mod is **folded in here** — don't run both.)

## Revert

Remove the DLLs and restore the `.bak` files next to
`m18_01_00_00.luabnd.dcx`, `m18_01_00_00.emevd.dcx`, and each `menu.msgbnd.dcx`.

## Status

Built and verified on Linux against the real game files: the AI compiles and
round-trips, and `Patch()` applies the luabnd swap + fart + 10 mood events +
all FMG text, re-decompiles cleanly, and is idempotent (running twice leaves 55
events and a single fart). **Not yet tested in a live game.** Output logs to
`<game>/mods/GoofyDemon.log`.
