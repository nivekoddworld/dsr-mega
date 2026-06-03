/**
 * heapfix.cpp — patches DSR's static FFX and Lua heap allocation sizes
 *
 * Dark Souls Remastered pre-allocates fixed-size heaps for visual effects
 * (FFX) and AI scripts (Lua) at startup.  The vanilla sizes are too small
 * to hold all enemy-range effects once BossSfxMerger merges them into
 * FRPG_SfxBnd_CommonEffects; the game crashes immediately on area load.
 *
 * This replicates the approach from thefifthmatt/DS1HeapPatch:
 *   - Scan the loaded image for known byte patterns (mov ecx, <size>)
 *   - Unprotect the pages with VirtualProtect
 *   - Write the new (larger) size constant in-place
 *
 * Target: DarkSoulsRemastered.exe 1.03.1 (Steam)
 * Patterns are version-specific; update if the game is patched.
 */

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstring>
#include "heapfix.h"

// ── pattern / patch pairs ──────────────────────────────────────────────────

struct HeapPatch
{
    const char* label;
    BYTE        pattern[5];
    BYTE        patched[5];
    // Offsets (relative to pattern hit) where the 4-byte immediate lives.
    // DS1HeapPatch patches up to 3 occurrences per pattern.
    int         offsets[3];
    int         offsetCount;
};

// FFX effects heap: 0x00013200 (~78 KB) → 0x02000000 (~32 MB)
// Lua AI heap:      0x00003800 (~14 KB) → 0x009C4000 (~10 MB)
//
// The pattern is the full 5-byte instruction:  B9 xx xx xx xx  (mov ecx, imm32)
// offsets[] are how many bytes past the pattern start the imm32 sits (always 1
// for a simple B9 encoding, but the patcher may need to hit multiple sites).
static const HeapPatch k_patches[] =
{
    {
        "FFX heap",
        { 0xB9, 0x00, 0x00, 0x32, 0x01 },
        { 0xB9, 0x00, 0x00, 0x00, 0x02 },
        { 1, 40, 110 }, 3
    },
    {
        "Lua heap",
        { 0xB9, 0x00, 0x00, 0x38, 0x00 },
        { 0xB9, 0x40, 0x9C, 0x00, 0x00 },
        { 1 }, 1
    },
};

// ── helpers ────────────────────────────────────────────────────────────────

static bool PatchBytes(BYTE* addr, const BYTE* newBytes, size_t len)
{
    DWORD oldProtect;
    if (!VirtualProtect(addr, len, PAGE_EXECUTE_READWRITE, &oldProtect))
        return false;
    memcpy(addr, newBytes, len);
    VirtualProtect(addr, len, oldProtect, &oldProtect);
    FlushInstructionCache(GetCurrentProcess(), addr, len);
    return true;
}

static BYTE* ScanImage(BYTE* base, size_t imageSize, const BYTE* pattern, size_t patLen)
{
    for (size_t i = 0; i + patLen <= imageSize; ++i)
    {
        if (memcmp(base + i, pattern, patLen) == 0)
            return base + i;
    }
    return nullptr;
}

// ── public entry point ─────────────────────────────────────────────────────

void ApplyHeapFix()
{
    HMODULE exe = GetModuleHandleW(nullptr);
    if (!exe) return;

    // Walk the PE headers to find the .text section bounds
    auto* dos  = reinterpret_cast<IMAGE_DOS_HEADER*>(exe);
    auto* nt   = reinterpret_cast<IMAGE_NT_HEADERS*>(
                     reinterpret_cast<BYTE*>(exe) + dos->e_lfanew);
    BYTE*  base      = reinterpret_cast<BYTE*>(exe);
    size_t imageSize = nt->OptionalHeader.SizeOfImage;

    for (const auto& p : k_patches)
    {
        BYTE* hit = ScanImage(base, imageSize, p.pattern, sizeof(p.pattern));
        if (!hit)
        {
            OutputDebugStringA(("[DS1Mod] HeapFix: pattern not found for "
                                + std::string(p.label) + "\n").c_str());
            continue;
        }

        for (int i = 0; i < p.offsetCount; ++i)
        {
            BYTE* target = hit + p.offsets[i] - 1; // -1: offset is from pattern[0]
            // Patch the full 5-byte instruction at the pattern site
            if (i == 0)
                PatchBytes(hit, p.patched, sizeof(p.patched));
            // Additional occurrences share the same imm32 bytes (just the 4-byte value)
            else
                PatchBytes(hit + p.offsets[i], p.patched + 1, 4);
        }

        OutputDebugStringA(("[DS1Mod] HeapFix: patched " +
                            std::string(p.label) + "\n").c_str());
    }
}
