#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "modloader.h"

void ApplyHeapFix(); // defined in heapfix.cpp

static DWORD WINAPI WorkerThread(LPVOID)
{
    // Wait for Steam DRM to finish unpacking before touching memory.
    Sleep(1000);

    ApplyHeapFix();

    // Determine game directory from the process executable path.
    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);

    // Strip the filename, keep the directory.
    wchar_t* lastSlash = wcsrchr(exePath, L'\\');
    if (lastSlash) *lastSlash = L'\0';

    InitModLoader(exePath);

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
