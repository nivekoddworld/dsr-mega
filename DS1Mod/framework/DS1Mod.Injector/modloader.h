#pragma once
#include <windows.h>

bool InitModLoader(const wchar_t* gameDir);

// Call once from WorkerThread before Sleep() to open the log file.
void LogInit();
void Log(const wchar_t* msg);
