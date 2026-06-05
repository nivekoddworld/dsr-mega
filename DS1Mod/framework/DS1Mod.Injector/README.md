# DS1Mod.Injector

C++ DLL (`dinput8.dll`) that sideloads into Dark Souls Remastered at startup
via DirectInput proxy exports.

## What it does

1. **Heap fix** (`heapfix.cpp`) — patches DSR's allocator to handle large heaps
   without crashing. Applied before any game code runs.
2. **Mod loader** (`modloader.cpp`) — locates `hostfxr.dll`, initializes the
   .NET 8 runtime in-process, and calls into `DS1Mod.Host.dll` to start the mod
   lifecycle manager.

## Build

Requires MSVC (Visual Studio 2022 or Build Tools). The full build is automated
by `build.bat` at the repo root, which invokes MSBuild on this project and
copies the output DLL into the publish directory.

```bat
msbuild DS1Mod/DS1Mod.Injector/DS1Mod.Injector.vcxproj /p:Configuration=Release /p:Platform=x64
```

## Install

Place the compiled `dinput8.dll` next to `DarkSoulsRemastered.exe`. DSR loads
it automatically on startup via the DirectInput proxy chain.

Pre-built binaries are in `x64/Release/`. The `build.bat` build script
assembles the full distributable in `publish/`.

## Files

| File | Purpose |
|---|---|
| `dllmain.cpp` | DLL entry point — calls `ApplyHeapFix` + `InitModLoader` |
| `heapfix.cpp/h` | Heap allocator patch |
| `modloader.cpp/h` | hostfxr bootstrap and .NET runtime initialization |
| `dinput8_proxy.cpp/h` | DirectInput8 proxy exports so the real dinput8 still works |
| `exports.def` | DLL export list for the DirectInput proxy |
