using System.Runtime.InteropServices;
using DS1Mod.Core.Memory;

namespace DS1Mod.AutoEquip;

/// <summary>
/// All live addresses derived from the inventory signature. The signature is
/// an 8-byte self-pointer immediately followed by the capacity (2048); the
/// inventory array starts 16 bytes past it and the equip-slot tables sit at
/// fixed negative offsets (same layout the standalone AutoEquip tool uses).
/// </summary>
public readonly struct InventoryAddresses
{
    public readonly nint Signature;

    public InventoryAddresses(nint signature) => Signature = signature;

    public nint Inventory  => Signature + 16;
    public nint ArmorId    => Signature - 0x32C;
    public nint WeaponId   => Signature - 0x34C;
    public nint ArmorSlot  => Signature - 0x3AC;
    public nint WeaponSlot => Signature - 0x3CC;
    public nint RingSlot   => Signature - 0x398;
    public nint RingId     => Signature - 0x318;
    public nint InGame     => Signature - 0x660;

    public bool Found => Signature != 0;

    /// <summary>The signature is valid while the self-pointer still points at itself.</summary>
    public bool StillValid =>
        Found && GameMemory.Read<nint>(Signature) == Signature;
}

/// <summary>
/// Scans the process heap for the inventory structure. Unlike
/// GameMemory.Scan (module-only), the inventory lives in a private heap
/// allocation, so we walk MEM_PRIVATE committed regions ourselves. Running
/// in-process means a hit is just a pointer — no ReadProcessMemory needed.
/// </summary>
public static class InventoryScanner
{
    public const int SlotCount = 2048;

    /// <summary>
    /// Find the unique inventory signature: an 8-aligned qword whose value is
    /// its own address, followed by the capacity dwords (2048, 0). Returns 0
    /// if none or more than one match (ambiguous — caller retries later).
    /// </summary>
    public static unsafe nint FindSignature(Action<string>? log = null)
    {
        var hits = new List<nint>();
        nint addr = 0x10000;

        while (VirtualQuery(addr, out MEMORY_BASIC_INFORMATION mbi, MbiSize) != 0)
        {
            nint regionEnd = mbi.BaseAddress + mbi.RegionSize;
            if (mbi.RegionSize <= 0) break;

            bool scannable = mbi.State == MEM_COMMIT
                && mbi.Type == MEM_PRIVATE
                && (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) == 0
                && (mbi.Protect & PAGE_READWRITE) != 0;

            if (scannable)
            {
                ulong* p   = (ulong*)mbi.BaseAddress;
                ulong* end = (ulong*)(regionEnd - 16);
                for (; p < end; p++)
                {
                    // self-pointer, then capacity 2048 with zero padding
                    if (*p == (ulong)p && *(uint*)(p + 1) == SlotCount && *((uint*)(p + 1) + 1) == 0)
                        hits.Add((nint)p);
                }
            }

            addr = regionEnd;
        }

        if (hits.Count == 1) return hits[0];
        if (hits.Count > 1)
            log?.Invoke($"[AutoEquip] {hits.Count} inventory signatures found — can't pin down the right one, retrying later.");
        return 0;
    }

    /// <summary>Snapshot the full 2048-slot inventory. False if the block is unreadable.</summary>
    public static unsafe bool ReadInventory(nint invAddr, InvSlot[] dest)
    {
        int bytes = SlotCount * sizeof(InvSlot);
        if (!GameMemory.CanRead(invAddr, bytes)) return false;

        fixed (InvSlot* d = dest)
            Buffer.MemoryCopy((void*)invAddr, d, bytes, bytes);
        return true;
    }

    // ── VirtualQuery interop ───────────────────────────────────────────────

    private const uint MEM_COMMIT     = 0x1000;
    private const uint MEM_PRIVATE    = 0x20000;
    private const uint PAGE_NOACCESS  = 0x001;
    private const uint PAGE_GUARD     = 0x100;
    private const uint PAGE_READWRITE = 0x04;

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private static readonly nint MbiSize = Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();

    [DllImport("kernel32.dll")]
    private static extern nint VirtualQuery(nint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, nint dwLength);
}
