// ds1mod_imgui_backends.cpp
// Thin C wrappers around imgui_impl_dx11 and imgui_impl_win32.
// These are exported from cimgui.dll so that d3d_hook.cpp can load them
// via GetProcAddress at runtime — keeping the injector dependency-free.

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "imgui/imgui.h"
#include "imgui/backends/imgui_impl_dx11.h"
#include "imgui/backends/imgui_impl_win32.h"
#include "ds1mod_imgui_backends.h"

extern "C"
{
    bool DS1Mod_ImplDX11_Init(void* device, void* deviceContext)
    {
        return ImGui_ImplDX11_Init(
            static_cast<ID3D11Device*>(device),
            static_cast<ID3D11DeviceContext*>(deviceContext));
    }

    void DS1Mod_ImplDX11_Shutdown()
    {
        ImGui_ImplDX11_Shutdown();
    }

    void DS1Mod_ImplDX11_NewFrame()
    {
        ImGui_ImplDX11_NewFrame();
    }

    void DS1Mod_ImplDX11_RenderDrawData(void* drawData)
    {
        ImGui_ImplDX11_RenderDrawData(static_cast<ImDrawData*>(drawData));
    }

    bool DS1Mod_ImplWin32_Init(void* hwnd)
    {
        return ImGui_ImplWin32_Init(hwnd);
    }

    void DS1Mod_ImplWin32_Shutdown()
    {
        ImGui_ImplWin32_Shutdown();
    }

    void DS1Mod_ImplWin32_NewFrame()
    {
        ImGui_ImplWin32_NewFrame();
    }

    bool DS1Mod_ImplWin32_WndProcHandler(
        void* hwnd, unsigned int msg, unsigned __int64 wParam, __int64 lParam)
    {
        // The imgui_impl_win32 handler has the same signature as WndProc but
        // returns an LRESULT, where non-zero means "consumed".
        extern LRESULT ImGui_ImplWin32_WndProcHandler(HWND, UINT, WPARAM, LPARAM);
        return ImGui_ImplWin32_WndProcHandler(
            static_cast<HWND>(hwnd),
            static_cast<UINT>(msg),
            static_cast<WPARAM>(wParam),
            static_cast<LPARAM>(lParam)) != 0;
    }
}
