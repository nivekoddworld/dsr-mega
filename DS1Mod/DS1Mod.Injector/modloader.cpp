#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <shlobj.h>
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>
#include <string>
#include <filesystem>
#include "modloader.h"

namespace fs = std::filesystem;

// ── hostfxr function pointer typedefs ────────────────────────────────────────

using hostfxr_initialize_for_runtime_config_fn = int(*)(
    const wchar_t* runtime_config_path,
    const void*    parameters,
    void**         host_context_handle);

using hostfxr_get_runtime_delegate_fn = int(*)(
    void*  host_context_handle,
    int    type,
    void** delegate);

using hostfxr_close_fn = int(*)(void* host_context_handle);

// ── helper: find highest 8.x.y hostfxr.dll ───────────────────────────────────

static std::wstring FindHostfxr()
{
    wchar_t progFiles[MAX_PATH];
    if (!SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_PROGRAM_FILES, nullptr, 0, progFiles)))
        ExpandEnvironmentStringsW(L"%ProgramFiles%", progFiles, MAX_PATH);

    fs::path fxrBase = fs::path(progFiles) / L"dotnet" / L"host" / L"fxr";
    if (!fs::exists(fxrBase)) return {};

    fs::path best;
    for (auto& entry : fs::directory_iterator(fxrBase))
    {
        if (!entry.is_directory()) continue;
        std::wstring name = entry.path().filename().wstring();
        if (name.rfind(L"8.", 0) != 0) continue; // only 8.x.y
        if (best.empty() || entry.path().filename() > best.filename())
            best = entry.path();
    }

    if (best.empty()) return {};
    return (best / L"hostfxr.dll").wstring();
}

// ── helper: path to DS1Mod.Host next to the injector DLL ─────────────────────

static std::wstring GetHostPath()
{
    wchar_t selfPath[MAX_PATH];
    HMODULE hSelf = nullptr;
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |
                       GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                       (LPCWSTR)&GetHostPath, &hSelf);
    GetModuleFileNameW(hSelf, selfPath, MAX_PATH);

    fs::path hostDir = fs::path(selfPath).parent_path();
    return (hostDir / L"DS1Mod.Host.dll").wstring();
}

// ── public API ───────────────────────────────────────────────────────────────

bool InitModLoader(const wchar_t* gameDir)
{
    // 1. Locate hostfxr
    std::wstring hostfxrPath = FindHostfxr();
    if (hostfxrPath.empty()) return false;

    HMODULE hFxr = LoadLibraryW(hostfxrPath.c_str());
    if (!hFxr) return false;

    auto pfn_init = (hostfxr_initialize_for_runtime_config_fn)
        GetProcAddress(hFxr, "hostfxr_initialize_for_runtime_config");
    auto pfn_get_delegate = (hostfxr_get_runtime_delegate_fn)
        GetProcAddress(hFxr, "hostfxr_get_runtime_delegate");
    auto pfn_close = (hostfxr_close_fn)
        GetProcAddress(hFxr, "hostfxr_close");

    if (!pfn_init || !pfn_get_delegate || !pfn_close) return false;

    // 2. Initialize .NET runtime using DS1Mod.Host.runtimeconfig.json
    std::wstring hostDll = GetHostPath();
    std::wstring runtimeConfig = hostDll.substr(0, hostDll.size() - 4) + L".runtimeconfig.json";

    void* ctx = nullptr;
    if (pfn_init(runtimeConfig.c_str(), nullptr, &ctx) != 0 || !ctx)
        return false;

    // 3. Get load_assembly_and_get_function_pointer delegate
    using load_asm_fn = int(*)(
        const wchar_t* assembly_path,
        const wchar_t* type_name,
        const wchar_t* method_name,
        const wchar_t* delegate_type_name,
        void*          reserved,
        void**         delegate);

    load_asm_fn pfn_load = nullptr;
    // hdt_load_assembly_and_get_function_pointer = 4
    if (pfn_get_delegate(ctx, 4, (void**)&pfn_load) != 0 || !pfn_load)
    {
        pfn_close(ctx);
        return false;
    }

    // 4. Resolve DS1Mod.Host.ModLoader.Initialize
    using entry_point_fn = int(*)(const wchar_t*, int);
    entry_point_fn pfn_initialize = nullptr;

    int rc = pfn_load(
        hostDll.c_str(),
        L"DS1Mod.Host.ModLoader, DS1Mod.Host",
        L"Initialize",
        nullptr,  // nullptr → uses UnmanagedCallersOnly
        nullptr,
        (void**)&pfn_initialize);

    pfn_close(ctx);

    if (rc != 0 || !pfn_initialize) return false;

    // 5. Call managed Initialize with game directory as UTF-16 string
    int gameDirLen = (int)(wcslen(gameDir) + 1) * (int)sizeof(wchar_t);
    return pfn_initialize(gameDir, gameDirLen) == 0;
}
