#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <filesystem>
#include "modloader.h"

namespace fs = std::filesystem;

// Define hostfxr delegate types inline — no .NET SDK headers required.
using hostfxr_init_fn     = int (*)(const wchar_t*, const void*, void**);
using hostfxr_delegate_fn = int (*)(void*, int, void**);
using hostfxr_close_fn    = int (*)(void*);
using load_asm_fn         = int (*)(const wchar_t*, const wchar_t*, const wchar_t*,
                                    const wchar_t*, void*, void**);

// hdt_load_assembly_and_get_function_pointer
static constexpr int HDT_LOAD_ASSEMBLY = 4;

// ── helpers ───────────────────────────────────────────────────────────────────

static std::wstring FindHostfxr()
{
    wchar_t progFiles[MAX_PATH] = {};
    ExpandEnvironmentStringsW(L"%ProgramFiles%", progFiles, MAX_PATH);

    fs::path fxrBase = fs::path(progFiles) / L"dotnet" / L"host" / L"fxr";
    if (!fs::exists(fxrBase)) return {};

    fs::path best;
    for (auto& entry : fs::directory_iterator(fxrBase))
    {
        if (!entry.is_directory()) continue;
        std::wstring name = entry.path().filename().wstring();
        if (name.rfind(L"8.", 0) != 0) continue; // .NET 8 only
        if (best.empty() || entry.path().filename() > best.filename())
            best = entry.path();
    }

    if (best.empty()) return {};
    return (best / L"hostfxr.dll").wstring();
}

static std::wstring GetHostDllPath()
{
    wchar_t selfPath[MAX_PATH] = {};
    HMODULE hSelf = nullptr;
    GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
        GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(&GetHostDllPath), &hSelf);
    GetModuleFileNameW(hSelf, selfPath, MAX_PATH);
    return (fs::path(selfPath).parent_path() / L"DS1Mod.Host.dll").wstring();
}

// ── public ────────────────────────────────────────────────────────────────────

bool InitModLoader(const wchar_t* gameDir)
{
    // 1. Locate and load hostfxr.dll
    std::wstring hostfxrPath = FindHostfxr();
    if (hostfxrPath.empty()) return false;

    HMODULE hFxr = LoadLibraryW(hostfxrPath.c_str());
    if (!hFxr) return false;

    auto pfn_init     = reinterpret_cast<hostfxr_init_fn>    (GetProcAddress(hFxr, "hostfxr_initialize_for_runtime_config"));
    auto pfn_delegate = reinterpret_cast<hostfxr_delegate_fn>(GetProcAddress(hFxr, "hostfxr_get_runtime_delegate"));
    auto pfn_close    = reinterpret_cast<hostfxr_close_fn>   (GetProcAddress(hFxr, "hostfxr_close"));
    if (!pfn_init || !pfn_delegate || !pfn_close) return false;

    // 2. Init runtime via DS1Mod.Host.runtimeconfig.json
    std::wstring hostDll    = GetHostDllPath();
    std::wstring runtimeCfg = hostDll.substr(0, hostDll.size() - 4) + L".runtimeconfig.json";

    void* ctx = nullptr;
    if (pfn_init(runtimeCfg.c_str(), nullptr, &ctx) != 0 || !ctx)
        return false;

    // 3. Get load_assembly_and_get_function_pointer
    load_asm_fn pfn_load = nullptr;
    if (pfn_delegate(ctx, HDT_LOAD_ASSEMBLY, reinterpret_cast<void**>(&pfn_load)) != 0 || !pfn_load)
    {
        pfn_close(ctx);
        return false;
    }

    // 4. Resolve DS1Mod.Host.ModLoader.Initialize
    using entry_fn = int (*)(const wchar_t*, int);
    entry_fn pfn_initialize = nullptr;

    int rc = pfn_load(
        hostDll.c_str(),
        L"DS1Mod.Host.ModLoader, DS1Mod.Host",
        L"Initialize",
        nullptr,   // UnmanagedCallersOnly — no delegate type name needed
        nullptr,
        reinterpret_cast<void**>(&pfn_initialize));

    pfn_close(ctx);
    if (rc != 0 || !pfn_initialize) return false;

    // 5. Call into managed code
    int gameDirBytes = static_cast<int>((wcslen(gameDir) + 1) * sizeof(wchar_t));
    return pfn_initialize(gameDir, gameDirBytes) == 0;
}
