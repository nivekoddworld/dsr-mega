#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <ctime>
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

// ── logging ───────────────────────────────────────────────────────────────────

static std::wofstream g_log;

static void LogInit(const wchar_t* gameDir)
{
    fs::path logPath = fs::path(gameDir) / L"ds1mod.log";
    g_log.open(logPath, std::ios::out | std::ios::trunc);
    g_log << L"[DS1Mod] Log started\n";
    g_log.flush();
}

static void Log(const std::wstring& msg)
{
    if (g_log.is_open())
    {
        g_log << L"[DS1Mod] " << msg << L"\n";
        g_log.flush();
    }
    OutputDebugStringW((L"[DS1Mod] " + msg + L"\n").c_str());
}

static void LogHR(const std::wstring& label, int hr)
{
    std::wostringstream ss;
    ss << label << L" → 0x" << std::hex << (unsigned)hr;
    Log(ss.str());
}

// ── helpers ───────────────────────────────────────────────────────────────────

static std::wstring FindHostfxr()
{
    wchar_t progFiles[MAX_PATH] = {};
    ExpandEnvironmentStringsW(L"%ProgramFiles%", progFiles, MAX_PATH);

    fs::path fxrBase = fs::path(progFiles) / L"dotnet" / L"host" / L"fxr";
    Log(L"Looking for hostfxr in: " + fxrBase.wstring());

    if (!fs::exists(fxrBase))
    {
        Log(L"hostfxr base directory not found — is the .NET runtime installed?");
        return {};
    }

    fs::path best;
    int bestMajor = -1;
    for (auto& entry : fs::directory_iterator(fxrBase))
    {
        if (!entry.is_directory()) continue;
        std::wstring name = entry.path().filename().wstring();

        // Accept any major version >= 8
        int major = 0;
        try { major = std::stoi(name); } catch (...) { continue; }
        if (major < 8) continue;

        if (major > bestMajor || (major == bestMajor && entry.path().filename() > best.filename()))
        {
            bestMajor = major;
            best = entry.path();
        }
    }

    if (best.empty())
    {
        Log(L"No .NET 8+ hostfxr found in " + fxrBase.wstring());
        return {};
    }

    fs::path fxrDll = best / L"hostfxr.dll";
    Log(L"Found hostfxr: " + fxrDll.wstring());
    return fxrDll.wstring();
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
    fs::path hostDll = fs::path(selfPath).parent_path() / L"DS1Mod.Host.dll";
    Log(L"Host DLL path: " + hostDll.wstring());
    return hostDll.wstring();
}

// ── public ────────────────────────────────────────────────────────────────────

bool InitModLoader(const wchar_t* gameDir)
{
    LogInit(gameDir);
    Log(L"InitModLoader — gameDir: " + std::wstring(gameDir));

    // 1. Locate and load hostfxr.dll
    std::wstring hostfxrPath = FindHostfxr();
    if (hostfxrPath.empty())
    {
        Log(L"FAIL: hostfxr not found");
        return false;
    }

    HMODULE hFxr = LoadLibraryW(hostfxrPath.c_str());
    if (!hFxr)
    {
        LogHR(L"FAIL: LoadLibrary(hostfxr) error", (int)GetLastError());
        return false;
    }
    Log(L"Loaded hostfxr.dll");

    auto pfn_init     = reinterpret_cast<hostfxr_init_fn>    (GetProcAddress(hFxr, "hostfxr_initialize_for_runtime_config"));
    auto pfn_delegate = reinterpret_cast<hostfxr_delegate_fn>(GetProcAddress(hFxr, "hostfxr_get_runtime_delegate"));
    auto pfn_close    = reinterpret_cast<hostfxr_close_fn>   (GetProcAddress(hFxr, "hostfxr_close"));
    if (!pfn_init || !pfn_delegate || !pfn_close)
    {
        Log(L"FAIL: missing hostfxr exports");
        return false;
    }

    // 2. Init runtime via DS1Mod.Host.runtimeconfig.json
    std::wstring hostDll    = GetHostDllPath();
    std::wstring runtimeCfg = hostDll.substr(0, hostDll.size() - 4) + L".runtimeconfig.json";
    Log(L"runtimeconfig: " + runtimeCfg);

    if (!fs::exists(runtimeCfg))
    {
        Log(L"FAIL: runtimeconfig.json not found");
        return false;
    }

    void* ctx = nullptr;
    int rc = pfn_init(runtimeCfg.c_str(), nullptr, &ctx);
    LogHR(L"hostfxr_initialize_for_runtime_config", rc);
    if (rc != 0 || !ctx)
    {
        Log(L"FAIL: runtime init failed");
        return false;
    }

    // 3. Get load_assembly_and_get_function_pointer
    load_asm_fn pfn_load = nullptr;
    rc = pfn_delegate(ctx, HDT_LOAD_ASSEMBLY, reinterpret_cast<void**>(&pfn_load));
    LogHR(L"hostfxr_get_runtime_delegate", rc);
    if (rc != 0 || !pfn_load)
    {
        Log(L"FAIL: get delegate failed");
        pfn_close(ctx);
        return false;
    }

    // 4. Resolve DS1Mod.Host.ModLoader.Initialize
    using entry_fn = int (*)(const wchar_t*, int);
    entry_fn pfn_initialize = nullptr;

    if (!fs::exists(hostDll))
    {
        Log(L"FAIL: DS1Mod.Host.dll not found at " + hostDll);
        pfn_close(ctx);
        return false;
    }

    rc = pfn_load(
        hostDll.c_str(),
        L"DS1Mod.Host.ModLoader, DS1Mod.Host",
        L"Initialize",
        nullptr,   // UnmanagedCallersOnly — no delegate type name needed
        nullptr,
        reinterpret_cast<void**>(&pfn_initialize));

    pfn_close(ctx);
    LogHR(L"load_assembly_and_get_function_pointer", rc);
    if (rc != 0 || !pfn_initialize)
    {
        Log(L"FAIL: could not resolve Initialize entry point");
        return false;
    }

    // 5. Call into managed code
    Log(L"Calling Initialize...");
    int gameDirBytes = static_cast<int>((wcslen(gameDir) + 1) * sizeof(wchar_t));
    int result = pfn_initialize(gameDir, gameDirBytes);
    LogHR(L"Initialize returned", result);

    if (result == 0)
        Log(L"Mod loader initialised successfully");
    else
        Log(L"FAIL: Initialize returned non-zero");

    return result == 0;
}
