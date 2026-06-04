# DS1Mod — Framework

The runtime infrastructure that ships alongside the randomizer. Build with
`DS1Mod.Framework.slnx` (from the `DS1Mod/` directory).

| Project | Language | Purpose |
|---|---|---|
| **DS1Mod.Injector** | C++ | `dinput8.dll` proxy — applies heap fix and bootstraps the .NET runtime via `hostfxr` |
| **DS1Mod.Host** | C# | Scans `mods/`, loads each DLL in its own `AssemblyLoadContext`, runs the 500 ms event pump |
| **DS1Mod.Core** | C# | In-process memory access, AOB pointer resolution, event hooks, game reader/writer |
| **DS1Mod.SDK** | C# | `ModBase` abstract class — the only dependency most mods need to reference |

`DS1Mod.Injector` is built separately via MSBuild (MSVC required); see `build.bat`.
