# reference

Legacy and archived codebases kept for historical context and as ground-truth
data sources. Nothing here is called by the active C# application except where
noted.

| Folder | Language | Status | Notes |
|---|---|---|---|
| **FogMod-master** | C# (WinForms) | Active dependency | `dist/fog.txt` is ground truth for boss EntityIDs. `dist/DS1R/event/` EMEVD patches are loaded at runtime by `FogGateWriter`. |
| **Dark-Souls-Enemy-Randomizer-master** | Python | Reference only | `method_names.py` maps EMEVD instruction names to Bank/ID pairs. `eventscripts/Remastered/` contains the original EMEVD patches. |
| **DarkSoulsItemRandomizer-master** | Python | Reference only | Original item randomizer logic; used as a reference when porting to C#. |
| **DS1Randomizer** | C# (Avalonia) | Archived | Prototype of an Avalonia-based cross-platform UI. Not part of the active solution. |
| **DS1RandomizerExports.sln** | — | Archived | Visual Studio solution for the DS1Randomizer prototype. |
