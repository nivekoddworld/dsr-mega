#pragma once
#include <windows.h>

/// <summary>
/// Loads DS1Mod.Host into the process via hostfxr and calls
/// DS1Mod.Host.ModLoader.Initialize with the game directory.
/// Returns true on success.
/// </summary>
bool InitModLoader(const wchar_t* gameDir);
