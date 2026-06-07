using System.Runtime.InteropServices;
using DS1Mod.Core.Memory;

namespace DS1Mod.DebugMenu;

/// <summary>
/// Injects items into the player's inventory via the game's internal item-get
/// function. Shellcode template from JKAnderson/DSR-Gadget Resources/Assembly/GetItem.txt.
///
/// Execution model: VirtualAlloc RWX → patch shellcode → CreateThread (waits
/// ≤5 s) → VirtualFree. Runs on a fresh OS thread so the game processes the
/// item-get on its own stack. This is identical to what DSR-Gadget does via
/// PropertyHook's Execute() method, except we're already in-process so there
/// is no remote-thread penalty.
/// </summary>
internal static class ItemGiver
{
    // ── AOB pattern for the item-get function ──────────────────────────────
    // From JKAnderson/DSR-Gadget DSROffsets.cs (ItemGetAOB).
    // This is a direct function-start match — the resolved address IS the
    // function entry point we'll call with movabs r14, addr; call r14.
    private const string ItemGetAOB =
        "48 89 5C 24 18 89 54 24 10 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 F9";

    private static nint _itemGetAddr;    // address of the item-get function
    private static bool _scanned;

    /// <summary>
    /// Resolve the item-get function address (AOB direct match, no RIP resolve).
    /// Unlike the manager pointers (which are RIP-relative LEA/MOV), this AOB
    /// matches the very first bytes of the function, so the hit address IS the
    /// function pointer.
    /// </summary>
    internal static bool EnsureScanned()
    {
        if (_scanned) return _itemGetAddr != 0;
        _scanned = true;
        _itemGetAddr = GameMemory.Scan(ItemGetAOB);
        Console.WriteLine($"[ItemGiver] itemGet=0x{(ulong)_itemGetAddr:X}");
        return _itemGetAddr != 0;
    }

    /// <summary>
    /// Give the player <paramref name="quantity"/> of item
    /// <paramref name="itemId"/> in <paramref name="category"/>.
    ///
    /// chrClassTarget — the static address that holds the GameDataMan pointer.
    /// This is the value DSR-Gadget calls ChrClassBasePtr.Resolve(): the RIP-
    /// resolved static address, NOT the dereferenced value. The shellcode does
    /// `movabs rax, [chrClassTarget]` to load the pointer at that address.
    /// </summary>
    internal static bool GiveItem(nint chrClassTarget, int category, int itemId, int quantity)
    {
        if (!EnsureScanned()) return false;
        if (chrClassTarget == 0) return false;

        // ── Build shellcode ───────────────────────────────────────────────
        // Template: every 0xFE byte is a placeholder. Offsets verified against
        // DSR-Gadget DSRAssembly.cs Array.Copy calls.
        byte[] code = (byte[])ShellcodeTemplate.Clone();

        // offset 0x01: category  (int32, edx  = 2nd arg)
        WriteInt32(code, 0x01, category);
        // offset 0x07: quantity  (int32, r9d  = 4th arg)
        WriteInt32(code, 0x07, quantity);
        // offset 0x0D: itemId    (int32, r8d  = 3rd arg)
        WriteInt32(code, 0x0D, itemId);
        // offset 0x19: chrClassTarget (int64, movabs rax, [addr])
        WriteInt64(code, 0x19, (long)chrClassTarget);
        // offset 0x46: itemGetAddr   (int64, movabs r14, addr)
        WriteInt64(code, 0x46, (long)_itemGetAddr);

        // ── Allocate RWX, run, free ───────────────────────────────────────
        nint mem = VirtualAlloc(nint.Zero, (nuint)code.Length,
                                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (mem == nint.Zero) return false;

        try
        {
            Marshal.Copy(code, 0, mem, code.Length);
            // CreateThread with mem as lpStartAddress — shellcode ignores
            // the lpParameter (RCX is overwritten by `lea rcx,[r15+280h]`).
            nint thread = CreateThread(nint.Zero, 0, mem, nint.Zero, 0, out _);
            if (thread == nint.Zero) return false;
            WaitForSingleObject(thread, 5000);
            CloseHandle(thread);
        }
        finally
        {
            VirtualFree(mem, 0, MEM_RELEASE);
        }

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void WriteInt32(byte[] buf, int offset, int value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Copy(b, 0, buf, offset, 4);
    }

    private static void WriteInt64(byte[] buf, int offset, long value)
    {
        byte[] b = BitConverter.GetBytes(value);
        Array.Copy(b, 0, buf, offset, 8);
    }

    // ── Win32 P/Invoke ────────────────────────────────────────────────────

    private const uint MEM_COMMIT  = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    [DllImport("kernel32.dll")]
    private static extern nint VirtualAlloc(nint lpAddress, nuint dwSize,
                                            uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(nint lpAddress, nuint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll")]
    private static extern nint CreateThread(nint lpAttr, nuint dwStackSize,
                                            nint lpStartAddr, nint lpParam,
                                            uint dwFlags, out uint lpThreadId);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    // ── Shellcode template (GetItem.txt, placeholders = 0xFE) ─────────────
    // Decoded from JKAnderson/DSR-Gadget Resources/Assembly/GetItem.txt
    //
    // 00: BA FE FE FE FE              mov edx, [category]
    // 05: 41 B9 FE FE FE FE           mov r9d, [quantity]
    // 0B: 41 B8 FE FE FE FE           mov r8d, [itemId]
    // 11: 41 BC FE FE FE FE           mov r12d, 0xFEFEFEFE  (not patched)
    // 17: 48 A1 FE×8                  movabs rax, [chrClassTarget ptr]
    // 21: C6 44 24 38 01              mov byte[rsp+38h], 1
    // 26: 40 88 7C 24 30              mov byte[rsp+30h], dil
    // 2B: C6 44 24 28 01              mov byte[rsp+28h], 1
    // 30: 4C 8B 78 10                 mov r15, [rax+10h]
    // 34: C6 44 24 20 01              mov byte[rsp+20h], 1
    // 39: 49 8D 8F 80 02 00 00        lea rcx, [r15+280h]
    // 40: 48 83 EC 38                 sub rsp, 38h
    // 44: 49 BE FE×8                  movabs r14, [itemGetAddr]
    // 4E: 41 FF D6                    call r14
    // 51: 48 83 C4 38                 add rsp, 38h
    // 55: C3                          ret
    private static readonly byte[] ShellcodeTemplate =
    {
        0xBA, 0xFE, 0xFE, 0xFE, 0xFE,              // 00
        0x41, 0xB9, 0xFE, 0xFE, 0xFE, 0xFE,        // 05
        0x41, 0xB8, 0xFE, 0xFE, 0xFE, 0xFE,        // 0B
        0x41, 0xBC, 0xFE, 0xFE, 0xFE, 0xFE,        // 11
        0x48, 0xA1, 0xFE, 0xFE, 0xFE, 0xFE,        // 17
              0xFE, 0xFE, 0xFE, 0xFE,               //   (upper 4 bytes of qword)
        0xC6, 0x44, 0x24, 0x38, 0x01,              // 21
        0x40, 0x88, 0x7C, 0x24, 0x30,              // 26
        0xC6, 0x44, 0x24, 0x28, 0x01,              // 2B
        0x4C, 0x8B, 0x78, 0x10,                    // 30
        0xC6, 0x44, 0x24, 0x20, 0x01,              // 34
        0x49, 0x8D, 0x8F, 0x80, 0x02, 0x00, 0x00,  // 39
        0x48, 0x83, 0xEC, 0x38,                    // 40
        0x49, 0xBE, 0xFE, 0xFE, 0xFE, 0xFE,        // 44
              0xFE, 0xFE, 0xFE, 0xFE,               //   (upper 4 bytes)
        0x41, 0xFF, 0xD6,                           // 4E
        0x48, 0x83, 0xC4, 0x38,                    // 51
        0xC3                                       // 55
    };
}
