#pragma once
#include <windows.h>

// Call from DLL_PROCESS_ATTACH (before the entry point runs) to install the
// dearxan hook. The hook fires once Arxan's entry point stubs finish, then
// signals g_arxanReady so InitModLoader knows it's safe to load mods.
void DisableArxan();

bool InitModLoader(const wchar_t* gameDir);

// Call once from WorkerThread before Sleep() to open the log file.
void LogInit();
void Log(const wchar_t* msg);
