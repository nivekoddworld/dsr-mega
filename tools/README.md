# tools

Developer tools for working with DSR game files.

| Folder / file | Purpose |
|---|---|
| **event_tools/** | EMEVD decompiler + DS1 EMEDF definition. See `event_tools/README.md`. |
| **ds1_ai_mods/** | Hand-written Lua AI scripts + full Linux toolchain to compile and repack them. See `ds1_ai_mods/README.md`. |
| **luac50** | Lua 5.0.3 compiler, Linux x86_64. Used by `DS1Mod.Modding.AiBuilder` when iterating on a Linux dev host. |
| **luac50.exe** | Lua 5.0.3 compiler, Windows x86_64 (static, stripped). Staged into `publish\framework\tools\` by `build.bat` and copied into `<gameDir>\tools\` by the UI's "Launch with Mod Framework" button so `AiBuilder.EditAi` has a compiler at mod-load time. Cross-built from upstream `lua-5.0.3` via `x86_64-w64-mingw32-gcc`. |
