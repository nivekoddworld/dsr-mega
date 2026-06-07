#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "modloader.h"

void ApplyHeapFix();

static DWORD g_mainThreadId = 0;

static DWORD WINAPI WorkerThread(LPVOID)
{
    Log(L"WorkerThread started");

    Sleep(1000);
    Log(L"Sleep done, applying heap fix...");

    ApplyHeapFix();
    Log(L"Heap fix applied");

    // Suspend the main thread while patches are written to disk.
    // This prevents the game from reading files (params, FMGs, maps) before
    // the mod patcher has finished writing them. The main thread is resumed
    // immediately after InitModLoader returns.
    HANDLE hMain = OpenThread(THREAD_SUSPEND_RESUME, FALSE, g_mainThreadId);
    if (hMain)
    {
        SuspendThread(hMain);
        Log(L"Main thread suspended — patching...");
    }
    else
    {
        Log(L"WARNING: Could not open main thread handle — patches may race with game load");
    }

    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);
    wchar_t* lastSlash = wcsrchr(exePath, L'\\');
    if (lastSlash) *lastSlash = L'\0';

    Log(L"Calling InitModLoader...");
    bool ok = InitModLoader(exePath);
    Log(ok ? L"InitModLoader succeeded" : L"InitModLoader FAILED");

    if (hMain)
    {
        ResumeThread(hMain);
        CloseHandle(hMain);
        Log(L"Main thread resumed — game loading proceeds");
    }

    return 0;
}

BOOL WINAPI DllMain(HMODULE hMod, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        // DllMain is called on the main thread — capture its ID so WorkerThread
        // can suspend it during the patch phase.
        g_mainThreadId = GetCurrentThreadId();
        DisableThreadLibraryCalls(hMod);

        // Open log before anything else so DisableArxan() can write to it.
        LogInit();

        // Install the dearxan entry point hook NOW, before the main thread
        // leaves DllMain and runs the game's entry point. This is the only
        // window where we can intercept it. The hook fires asynchronously once
        // Arxan's own stubs finish; InitModLoader waits on g_arxanReady before
        // loading managed mods.
        DisableArxan();

        CreateThread(nullptr, 0, WorkerThread, nullptr, 0, nullptr);
    }
    return TRUE;
}
