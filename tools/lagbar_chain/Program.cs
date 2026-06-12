// lagbar_chain — one-shot diagnostic. Given the session-local address of the
// HP lag-bar value (found in-game by LagBarProbe), scan the live DSR process
// from OUTSIDE for pointers into that allocation and walk them back, level by
// level, until a pointer living in the main module's static data is found.
// The resulting chain  module+RVA -> +off -> ... -> +off  can then be
// hardcoded in DS1Mod.Core so the lag bar never needs runtime calibration.
//
// Usage: dotnet run -- <lagValueAddressHex>   e.g.  dotnet run -- 2B81DF90

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_PRIVATE = 0x20000;
    private const uint PAGE_NOACCESS = 0x001;
    private const uint PAGE_GUARD = 0x100;

    private const int MaxDepth = 4;
    private const int MaxHoldersPerLevel = 24;
    private const ulong Window = 0x1000;   // pointer may target up to 0x1000 below the value

    private static int Main(string[] args)
    {
        if (args.Length < 1
            || !ulong.TryParse(args[0].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out ulong target)
            || target == 0)
        {
            Console.WriteLine("usage: lagbar_chain <lagValueAddressHex>");
            return 1;
        }

        Process? game = Process.GetProcessesByName("DarkSoulsRemastered").FirstOrDefault();
        if (game == null) { Console.WriteLine("DarkSoulsRemastered.exe not running."); return 1; }

        nint h = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, game.Id);
        if (h == 0) { Console.WriteLine($"OpenProcess failed ({Marshal.GetLastWin32Error()})."); return 1; }

        var main = game.MainModule!;
        ulong modBase = (ulong)main.BaseAddress;
        ulong modEnd = modBase + (uint)main.ModuleMemorySize;
        Console.WriteLine($"process {game.Id}, module {modBase:X}..{modEnd:X}");

        if (VirtualQueryEx(h, (nint)target, out var tmbi, MbiSize) == 0 || tmbi.State != MEM_COMMIT)
        {
            Console.WriteLine($"target 0x{target:X} is not committed memory (stale address?).");
            return 1;
        }
        ulong allocBase = (ulong)tmbi.AllocationBase;
        Console.WriteLine($"target 0x{target:X} lives in allocation 0x{allocBase:X} (region 0x{(ulong)tmbi.BaseAddress:X}, size 0x{(ulong)tmbi.RegionSize:X})");

        // ── BFS back through pointer levels ───────────────────────────────
        var levelTargets = new List<ulong> { target };
        var chains = new Dictionary<ulong, string> { [target] = $"[lagValue 0x{target:X}]" };
        bool foundStatic = false;

        for (int depth = 0; depth < MaxDepth && !foundStatic && levelTargets.Count > 0; depth++)
        {
            Console.WriteLine($"\n— level {depth + 1}: scanning for pointers to {levelTargets.Count} target(s)…");
            var sw = Stopwatch.StartNew();
            var holders = FindPointers(h, levelTargets, modBase, modEnd, out var staticHits);
            Console.WriteLine($"  {holders.Count} heap holder(s), {staticHits.Count} static holder(s) ({sw.ElapsedMilliseconds} ms)");

            foreach ((ulong holderAddr, ulong pointee, ulong pointed) in staticHits)
            {
                ulong rva = holderAddr - modBase;
                long innerOff = (long)(pointee - pointed);
                Console.WriteLine($"  STATIC  module+0x{rva:X}  →  0x{pointed:X} (+0x{innerOff:X})  chain: module+0x{rva:X} -> +0x{innerOff:X} -> {chains[pointee]}");
                foundStatic = true;
            }

            var next = new List<ulong>();
            foreach ((ulong holderAddr, ulong pointee, ulong pointed) in holders.Take(MaxHoldersPerLevel))
            {
                long innerOff = (long)(pointee - pointed);
                if (!chains.ContainsKey(holderAddr))
                {
                    chains[holderAddr] = $"[0x{holderAddr:X}] -> +0x{innerOff:X} -> {chains[pointee]}";
                    next.Add(holderAddr);
                }
                Console.WriteLine($"  heap    0x{holderAddr:X}  →  0x{pointed:X} (+0x{innerOff:X})");
                ProbeRtti(h, pointed, modBase, modEnd);
            }
            levelTargets = next;
        }

        if (!foundStatic)
            Console.WriteLine("\nNo static holder found within depth limit.");

        // Identify the owning object's RTTI class if it starts with a vptr.
        ProbeRtti(h, allocBase, modBase, modEnd);
        return 0;
    }

    private static List<(ulong Holder, ulong Pointee, ulong Pointed)> FindPointers(
        nint h, List<ulong> targets, ulong modBase, ulong modEnd,
        out List<(ulong Holder, ulong Pointee, ulong Pointed)> staticHits)
    {
        var heap = new List<(ulong, ulong, ulong)>();
        staticHits = new List<(ulong, ulong, ulong)>();
        ulong[] sorted = targets.OrderBy(t => t).ToArray();
        byte[] buf = new byte[1 << 20];

        ulong addr = 0x10000;
        while (VirtualQueryEx(h, (nint)addr, out var mbi, MbiSize) != 0)
        {
            ulong regionEnd = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;
            if ((ulong)mbi.RegionSize == 0) break;

            bool isModule = (ulong)mbi.BaseAddress >= modBase && (ulong)mbi.BaseAddress < modEnd;
            bool scannable = mbi.State == MEM_COMMIT
                && (mbi.Protect & (PAGE_GUARD | PAGE_NOACCESS)) == 0
                && (mbi.Type == MEM_PRIVATE || isModule);

            if (scannable)
            {
                for (ulong chunk = (ulong)mbi.BaseAddress; chunk < regionEnd; chunk += (ulong)buf.Length)
                {
                    int len = (int)Math.Min((ulong)buf.Length, regionEnd - chunk);
                    if (!ReadProcessMemory(h, (nint)chunk, buf, len, out _)) continue;
                    int n = len / 8;
                    for (int i = 0; i < n; i++)
                    {
                        ulong v = BitConverter.ToUInt64(buf, i * 8);
                        if (v == 0) continue;
                        foreach (ulong t in sorted)
                        {
                            if (v <= t && t - v <= Window)
                            {
                                ulong holder = chunk + (ulong)i * 8;
                                // skip self-referential neighbors inside the same window
                                if (holder >= v && holder - v <= Window) break;
                                var hit = (holder, t, v);
                                if (holder >= modBase && holder < modEnd) staticHits.Add(hit);
                                else heap.Add(hit);
                                break;
                            }
                        }
                    }
                }
            }
            addr = regionEnd;
            if (addr > 0x7FFF_FFFF_FFFF) break;
        }
        return heap;
    }

    private static void ProbeRtti(nint h, ulong objAddr, ulong modBase, ulong modEnd)
    {
        byte[] q = new byte[8];
        if (!ReadProcessMemory(h, (nint)objAddr, q, 8, out _)) return;
        ulong vft = BitConverter.ToUInt64(q, 0);
        if (vft < modBase || vft >= modEnd) return;
        if (!ReadProcessMemory(h, (nint)(vft - 8), q, 8, out _)) return;
        ulong col = BitConverter.ToUInt64(q, 0);
        if (col < modBase || col >= modEnd) return;
        byte[] colBuf = new byte[0x18];
        if (!ReadProcessMemory(h, (nint)col, colBuf, colBuf.Length, out _)) return;
        if (BitConverter.ToUInt32(colBuf, 0) != 1) return;
        uint tdRva = BitConverter.ToUInt32(colBuf, 0x0C);
        byte[] name = new byte[96];
        if (!ReadProcessMemory(h, (nint)(modBase + tdRva + 0x10), name, name.Length, out _)) return;
        int z = Array.IndexOf(name, (byte)0);
        if (z < 1) return;
        string cls = Encoding.ASCII.GetString(name, 0, z);
        if (cls.StartsWith(".?A")) Console.WriteLine($"    RTTI at 0x{objAddr:X}: {cls}");
    }

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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll")]
    private static extern nint VirtualQueryEx(nint h, nint addr, out MEMORY_BASIC_INFORMATION mbi, nint len);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(nint h, nint addr, byte[] buf, int len, out nint read);
}
