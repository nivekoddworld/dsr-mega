#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <filesystem>
#include "modloader.h"

namespace fs = std::filesystem;

using hostfxr_init_fn     = int (*)(const wchar_t*, const void*, void**);
using hostfxr_delegate_fn = int (*)(void*, int, void**);
using hostfxr_close_fn    = int (*)(void*);
using load_asm_fn         = int (*)(const wchar_t*, const wchar_t*, const wchar_t*,
                                    const wchar_t*, void*, void**);

static constexpr int HDT_LOAD_ASSEMBLY = 4;

// Sentinel: tells load_assembly_and_get_function_pointer that the target
// method has [UnmanagedCallersOnly] — return a raw fn ptr, not a delegate.
static const wchar_t* const UNMANAGEDCALLERSONLY_METHOD = (const wchar_t*)-1;

// ── logging (raw Win32 so it works even if CRT is weird) ──────────────────────

static HANDLE g_logFile = INVALID_HANDLE_VALUE;

void LogInit()
{
    // Write log next to dinput8.dll (same folder as the game exe)
    wchar_t selfPath[MAX_PATH] = {};
    HMODULE hSelf = nullptr;
    GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(&LogInit), &hSelf);
    GetModuleFileNameW(hSelf, selfPath, MAX_PATH);

    wchar_t* lastSlash = wcsrchr(selfPath, L'\\');
    if (lastSlash) wcscpy_s(lastSlash + 1, MAX_PATH - (lastSlash - selfPath + 1), L"ds1mod.log");

    g_logFile = CreateFileW(selfPath, GENERIC_WRITE, FILE_SHARE_READ,
                            nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    Log(L"DS1Mod log opened");
}

void Log(const wchar_t* msg)
{
    if (g_logFile == INVALID_HANDLE_VALUE) return;

    // Convert to UTF-8 for a simple text file
    int len = WideCharToMultiByte(CP_UTF8, 0, msg, -1, nullptr, 0, nullptr, nullptr);
    if (len <= 0) return;

    std::string utf8(len - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, msg, -1, utf8.data(), len, nullptr, nullptr);
    utf8 += "\r\n";

    DWORD written;
    WriteFile(g_logFile, utf8.c_str(), static_cast<DWORD>(utf8.size()), &written, nullptr);

    OutputDebugStringW(msg);
    OutputDebugStringW(L"\n");
}

static void LogFmt(const wchar_t* prefix, const wchar_t* value)
{
    std::wstring line = std::wstring(prefix) + value;
    Log(line.c_str());
}

static void LogHR(const wchar_t* label, int hr)
{
    wchar_t buf[128];
    swprintf_s(buf, L"%s -> 0x%08X", label, (unsigned)hr);
    Log(buf);
}

// ── helpers ───────────────────────────────────────────────────────────────────

static std::wstring FindHostfxr()
{
    wchar_t progFiles[MAX_PATH] = {};
    ExpandEnvironmentStringsW(L"%ProgramFiles%", progFiles, MAX_PATH);

    fs::path fxrBase = fs::path(progFiles) / L"dotnet" / L"host" / L"fxr";
    LogFmt(L"Searching hostfxr in: ", fxrBase.wstring().c_str());

    if (!fs::exists(fxrBase))
    {
        Log(L"FAIL: fxr directory not found — install .NET 8+ runtime");
        return {};
    }

    fs::path best;
    int bestMajor = -1;
    for (auto& entry : fs::directory_iterator(fxrBase))
    {
        if (!entry.is_directory()) continue;
        std::wstring name = entry.path().filename().wstring();
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
        Log(L"FAIL: no .NET 8+ hostfxr found");
        return {};
    }

    fs::path fxrDll = best / L"hostfxr.dll";
    LogFmt(L"Found hostfxr: ", fxrDll.wstring().c_str());
    return fxrDll.wstring();
}

static std::wstring GetHostDllPath()
{
    wchar_t selfPath[MAX_PATH] = {};
    HMODULE hSelf = nullptr;
    GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(&GetHostDllPath), &hSelf);
    GetModuleFileNameW(hSelf, selfPath, MAX_PATH);
    fs::path hostDll = fs::path(selfPath).parent_path() / L"DS1Mod.Host.dll";
    LogFmt(L"Host DLL: ", hostDll.wstring().c_str());
    return hostDll.wstring();
}

// ── SEH-safe wrappers (no C++ objects → __try allowed) ───────────────────────

// Extract HRESULT from CLR exception record so we know WHICH .NET exception fired.
// ExceptionInformation[1] holds the HRESULT for code 0xE0434352.
static LONG FilterClr(EXCEPTION_POINTERS* ep, wchar_t* buf, int bufLen)
{
    DWORD code = ep->ExceptionRecord->ExceptionCode;
    DWORD hr   = (ep->ExceptionRecord->NumberParameters >= 2)
                 ? (DWORD)ep->ExceptionRecord->ExceptionInformation[1]
                 : 0;
    swprintf_s(buf, bufLen, L"EXCEPTION 0x%08X  HRESULT 0x%08X", code, hr);
    return EXCEPTION_EXECUTE_HANDLER;
}

static int SafePfnLoad(load_asm_fn pfn,
                       const wchar_t* dll, const wchar_t* type,
                       const wchar_t* method, void** out)
{
    wchar_t exBuf[128] = {};
    __try
    {
        return pfn(dll, type, method, UNMANAGEDCALLERSONLY_METHOD, nullptr, out);
    }
    __except (FilterClr(GetExceptionInformation(), exBuf, 128))
    {
        Log(exBuf);
        return -1;
    }
}

static int SafeInitialize(int (*fn)(const wchar_t*, int),
                          const wchar_t* gameDir, int bytes)
{
    wchar_t exBuf[128] = {};
    __try
    {
        return fn(gameDir, bytes);
    }
    __except (FilterClr(GetExceptionInformation(), exBuf, 128))
    {
        Log(exBuf);
        return -1;
    }
}

// ── public ────────────────────────────────────────────────────────────────────

bool InitModLoader(const wchar_t* gameDir)
{
    LogFmt(L"InitModLoader gameDir: ", gameDir);

    std::wstring hostfxrPath = FindHostfxr();
    if (hostfxrPath.empty()) return false;

    HMODULE hFxr = LoadLibraryW(hostfxrPath.c_str());
    if (!hFxr)
    {
        LogHR(L"FAIL: LoadLibrary(hostfxr) GetLastError", (int)GetLastError());
        return false;
    }
    Log(L"hostfxr.dll loaded");

    auto pfn_init     = reinterpret_cast<hostfxr_init_fn>    (GetProcAddress(hFxr, "hostfxr_initialize_for_runtime_config"));
    auto pfn_delegate = reinterpret_cast<hostfxr_delegate_fn>(GetProcAddress(hFxr, "hostfxr_get_runtime_delegate"));
    auto pfn_close    = reinterpret_cast<hostfxr_close_fn>   (GetProcAddress(hFxr, "hostfxr_close"));
    if (!pfn_init || !pfn_delegate || !pfn_close)
    {
        Log(L"FAIL: missing hostfxr exports");
        return false;
    }

    std::wstring hostDll    = GetHostDllPath();
    std::wstring runtimeCfg = hostDll.substr(0, hostDll.size() - 4) + L".runtimeconfig.json";
    LogFmt(L"runtimeconfig: ", runtimeCfg.c_str());

    if (!fs::exists(runtimeCfg))
    {
        Log(L"FAIL: runtimeconfig.json not found next to dinput8.dll");
        return false;
    }

    void* ctx = nullptr;
    int rc = pfn_init(runtimeCfg.c_str(), nullptr, &ctx);
    LogHR(L"hostfxr_initialize_for_runtime_config", rc);
    if (rc != 0 || !ctx)
    {
        Log(L"FAIL: runtime init");
        return false;
    }

    load_asm_fn pfn_load = nullptr;
    rc = pfn_delegate(ctx, HDT_LOAD_ASSEMBLY, reinterpret_cast<void**>(&pfn_load));
    LogHR(L"hostfxr_get_runtime_delegate", rc);
    if (rc != 0 || !pfn_load)
    {
        Log(L"FAIL: get delegate");
        pfn_close(ctx);
        return false;
    }

    if (!fs::exists(hostDll))
    {
        Log(L"FAIL: DS1Mod.Host.dll not found");
        pfn_close(ctx);
        return false;
    }

    using entry_fn = int (*)(const wchar_t*, int);
    entry_fn pfn_initialize = nullptr;

    rc = SafePfnLoad(pfn_load,
                     hostDll.c_str(),
                     L"DS1Mod.Host.ModLoader, DS1Mod.Host",
                     L"Initialize",
                     reinterpret_cast<void**>(&pfn_initialize));

    pfn_close(ctx);
    LogHR(L"load_assembly_and_get_function_pointer", rc);
    if (rc != 0 || !pfn_initialize)
    {
        Log(L"FAIL: could not resolve Initialize");
        return false;
    }

    Log(L"Calling managed Initialize...");
    int gameDirBytes = static_cast<int>((wcslen(gameDir) + 1) * sizeof(wchar_t));
    rc = SafeInitialize(pfn_initialize, gameDir, gameDirBytes);

    LogHR(L"Initialize returned", rc);
    Log(rc == 0 ? L"Mod loader OK" : L"FAIL: Initialize non-zero");
    return rc == 0;
}
