/**
 * dinput8_proxy.cpp — forwards DirectInput8Create to the real system DLL
 *
 * Windows loads dinput8.dll from the game directory first (DLL sideloading).
 * We must still export DirectInput8Create so the game's input system works.
 * This module loads the real system copy and delegates all calls to it.
 *
 * The real DLL is loaded lazily on the first DirectInput8Create call rather
 * than in DllMain, because calling LoadLibrary from DllMain while the loader
 * lock is held can deadlock.  By the time the game calls DirectInput8Create
 * the loader lock has been released and it is safe to load more DLLs.
 */

#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#include <windows.h>
#include <dinput.h>

using DirectInput8Create_t = HRESULT(WINAPI*)(
    HINSTANCE, DWORD, REFIID, LPVOID*, LPUNKNOWN);

static DirectInput8Create_t g_realCreate = nullptr;

static void EnsureRealDInput8()
{
    if (g_realCreate) return;

    wchar_t sysPath[MAX_PATH];
    GetSystemDirectoryW(sysPath, MAX_PATH);
    wcscat_s(sysPath, MAX_PATH, L"\\dinput8.dll");

    HMODULE real = LoadLibraryW(sysPath);
    if (real)
        g_realCreate = reinterpret_cast<DirectInput8Create_t>(
            GetProcAddress(real, "DirectInput8Create"));
}

extern "C" HRESULT WINAPI DirectInput8Create(
    HINSTANCE hinst, DWORD dwVersion,
    REFIID riidltf, LPVOID* ppvOut, LPUNKNOWN punkOuter)
{
    EnsureRealDInput8();
    if (g_realCreate)
        return g_realCreate(hinst, dwVersion, riidltf, ppvOut, punkOuter);
    return E_FAIL;
}
