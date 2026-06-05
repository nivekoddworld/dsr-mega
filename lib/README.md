# lib

Third-party libraries used by the randomizer and mod framework.

| Library | Purpose |
|---|---|
| **SoulsFormats** | Binary format library for DSR files: BND3, PARAM, MSB1, EMEVD, DCX, ESD, … |
| **SoulsIds** | Game ID utilities and event scripting helpers |

Both are included as `ProjectReference` entries (not NuGet packages) so they
build from source alongside the main solution.
