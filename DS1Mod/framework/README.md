# DS1Mod — Framework

The runtime infrastructure that ships alongside the randomizer. Build with
`DS1Mod.Framework.slnx` (from the `DS1Mod/` directory).

| Project | Language | Purpose |
|---|---|---|
| **DS1Mod.Injector** | C++ | `dinput8.dll` proxy — applies heap fix, bootstraps the .NET runtime via `hostfxr`, D3D11 Present hook for ImGui |
| **DS1Mod.ImGuiNative** | C++ | Builds `cimgui.dll` — the C wrapper ImGui.NET expects; vendored Dear ImGui sources |
| **DS1Mod.Host** | C# | Scans `mods/`, loads each DLL in its own `AssemblyLoadContext`, runs the 500 ms event pump, drives `IGuiMod.OnGui()` |
| **DS1Mod.Core** | C# | In-process memory access, AOB pointer resolution, event hooks, game reader/writer; defines `IGuiMod` and the `DS1ImGui` wrapper |
| **DS1Mod.SDK** | C# | `ModBase` abstract class — the only dependency most mods need to reference |
| **DS1Mod.Rendering** | C# | Bridges the C++ Present hook and managed `IGuiMod` mods; `ImGuiRenderer` + `NativeD3DHook` P/Invoke |
| **DS1Mod.Modding** | C# | Helper library for game-data patcher mods — wraps SoulsFormats into idempotent PARAM/FMG/EMEVD/Lua edits |

`DS1Mod.Injector` and `DS1Mod.ImGuiNative` are built separately via MSBuild (MSVC required); see `build.bat`.
