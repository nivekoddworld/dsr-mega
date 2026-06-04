#define WIN32_LEAN_AND_MEAN
#include <windows.h>

void ApplyHeapFix(); // Forward declaration

static DWORD WINAPI WorkerThread(LPVOID) {
    // Increase sleep to ensure Steam DRM is fully finished unpacking
    Sleep(1000);
    ApplyHeapFix();
    return 0;
}

BOOL WINAPI DllMain(HMODULE hMod, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hMod);
        CreateThread(nullptr, 0, WorkerThread, nullptr, 0, nullptr);
    }
    return TRUE;
}