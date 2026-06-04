// d3d_hook.cpp
// Hooks IDXGISwapChain::Present via vtable overwrite.
// ImGui is compiled directly into dinput8.dll — no external cimgui.dll required.
// imgui sources must be vendored into DS1Mod/DS1Mod.Injector/imgui/
// (copy imgui.h, imgui.cpp, imgui_impl_dx11.h/cpp, imgui_impl_win32.h/cpp there)

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <atomic>
#include "d3d_hook.h"
#include "modloader.h"   // Log() / LogInit()

#include "imgui/imgui.h"
#include "imgui/imgui_impl_dx11.h"
#include "imgui/imgui_impl_win32.h"

// ── globals ──────────────────────────────────────────────────────────────────

static std::atomic<bool>  g_imguiReady  { false };
static bool               g_initAttempted = false;
static OnGuiCallbackFn    g_onGuiCallback = nullptr;

static HWND               g_gameHwnd    = nullptr;
static WNDPROC            g_origWndProc = nullptr;

using PresentFn = HRESULT(__stdcall*)(IDXGISwapChain*, UINT, UINT);
static PresentFn g_origPresent = nullptr;

// ── helpers ──────────────────────────────────────────────────────────────────

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

// ── WndProc subclass ─────────────────────────────────────────────────────────

extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

static LRESULT WINAPI HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (g_imguiReady)
    {
        if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
            return TRUE;
    }
    return CallWindowProcW(g_origWndProc, hWnd, msg, wParam, lParam);
}

// ── ImGui init ───────────────────────────────────────────────────────────────

static void InitImGui(IDXGISwapChain* sc)
{
    g_initAttempted = true;

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

    DXGI_SWAP_CHAIN_DESC desc = {};
    sc->GetDesc(&desc);
    g_gameHwnd = desc.OutputWindow;
    {
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] Got D3D11 device — HWND=0x%p", (void*)g_gameHwnd);
        Log(msg);
    }

    ImGui::CreateContext();
    Log(L"[D3DHook] ImGui context created");

    if (!ImGui_ImplWin32_Init(g_gameHwnd))
    {
        Log(L"[D3DHook] ImGui_ImplWin32_Init FAILED");
        ImGui::DestroyContext();
        device->Release();
        context->Release();
        return;
    }
    if (!ImGui_ImplDX11_Init(device, context))
    {
        Log(L"[D3DHook] ImGui_ImplDX11_Init FAILED");
        ImGui_ImplWin32_Shutdown();
        ImGui::DestroyContext();
        device->Release();
        context->Release();
        return;
    }

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
    if (!g_initAttempted)
        InitImGui(sc);

    if (g_imguiReady.load())
    {
        ImGui_ImplDX11_NewFrame();
        ImGui_ImplWin32_NewFrame();
        ImGui::NewFrame();

        if (g_onGuiCallback)
            g_onGuiCallback();

        ImGui::Render();
        ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
    }

    return g_origPresent(sc, syncInterval, flags);
}

// ── D3DHook::Initialize ───────────────────────────────────────────────────────

bool D3DHook::Initialize()
{
    Log(L"[D3DHook] Initialize — creating probe device to read DXGI vtable...");

    // Create a throw-away window + D3D11 device + swap chain.
    // We only need them long enough to read the DXGI vtable and patch Present.
    WNDCLASSEXW wc  = { sizeof(wc) };
    wc.lpfnWndProc  = DefWindowProcW;
    wc.hInstance    = GetModuleHandleW(nullptr);
    wc.lpszClassName = L"DS1ModD3DProbe";
    RegisterClassExW(&wc);

    HWND hwnd = CreateWindowExW(0, wc.lpszClassName, L"", WS_OVERLAPPEDWINDOW,
                                0, 0, 8, 8, nullptr, nullptr, wc.hInstance, nullptr);
    if (!hwnd)
    {
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] CreateWindowEx FAILED — GetLastError=%u", GetLastError());
        Log(msg);
        UnregisterClassW(wc.lpszClassName, wc.hInstance);
        return false;
    }

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
        wchar_t msg[128];
        swprintf_s(msg, L"[D3DHook] D3D11CreateDeviceAndSwapChain FAILED hr=0x%08X", (unsigned)hr);
        Log(msg);
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
        ImGui_ImplWin32_Shutdown();
        ImGui_ImplDX11_Shutdown();
        ImGui::DestroyContext();
        g_imguiReady.store(false);
    }
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
        return ImGui::GetCurrentContext();
    }
}
