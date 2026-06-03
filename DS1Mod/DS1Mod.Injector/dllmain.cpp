/**
 * DS1Mod.Injector — dinput8.dll proxy for Dark Souls Remastered
 *
 * Drop this file as dinput8.dll in the DSR game directory.
 * On game start Windows loads it before the system dinput8.dll.
 *
 * What it does:
 *   1. Forwards DirectInput8Create to the real system dinput8.dll
 *   2. Spawns a worker thread that applies the FFX + Lua heap fix before
 *      the game allocates those static pools (prevents crash when
 *      BossSfxMerger enlarges FRPG_SfxBnd_CommonEffects.ffxbnd.dcx)
 *
 * Build: Visual Studio 2022, x64 Release, /MT runtime, exports dinput8.def
 * Requires: Windows SDK (dinput8.h)
 */

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "heapfix.h"

static DWORD WINAPI WorkerThread(LPVOID)
{
    // Give the game's module loader a moment to finish mapping sections
    // before we scan for heap-size constants.
    Sleep(100);
    ApplyHeapFix();
    return 0;
}

BOOL WINAPI DllMain(HMODULE hMod, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hMod);
        // No LoadLibrary here — loader lock is held during DllMain.
        // The real dinput8.dll is loaded lazily in DirectInput8Create.
        CreateThread(nullptr, 0, WorkerThread, nullptr, 0, nullptr);
    }
    return TRUE;
}
