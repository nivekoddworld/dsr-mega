#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>

// Helper to safely write a 4-byte value to protected memory
bool SafeWriteDWORD(BYTE* addr, DWORD value) {
    DWORD oldProtect;
    if (VirtualProtect(addr, sizeof(DWORD), PAGE_EXECUTE_READWRITE, &oldProtect)) {
        *reinterpret_cast<DWORD*>(addr) = value;
        VirtualProtect(addr, sizeof(DWORD), oldProtect, &oldProtect);
        return true;
    }
    return false;
}

void ApplyHeapFix() {
    HMODULE exe = GetModuleHandleW(nullptr);
    if (!exe) return;

    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(exe);
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS*>(reinterpret_cast<BYTE*>(exe) + dos->e_lfanew);

    // We scan the whole image to find the constants
    BYTE* base = reinterpret_cast<BYTE*>(exe);
    size_t size = nt->OptionalHeader.SizeOfImage;

    // --- FFX Heap Fix (3 locations required) ---
    // Pattern: B9 00 32 01 00 (mov ecx, 13200h)
    // In memory (Little Endian), 0x13200 is 00 32 01 00
    BYTE ffxPat[] = { 0xB9, 0x00, 0x32, 0x01, 0x00 };
    DWORD ffxNewSize = 0x02000000; // 32MB

    for (size_t i = 0; i < size - 120; ++i) {
        if (memcmp(base + i, ffxPat, 5) == 0) {
            // Site 1: The Anchor (The instruction itself)
            SafeWriteDWORD(base + i + 1, ffxNewSize);

            // Site 2: Boundary Check (Usually +40 bytes)
            if (*reinterpret_cast<DWORD*>(base + i + 40) == 0x13200) {
                SafeWriteDWORD(base + i + 40, ffxNewSize);
            }

            // Site 3: Pool Limit (Usually +110 bytes)
            if (*reinterpret_cast<DWORD*>(base + i + 110) == 0x13200) {
                SafeWriteDWORD(base + i + 110, ffxNewSize);
            }
            break;
        }
    }

    // --- Lua Heap Fix ---
    // Pattern: B9 00 38 00 00 (mov ecx, 3800h)
    BYTE luaPat[] = { 0xB9, 0x00, 0x38, 0x00, 0x00 };
    DWORD luaNewSize = 0x009C4000; // 10MB

    for (size_t i = 0; i < size - 5; ++i) {
        if (memcmp(base + i, luaPat, 5) == 0) {
            SafeWriteDWORD(base + i + 1, luaNewSize);
            break;
        }
    }

    FlushInstructionCache(GetCurrentProcess(), nullptr, 0);
}