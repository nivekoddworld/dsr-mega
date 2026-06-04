# event_tools

Tools and definitions for working with DSR EMEVD event scripts.

## Contents

| Path | Purpose |
|---|---|
| `ds1emedf.json` | DS1 EMEDF — maps every EMEVD instruction to its Bank/ID, argument names, types, and enum labels. Used by the decompiler and useful for cross-referencing Bank/ID pairs when writing EMEVD patches. |
| `emevd_decompile/` | .NET tool that reads binary `.emevd.dcx` files and renders them as human-readable text using the EMEDF. Output goes to `../decompiled_emevd/`. |

## Running the decompiler

```sh
dotnet run --project event_tools/emevd_decompile -- \
    event_tools/ds1emedf.json \
    DSR_Event_Folder/event \
    decompiled_emevd
```

This regenerates all `.evd.txt` files in `decompiled_emevd/`. The output is
read-only reference — edits to events go through SoulsFormats on the binary
(see `DS1MegaRando.Enemies/BossEmevdPatcher.cs` and the mod patchers).

## EMEDF quick reference

The EMEDF is also the source of truth for instruction Bank/ID pairs used in
`DS1MegaRando.Data/Enemies/BossIds.cs` (EMEVD patches) and the mod framework
patchers. Key entries for the randomizer:

| Instruction | Bank | ID |
|---|---|---|
| `ForceAnimationPlayback` | 2003 | 18 |
| `WarpCharacter` | 2004 | 41 |
| `SetImmortality` | 2004 | 12 |
| `CreateMultipartNpc` | 2004 | 22 |
| `DisplayBattleBanner` (boss name/HP bar) | 2003 | 95 |
