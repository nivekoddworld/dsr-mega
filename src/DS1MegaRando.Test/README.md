# DS1MegaRando.Test

Regression harness for the fog gate randomizer and boss combo validator.

Targets `net9.0-windows` (uses newer APIs); all other projects target `net8.0-windows`.

## What it tests

- **500-seed fog gate sweep** — runs `FogGateRandomizer` across 500 seeds and
  verifies that `SoftlockChecker` finds a valid path through the game for every
  seed. Any seed that fails the softlock check is reported.
- **Boss combo validation** — exercises `boss_overrides.json` blocked/pinned
  entries to confirm no disallowed replacement slips through.

## Running

```sh
dotnet test DS1MegaRando.Test
# or
dotnet run --project DS1MegaRando.Test
```

Note: `Program.cs` contains a hardcoded game path used for integration tests
that require real game files. These tests are skipped automatically when the
path does not exist.
