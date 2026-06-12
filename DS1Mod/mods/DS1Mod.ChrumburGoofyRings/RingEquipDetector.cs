using System.Runtime.InteropServices;
using DS1Mod.Core.Memory;

namespace DS1Mod.ChrumburGoofyRings;

/// <summary>
/// Detects whether a given ring is currently equipped. Uses the same heap
/// signature as DS1Mod.AutoEquip: an 8-byte self-pointer followed by the
/// inventory capacity (2048). The two equipped-ring ids (left, right) live at
/// fixed negative offset -0x318 from that signature as consecutive uint32s.
/// The signature dies on quit-out, so callers re-validate and rescan lazily.
///
/// Scanning copies each region with ReadProcessMemory (own process) instead
/// of dereferencing it: a region can be decommitted between VirtualQuery and
/// the read, and that AccessViolation would crash DSR. RPM returns false.
/// </summary>
internal sealed class RingEquipDetector
{
    private nint _signature;

    private const int RingIdOff = -0x318;
    private const int ChunkSize = 1 << 20;

    public bool HasSignature =>
        _signature != 0 && GameMemory.Read<nint>(_signature) == _signature;

    /// <summary>True if either ring slot holds <paramref name="ringId"/>.</summary>
    public bool IsRingEquipped(uint ringId)
    {
        if (!HasSignature) return false;
        nint rings = _signature + RingIdOff;
        return GameMemory.Read<uint>(rings) == ringId
            || GameMemory.Read<uint>(rings + 4) == ringId;
    }

    /// <summary>Heap-scan for the inventory signature. Expensive — caller throttles.</summary>
    public unsafe bool TryFindSignature(Action<string>? log = null)
    {
        _signature = 0;
        var hits = new List<nint>();
        var buffer = new byte[ChunkSize];
        nint self = GetCurrentProcess();
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
                // Chunks overlap by 16 bytes so a signature can't straddle two.
                for (nint chunk = mbi.BaseAddress; chunk < regionEnd; chunk += ChunkSize - 16)
                {
                    nint len = nint.Min(ChunkSize, regionEnd - chunk);
                    if (len < 16) break;
                    bool ok;
                    fixed (byte* b = buffer)
                        ok = ReadProcessMemory(self, chunk, b, len, out _);
                    if (!ok) continue;

                    fixed (byte* b = buffer)
                    {
                        for (nint off = 0; off + 16 <= len; off += 8)
                        {
                            ulong* p = (ulong*)(b + off);
                            if (*p == (ulong)(chunk + off)
                                && *(uint*)(p + 1) == 2048 && *((uint*)(p + 1) + 1) == 0)
                                hits.Add(chunk + off);
                        }
                    }
                }
            }

            addr = regionEnd;
        }

        if (hits.Count == 1) _signature = hits[0];
        else log?.Invoke($"[GoofyRings] inventory scan: {hits.Count} signature hits (need exactly 1) — retrying later.");
        return _signature != 0;
    }

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

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool ReadProcessMemory(nint hProcess, nint baseAddress, byte* buffer, nint size, out nint bytesRead);
}
