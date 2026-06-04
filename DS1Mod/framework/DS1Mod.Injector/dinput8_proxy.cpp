#define WIN32_LEAN_AND_MEAN
#define DIRECTINPUT_VERSION 0x0800
#include <windows.h>
#include <dinput.h>

using DirectInput8Create_t = HRESULT(WINAPI*)(HINSTANCE, DWORD, REFIID, LPVOID*, LPUNKNOWN);
static DirectInput8Create_t g_realCreate = nullptr;

static void EnsureRealDInput8() {
    if (g_realCreate) return;
    wchar_t sysPath[MAX_PATH];
    GetSystemDirectoryW(sysPath, MAX_PATH);
    wcscat_s(sysPath, MAX_PATH, L"\\dinput8.dll");
    HMODULE real = LoadLibraryW(sysPath);
    if (real)
        g_realCreate = reinterpret_cast<DirectInput8Create_t>(GetProcAddress(real, "DirectInput8Create"));
}

extern "C" HRESULT WINAPI DirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID* ppvOut, LPUNKNOWN punkOuter) {
    EnsureRealDInput8();
    if (g_realCreate)
        return g_realCreate(hinst, dwVersion, riidltf, ppvOut, punkOuter);
    return E_FAIL;
}