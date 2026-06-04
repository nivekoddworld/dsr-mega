#pragma once
// C-style exports for the D3D11 and Win32 backends.
// These are the functions that d3d_hook.cpp loads at runtime via GetProcAddress.

#ifdef __cplusplus
extern "C" {
#endif

// ── D3D11 backend ─────────────────────────────────────────────────────────────

// device   : ID3D11Device*
// devCtx   : ID3D11DeviceContext*
__declspec(dllexport) bool DS1Mod_ImplDX11_Init(void* device, void* deviceContext);
__declspec(dllexport) void DS1Mod_ImplDX11_Shutdown();
__declspec(dllexport) void DS1Mod_ImplDX11_NewFrame();

// drawData : ImDrawData* (returned by igGetDrawData())
__declspec(dllexport) void DS1Mod_ImplDX11_RenderDrawData(void* drawData);

// ── Win32 backend ─────────────────────────────────────────────────────────────

// hwnd : HWND of the game window
__declspec(dllexport) bool DS1Mod_ImplWin32_Init(void* hwnd);
__declspec(dllexport) void DS1Mod_ImplWin32_Shutdown();
__declspec(dllexport) void DS1Mod_ImplWin32_NewFrame();

// Routes Windows messages to ImGui.  Call from a WndProc hook.
// Return true if ImGui consumed the message.
__declspec(dllexport) bool DS1Mod_ImplWin32_WndProcHandler(
    void* hwnd, unsigned int msg, unsigned __int64 wParam, __int64 lParam);

#ifdef __cplusplus
}
#endif
