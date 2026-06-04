#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <filesystem>
#include "modloader.h"

namespace fs = std::filesystem;

using hostfxr_init_fn = int (*)(const wchar_t*, const void*, void**);
using hostfxr_delegate_fn = int (*)(void*, int, void**);
using hostfxr_close_fn = int (*)(void*);
using load_asm_fn = int (*)(const wchar_t*, const wchar_t*, const wchar_t*,
    const wchar_t*, void*, void**);

static constexpr int HDT_LOAD_ASSEMBLY = 4;

// ── logging ──────────────────────────────────────────────────────────────────

static HANDLE g_logFile = INVALID_HANDLE_VALUE;

void LogInit() {
    wchar_t selfPath[MAX_PATH] = {};
    HMODULE hSelf = nullptr;
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        reinterpret_cast<LPCWSTR>(&LogInit), &hSelf);
    GetModuleFileNameW(hSelf, selfPath, MAX_PATH);
    wchar_t* lastSlash = wcsrchr(selfPath, L'\\');
    if (lastSlash) wcscpy_s(lastSlash + 1, MAX_PATH - (lastSlash - selfPath + 1), L"ds1mod.log");
    g_logFile = CreateFileW(selfPath, GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
    Log(L"DS1Mod Deep Debug Log opened");
}

void Log(const wchar_t* msg) {
    if (g_logFile == INVALID_HANDLE_VALUE) return;
    int len = WideCharToMultiByte(CP_UTF8, 0, msg, -1, nullptr, 0, nullptr, nullptr);
    if (len <= 0) return;
    std::string utf8(len - 1, '\0');
    WideCharToMultiByte(CP_UTF8, 0, msg, -1, utf8.data(), len, nullptr, nullptr);
    utf8 += "\r\n";
    DWORD written;
    WriteFile(g_logFile, utf8.c_str(), static_cast<DWORD>(utf8.size()), &written, nullptr);
    OutputDebugStringW(msg);
}

static void LogFmt(const wchar_t* prefix, const wchar_t* value) {
    std::wstring line = std::wstring(prefix) + L"\"" + value + L"\"";
    Log(line.c_str());
}

static void LogHR(const wchar_t* label, int hr) {
    wchar_t buf[128];
    swprintf_s(buf, L"%s -> 0x%08X", label, (unsigned)hr);
    Log(buf);
}

// ── helpers ───────────────────────────────────────────────────────────────────

static LONG FilterClr(EXCEPTION_POINTERS* ep, wchar_t* buf, int bufLen) {
    DWORD code = ep->ExceptionRecord->ExceptionCode;
    DWORD hr = (ep->ExceptionRecord->NumberParameters >= 1) ? (DWORD)ep->ExceptionRecord->ExceptionInformation[0] : 0;
    swprintf_s(buf, bufLen, L"CLR EXCEPTION 0x%08X (HRESULT: 0x%08X)", code, hr);
    return EXCEPTION_EXECUTE_HANDLER;
}

static int SafePfnLoad(load_asm_fn pfn, const wchar_t* dll, const wchar_t* type, const wchar_t* method, void** out) {
    wchar_t exBuf[256] = {};
    __try {
        // (const wchar_t*)-1 is the sentinel for [UnmanagedCallersOnly]
        return pfn(dll, type, method, (const wchar_t*)-1, nullptr, out);
    }
    __except (FilterClr(GetExceptionInformation(), exBuf, 256)) {
        Log(exBuf);
        return -1;
    }
}

// ── public ────────────────────────────────────────────────────────────────────

bool InitModLoader(const wchar_t* gameDir) {
    LogFmt(L"Game Directory: ", gameDir);

    // 1. Locate hostfxr
    wchar_t progFiles[MAX_PATH] = {};
    ExpandEnvironmentStringsW(L"%ProgramFiles%", progFiles, MAX_PATH);
    fs::path fxrPath = fs::path(progFiles) / L"dotnet" / L"host" / L"fxr";

    fs::path bestDll;
    if (fs::exists(fxrPath)) {
        for (auto& entry : fs::directory_iterator(fxrPath)) {
            if (entry.is_directory()) {
                fs::path p = entry.path() / L"hostfxr.dll";
                if (fs::exists(p)) bestDll = p;
            }
        }
    }

    if (bestDll.empty()) { Log(L"FAIL: No hostfxr.dll found"); return false; }
    LogFmt(L"Using hostfxr: ", bestDll.wstring().c_str());

    HMODULE hFxr = LoadLibraryW(bestDll.wstring().c_str());
    if (!hFxr) return false;

    auto pfn_init = (hostfxr_init_fn)GetProcAddress(hFxr, "hostfxr_initialize_for_runtime_config");
    auto pfn_delegate = (hostfxr_delegate_fn)GetProcAddress(hFxr, "hostfxr_get_runtime_delegate");
    auto pfn_close = (hostfxr_close_fn)GetProcAddress(hFxr, "hostfxr_close");

    // 2. Resolve Host DLL paths
    wchar_t selfPath[MAX_PATH];
    GetModuleFileNameW(GetModuleHandleW(L"dinput8.dll"), selfPath, MAX_PATH);
    fs::path root = fs::path(selfPath).parent_path();
    fs::path hostDll = root / L"DS1Mod.Host.dll";
    fs::path config = root / L"DS1Mod.Host.runtimeconfig.json";

    LogFmt(L"Loading Host DLL: ", hostDll.wstring().c_str());

    // 3. Runtime Initialization
    void* ctx = nullptr;
    int rc = pfn_init(config.wstring().c_str(), nullptr, &ctx);
    LogHR(L"hostfxr_initialize", rc);
    if (rc != 0 || !ctx) return false;

    load_asm_fn pfn_load = nullptr;
    rc = pfn_delegate(ctx, HDT_LOAD_ASSEMBLY, (void**)&pfn_load);
    LogHR(L"hostfxr_get_delegate", rc);

    if (rc == 0 && pfn_load) {
        using entry_fn = int (*)(const wchar_t*, int);
        entry_fn pfn_init_managed = nullptr;

        const wchar_t* methodName = L"Initialize";

        // TRY 1: The "Strict" format (Namespace.Class,AssemblyName) - NO SPACE after comma
        const wchar_t* typeStrict = L"DS1Mod.Host.ModLoader, DS1Mod.Host";
        LogFmt(L"Attempt 1 - Type: ", typeStrict);
        rc = SafePfnLoad(pfn_load, hostDll.wstring().c_str(), typeStrict, methodName, (void**)&pfn_init_managed);

        if (rc != 0) {
            // TRY 2: The "Short" format (Namespace.Class)
            const wchar_t* typeShort = L"DS1Mod.Host.ModLoader";
            LogFmt(L"Attempt 2 - Type: ", typeShort);
            rc = SafePfnLoad(pfn_load, hostDll.wstring().c_str(), typeShort, methodName, (void**)&pfn_init_managed);
        }

        LogHR(L"Final Load Result", rc);

        if (rc == 0 && pfn_init_managed) {
            Log(L"Calling managed Initialize...");
            int dirLen = (int)((wcslen(gameDir) + 1) * sizeof(wchar_t));
            rc = pfn_init_managed(gameDir, dirLen);
            LogHR(L"Managed Initialize returned", rc);
        }
    }

    pfn_close(ctx);
    return rc == 0;
}