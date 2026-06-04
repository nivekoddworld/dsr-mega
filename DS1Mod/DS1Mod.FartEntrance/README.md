# DS1Mod.FartEntrance

The Asylum Demon **farts** the instant he lands his entrance. A big `*farts*`
message pops up on screen the moment he touches down.

## How it works

The entrance is event `11810310` in `event/m18_01_00_00.emevd.dcx`. Its
jump-down animation (`Force Animation Playback(9060)`) has
*Wait-For-Completion* set, so the event pauses there until he's fully landed.
At launch the mod makes two surgical, idempotent, backed-up edits with
SoulsFormats:

1. **EMEVD** — inserts `Display Message(6900690)` immediately after the `9060`
   landing, so it fires the exact frame he touches down.
2. **FMG** — adds the line `*farts*` at message id `6900690` to the
   `Event_text` FMG inside **every** `msg/<lang>/menu.msgbnd.dcx`, so the
   message has text in any language.

Banners (`Display Banner`) can only show fixed presets ("Victory Achieved",
etc.), so the custom text uses `Display Message`, a centered announcement.

## Install

`dotnet build -c Release`, then copy both DLLs into `<game>/mods/`:

- `DS1Mod.FartEntrance.dll`
- `SoulsFormats.dll`

It edits `event/` and `msg/` files (not the AI `luabnd`), so it's fully
compatible with the AI mods (`GoofyDemon`, `AsylumSlam`) — run them together.

## Revert

Remove the DLL and restore the `.bak` files the mod created next to
`m18_01_00_00.emevd.dcx` and each `menu.msgbnd.dcx`.

## Status

Built and verified on Linux against the real game files: both edits apply,
re-decompile cleanly, and re-running is idempotent (the message is not inserted
twice). **Not yet tested in a live game.** If the entrance misbehaves, restore
the backups. Output is logged to `<game>/mods/FartEntrance.log`.
