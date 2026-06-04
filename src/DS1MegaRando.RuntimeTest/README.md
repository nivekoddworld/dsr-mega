# _rttest — Runtime ESD Test Harness

Validates that the replacement bonfire ESD in `DS1MegaRando.Data/ESDs/` behaves
correctly by comparing it against the vanilla bonfire ESD.

## Contents

| File | Purpose |
|---|---|
| `rttest.csproj` | .NET 8 console app that loads both ESDs via SoulsFormats and diffs their state machine structure |
| `van_bonfire.esd` | Vanilla bonfire ESD (extracted from an unmodified DSR install) |
| `fog_bonfire.esd` | Replacement ESD used by the randomizer |
| `chk.py` | Python helper script for quick manual diffing of ESD output |
| `_out/` | Generated diff output from the last run |

## Running

```sh
dotnet run --project _rttest/rttest.csproj
```

Pass if the console reports no unexpected state differences. The replacement ESD
must preserve all vanilla transitions except the Firekeeper-availability check,
which is unconditionally enabled.
