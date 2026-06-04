#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "modloader.h"

void ApplyHeapFix();

static DWORD WINAPI WorkerThread(LPVOID)
{
    // Open log immediately so any crash during Sleep or HeapFix is visible.
    LogInit();
    Log(L"WorkerThread started");

    Sleep(1000);
    Log(L"Sleep done, applying heap fix...");

    ApplyHeapFix();
    Log(L"Heap fix applied");

    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);
    wchar_t* lastSlash = wcsrchr(exePath, L'\\');
    if (lastSlash) *lastSlash = L'\0';

    Log(L"Calling InitModLoader...");
    bool ok = InitModLoader(exePath);
    Log(ok ? L"InitModLoader succeeded" : L"InitModLoader FAILED");

    return 0;
}

BOOL WINAPI DllMain(HMODULE hMod, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hMod);
        CreateThread(nullptr, 0, WorkerThread, nullptr, 0, nullptr);
    }
    return TRUE;
}
