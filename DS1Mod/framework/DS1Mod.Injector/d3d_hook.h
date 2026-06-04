#pragma once
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

// Signature of the managed OnImGuiFrame callback.
// Called every frame from the Present hook, on the render thread.
// The CLR delegate thunk handles thread attachment automatically.
using OnGuiCallbackFn = void(__cdecl*)();

namespace D3DHook
{
    // Call once after the managed runtime is initialised.
    // Creates a throw-away D3D11 device + swap chain, reads the DXGI
    // vtable, and overwrites the Present slot with our hook.
    // Returns false if D3D11 is not available or the hook fails.
    bool Initialize();

    void Shutdown();
}

// ── C exports — called by DS1Mod.Rendering via P/Invoke ──────────────────────

extern "C"
{
    // Register the managed delegate that is called inside every Present().
    // Must be called before the first Present fires (i.e. right after
    // D3DHook::Initialize returns).
    __declspec(dllexport) void DS1Mod_SetOnGuiCallback(OnGuiCallbackFn callback);

    // True once the ImGui context, D3D11 backend, and Win32 backend have been
    // initialised on the first real Present call.
    __declspec(dllexport) bool DS1Mod_IsImGuiReady();

    // Returns the ImGuiContext* so the managed side can call
    // igSetCurrentContext() and share the same imgui instance.
    __declspec(dllexport) void* DS1Mod_GetImGuiContext();
}
