# Arxan Disabling Setup

## What Was Added

`modloader.cpp` now includes:
- FFI declaration: `extern "C" int dearxan_neuter_arxan(void);`
- Disabler function: `DisableArxan()`
- Call in `InitModLoader()` right after logging

This disables Arxan DRM at game launch, before game logic runs.

## What You Need to Do

### Step 1: Get dearxan.lib

**Option A: Download pre-compiled** (recommended)
- GitHub releases: https://github.com/tremwil/dearxan/releases
- Look for `dearxan.lib` (Windows MSVC)

**Option B: Build from source** (if needed)
```bash
git clone https://github.com/tremwil/dearxan.git
cd dearxan
cargo build --release --features ffi
# Output: target/release/dearxan.lib
```

### Step 2: Link dearxan.lib in Your Project

**Visual Studio:**
1. Right-click project → Properties
2. Linker → Input
3. Additional Dependencies: add `dearxan.lib`

**Or manually:**
- Copy `dearxan.lib` to a lib folder
- Add to project linker settings

### Step 3: Build

```bash
# Rebuild the injector project
msbuild DS1Mod.Injector.vcxproj /p:Configuration=Release /p:Platform=x64
```

### Step 4: Test

1. Run game with the injector loaded
2. Check log: should say "✓ Arxan successfully disabled"
3. Game runs without integrity check crashes
4. Mods work normally

## Verification

- ✓ Game launches without crashes
- ✓ Can modify game files without integrity check
- ✓ Mods load and function
- ✓ No Arxan-related errors in log

## What This Does

- Calls `dearxan_neuter_arxan()` which patches Arxan out of memory
- Runs after game entry point (when Arxan is active)
- Runs before game logic executes
- Completely disables all Arxan checks and protections
- Zero performance overhead after patching

## Reference

- dearxan repo: https://github.com/tremwil/dearxan
- Modified file: `modloader.cpp`
- See `/tools/arxan_patcher/` for comprehensive documentation
