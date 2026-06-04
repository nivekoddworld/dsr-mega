// d3d_hook.cpp
// Hooks IDXGISwapChain::Present via vtable overwrite.
// ImGui is managed entirely through cimgui.dll (loaded at runtime) so that
// the same context is shared with the managed DS1Mod.Rendering layer, which
// uses ImGui.NET — a C# wrapper that also P/Invokes into cimgui.dll.
//
// Build requirements:
//   - Link: d3d11.lib, dxgi.lib (add to vcxproj AdditionalDependencies)
//   - cimgui.dll must be present in the game directory at runtime
//     (built by DS1Mod.ImGuiNative — see that project for vendoring instructions)

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <atomic>
#include "d3d_hook.h"
#include "modloader.h"   // Log() / LogInit() — writes to ds1mod.log

// ── cimgui function pointer typedefs ────────────────────────────────────────
// We load cimgui.dll at runtime so that both this module and the managed
// ImGui.NET layer share a single copy — same context, same draw list.

using igCreateContext_t        = void* (*)(void*);
using igDestroyContext_t       = void (*)(void*);
using igGetDrawData_t          = void* (*)();
using igNewFrame_t             = void (*)();
using igRender_t               = void (*)();

// imgui_impl_dx11 backends (exported from our custom cimgui.dll)
using ImplDX11_Init_t          = bool (*)(void* device, void* deviceContext);
using ImplDX11_Shutdown_t      = void (*)();
using ImplDX11_NewFrame_t      = void (*)();
using ImplDX11_RenderDrawData_t = void (*)(void* drawData);

// imgui_impl_win32 backends
using ImplWin32_Init_t              = bool (*)(void* hwnd);
using ImplWin32_Shutdown_t          = void (*)();
using ImplWin32_NewFrame_t          = void (*)();
using ImplWin32_WndProcHandler_t    = bool (*)(HWND, UINT, WPARAM, LPARAM);

// ── globals ──────────────────────────────────────────────────────────────────

static HMODULE               g_cimgui                  = nullptr;

static igCreateContext_t     g_igCreateContext          = nullptr;
static igDestroyContext_t    g_igDestroyContext         = nullptr;
static igGetDrawData_t       g_igGetDrawData            = nullptr;
static igNewFrame_t          g_igNewFrame               = nullptr;
static igRender_t            g_igRender                 = nullptr;

static ImplDX11_Init_t           g_DX11_Init            = nullptr;
static ImplDX11_Shutdown_t       g_DX11_Shutdown        = nullptr;
static ImplDX11_NewFrame_t       g_DX11_NewFrame        = nullptr;
static ImplDX11_RenderDrawData_t g_DX11_RenderDrawData  = nullptr;

static ImplWin32_Init_t              g_Win32_Init         = nullptr;
static ImplWin32_Shutdown_t          g_Win32_Shutdown     = nullptr;
static ImplWin32_NewFrame_t          g_Win32_NewFrame      = nullptr;
static ImplWin32_WndProcHandler_t    g_Win32_WndProcHandler = nullptr;

static void*              g_imguiCtx    = nullptr;
static std::atomic<bool>  g_imguiReady  { false };
static OnGuiCallbackFn    g_onGuiCallback = nullptr;

static HWND               g_gameHwnd    = nullptr;
static WNDPROC            g_origWndProc = nullptr;

using PresentFn = HRESULT(__stdcall*)(IDXGISwapChain*, UINT, UINT);
static PresentFn g_origPresent = nullptr;

// ── helpers ──────────────────────────────────────────────────────────────────

static void* GetProc(const char* name)
{
    return GetProcAddress(g_cimgui, name);
}

template<typename T>
static bool Bind(T& dst, const char* name)
{
    dst = reinterpret_cast<T>(GetProc(name));
    return dst != nullptr;
}

static void* PatchVTable(void* obj, int slot, void* newFn)
{
    void** vt  = *reinterpret_cast<void***>(obj);
    void*  old = vt[slot];

    DWORD prev;
    VirtualProtect(&vt[slot], sizeof(void*), PAGE_READWRITE, &prev);
    vt[slot] = newFn;
    VirtualProtect(&vt[slot], sizeof(void*), prev, &prev);

    return old;
}

// ── WndProc subclass — routes input to ImGui ─────────────────────────────────

static LRESULT WINAPI HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (g_imguiReady && g_Win32_WndProcHandler)
    {
        if (g_Win32_WndProcHandler(hWnd, msg, wParam, lParam))
            return TRUE;
    }
    return CallWindowProcW(g_origWndProc, hWnd, msg, wParam, lParam);
}

// ── ImGui init — runs on the render thread on the first Present call ──────────

static void InitImGui(IDXGISwapChain* sc)
{
    // Load cimgui.dll from the same directory as dinput8.dll.
    wchar_t selfPath[MAX_PATH] = {};
    GetModuleFileNameW(GetModuleHandleW(L"dinput8.dll"), selfPath, MAX_PATH);
    wchar_t* sl = wcsrchr(selfPath, L'\\');
    if (sl) wcscpy_s(sl + 1, MAX_PATH - (sl - selfPath + 1), L"cimgui.dll");

    {
        wchar_t msg[MAX_PATH + 64];
        swprintf_s(msg, L"[D3DHook] Loading cimgui.dll from: %s", selfPath);
        Log(msg);
    }

    g_cimgui = LoadLibraryW(selfPath);
    if (!g_cimgui)
    {
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] LoadLibrary(cimgui.dll) FAILED — GetLastError=%u", GetLastError());
        Log(msg);
        return;
    }
    Log(L"[D3DHook] cimgui.dll loaded OK");

    // Bind core — log each failure individually
    auto BindLog = [&](auto& dst, const char* name) -> bool {
        dst = reinterpret_cast<std::remove_reference_t<decltype(dst)>>(GetProc(name));
        if (!dst) {
            wchar_t msg[128];
            swprintf_s(msg, L"[D3DHook] Missing export: %hs", name);
            Log(msg);
        }
        return dst != nullptr;
    };

    bool ok = true;
    ok &= BindLog(g_igCreateContext,  "igCreateContext");
    ok &= BindLog(g_igDestroyContext, "igDestroyContext");
    ok &= BindLog(g_igGetDrawData,    "igGetDrawData");
    ok &= BindLog(g_igNewFrame,       "igNewFrame");
    ok &= BindLog(g_igRender,         "igRender");

    ok &= BindLog(g_DX11_Init,            "DS1Mod_ImplDX11_Init");
    ok &= BindLog(g_DX11_Shutdown,        "DS1Mod_ImplDX11_Shutdown");
    ok &= BindLog(g_DX11_NewFrame,        "DS1Mod_ImplDX11_NewFrame");
    ok &= BindLog(g_DX11_RenderDrawData,  "DS1Mod_ImplDX11_RenderDrawData");
    ok &= BindLog(g_Win32_Init,           "DS1Mod_ImplWin32_Init");
    ok &= BindLog(g_Win32_Shutdown,       "DS1Mod_ImplWin32_Shutdown");
    ok &= BindLog(g_Win32_NewFrame,       "DS1Mod_ImplWin32_NewFrame");
    ok &= BindLog(g_Win32_WndProcHandler, "DS1Mod_ImplWin32_WndProcHandler");

    if (!ok)
    {
        Log(L"[D3DHook] One or more exports missing from cimgui.dll — ImGui disabled");
        FreeLibrary(g_cimgui);
        g_cimgui = nullptr;
        return;
    }

    // Get the D3D11 device + context from the real swap chain
    ID3D11Device*        device  = nullptr;
    ID3D11DeviceContext* context = nullptr;
    HRESULT hr = sc->GetDevice(__uuidof(ID3D11Device), reinterpret_cast<void**>(&device));
    if (FAILED(hr))
    {
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] GetDevice(ID3D11Device) FAILED hr=0x%08X", (unsigned)hr);
        Log(msg);
        return;
    }
    device->GetImmediateContext(&context);

    // Get the HWND from swap chain desc
    DXGI_SWAP_CHAIN_DESC desc = {};
    sc->GetDesc(&desc);
    g_gameHwnd = desc.OutputWindow;
    {
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] Got D3D11 device — HWND=0x%p", (void*)g_gameHwnd);
        Log(msg);
    }

    // Init ImGui
    g_imguiCtx = g_igCreateContext(nullptr);
    Log(L"[D3DHook] ImGui context created");

    if (!g_Win32_Init(g_gameHwnd))
    {
        Log(L"[D3DHook] ImGui_ImplWin32_Init FAILED");
        g_igDestroyContext(g_imguiCtx);
        g_imguiCtx = nullptr;
        device->Release();
        context->Release();
        return;
    }
    if (!g_DX11_Init(device, context))
    {
        Log(L"[D3DHook] ImGui_ImplDX11_Init FAILED");
        g_Win32_Shutdown();
        g_igDestroyContext(g_imguiCtx);
        g_imguiCtx = nullptr;
        device->Release();
        context->Release();
        return;
    }

    // Subclass the game window for input routing
    g_origWndProc = reinterpret_cast<WNDPROC>(
        SetWindowLongPtrW(g_gameHwnd, GWLP_WNDPROC,
                          reinterpret_cast<LONG_PTR>(HookedWndProc)));

    device->Release();
    context->Release();

    g_imguiReady.store(true);
    Log(L"[D3DHook] ImGui initialised on render thread");
}

// ── Present hook ─────────────────────────────────────────────────────────────

static HRESULT __stdcall HookedPresent(IDXGISwapChain* sc, UINT syncInterval, UINT flags)
{
    if (!g_imguiReady.load())
        InitImGui(sc);

    if (g_imguiReady.load())
    {
        g_DX11_NewFrame();
        g_Win32_NewFrame();
        g_igNewFrame();

        // Hand off to managed mods (OnGui implementations)
        if (g_onGuiCallback)
            g_onGuiCallback();

        g_igRender();
        g_DX11_RenderDrawData(g_igGetDrawData());
    }

    return g_origPresent(sc, syncInterval, flags);
}

// ── D3DHook::Initialize ───────────────────────────────────────────────────────

bool D3DHook::Initialize()
{
    // Create a throw-away window + D3D11 device + swap chain.
    // We only need them long enough to read the DXGI vtable and patch Present.
    WNDCLASSEXW wc  = { sizeof(wc) };
    wc.lpfnWndProc  = DefWindowProcW;
    wc.hInstance    = GetModuleHandleW(nullptr);
    wc.lpszClassName = L"DS1ModD3DProbe";
    RegisterClassExW(&wc);

    HWND hwnd = CreateWindowExW(0, wc.lpszClassName, L"", WS_OVERLAPPEDWINDOW,
                                0, 0, 8, 8, nullptr, nullptr, wc.hInstance, nullptr);
    if (!hwnd) { UnregisterClassW(wc.lpszClassName, wc.hInstance); return false; }

    DXGI_SWAP_CHAIN_DESC sd       = {};
    sd.BufferCount                 = 1;
    sd.BufferDesc.Format           = DXGI_FORMAT_R8G8B8A8_UNORM;
    sd.BufferUsage                 = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    sd.OutputWindow                = hwnd;
    sd.SampleDesc.Count            = 1;
    sd.Windowed                    = TRUE;
    sd.SwapEffect                  = DXGI_SWAP_EFFECT_DISCARD;

    IDXGISwapChain*      sc  = nullptr;
    ID3D11Device*        dev = nullptr;
    ID3D11DeviceContext* ctx = nullptr;
    D3D_FEATURE_LEVEL    fl;

    HRESULT hr = D3D11CreateDeviceAndSwapChain(
        nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, 0,
        nullptr, 0, D3D11_SDK_VERSION,
        &sd, &sc, &dev, &fl, &ctx);

    if (FAILED(hr) || !sc)
    {
        DestroyWindow(hwnd);
        UnregisterClassW(wc.lpszClassName, wc.hInstance);
        return false;
    }

    // Vtable slot 8 = IDXGISwapChain::Present
    g_origPresent = reinterpret_cast<PresentFn>(PatchVTable(sc, 8, HookedPresent));

    ctx->Release();
    dev->Release();
    sc->Release();
    DestroyWindow(hwnd);
    UnregisterClassW(wc.lpszClassName, wc.hInstance);

    Log(L"[D3DHook] Present vtable patched");
    return true;
}

void D3DHook::Shutdown()
{
    if (g_gameHwnd && g_origWndProc)
        SetWindowLongPtrW(g_gameHwnd, GWLP_WNDPROC,
                          reinterpret_cast<LONG_PTR>(g_origWndProc));

    if (g_imguiReady.load())
    {
        g_Win32_Shutdown();
        g_DX11_Shutdown();
        g_igDestroyContext(g_imguiCtx);
        g_imguiCtx   = nullptr;
        g_imguiReady.store(false);
    }

    if (g_cimgui) { FreeLibrary(g_cimgui); g_cimgui = nullptr; }
}

// ── C exports ────────────────────────────────────────────────────────────────

extern "C"
{
    __declspec(dllexport) void DS1Mod_SetOnGuiCallback(OnGuiCallbackFn callback)
    {
        g_onGuiCallback = callback;
    }

    __declspec(dllexport) bool DS1Mod_IsImGuiReady()
    {
        return g_imguiReady.load();
    }

    __declspec(dllexport) void* DS1Mod_GetImGuiContext()
    {
        return g_imguiCtx;
    }
}
