using System.Runtime.InteropServices;

namespace DS1Mod.DemoMod;

/// <summary>
/// Vftable hijack for <c>NS_FRPG::ChrInsFactory</c>. Overwrites the single
/// virtual slot (vftable[0]) with a hand-assembled native trampoline that
/// captures register state on every call, then tail-jumps to the original.
///
/// Why this approach and not an inline detour:
///  - The original function entry at 0x14033b880 is already a 5-byte E9 JMP
///    rel32 trampoline into the VMProtect overlay. Overwriting those bytes
///    would force us to relocate a RIP-relative jump — fragile.
///  - vftable[0] is a single 8-byte aligned pointer in <c>.rdata</c>. Swap
///    it, and every <c>call qword ptr [vftbl+0]</c> indirect call by the
///    game resolves through us. Reverting is one pointer write.
///  - 8-byte aligned QWORD writes on x64 are atomic — no torn-read race.
///
/// Capture buffer layout (zeroed at install, written by the stub):
/// <code>
///   +0x00  uint32  call counter (incremented atomically-ish on each call)
///   +0x04  uint32  padding
///   +0x08  nint    this  (RCX at call site)
///   +0x10  nint    arg1  (RDX)
///   +0x18  nint    arg2  (R8)
///   +0x20  nint    arg3  (R9)
///   +0x28  nint    arg4  (stack [rsp+0x28] in caller frame)
///   +0x30  nint    arg5  (stack [rsp+0x30])
///   +0x38  nint    arg6  (stack [rsp+0x38])
///   +0x40  nint    arg7  (stack [rsp+0x40])
/// </code>
///
/// Safety: forgetting to <see cref="Uninstall"/> before assembly unload leaves
/// vftable[0] pointing at our deallocated stub. Don't unload while the hook
/// is live. The intent is exploratory — install, load a map, read captures,
/// uninstall before exiting.
/// </summary>
internal static class ChrInsFactoryHook
{
    private const uint MEM_COMMIT             = 0x1000;
    private const uint MEM_RESERVE            = 0x2000;
    private const uint MEM_RELEASE            = 0x8000;
    private const uint PAGE_READWRITE         = 0x04;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    [DllImport("kernel32")]
    private static extern nint VirtualAlloc(nint addr, nuint size, uint allocType, uint protect);

    [DllImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(nint addr, nuint size, uint newProtect, out uint oldProtect);

    [DllImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(nint addr, nuint size, uint freeType);

    public sealed class State
    {
        public bool Installed;
        public nint VftableAddr;
        public nint OriginalFn;
        public nint StubAddr;
        public nint CaptureBuf;
    }

    private static State? _state;
    public static State? Current => _state;

    public sealed class Snapshot
    {
        public int  CallCount;
        public nint This, Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7;
    }

    public static Snapshot? Read()
    {
        if (_state is not { Installed: true } s) return null;
        nint b = s.CaptureBuf;
        return new Snapshot
        {
            CallCount = Marshal.ReadInt32(b + 0x00),
            This      = Marshal.ReadIntPtr(b + 0x08),
            Arg1      = Marshal.ReadIntPtr(b + 0x10),
            Arg2      = Marshal.ReadIntPtr(b + 0x18),
            Arg3      = Marshal.ReadIntPtr(b + 0x20),
            Arg4      = Marshal.ReadIntPtr(b + 0x28),
            Arg5      = Marshal.ReadIntPtr(b + 0x30),
            Arg6      = Marshal.ReadIntPtr(b + 0x38),
            Arg7      = Marshal.ReadIntPtr(b + 0x40),
        };
    }

    public static (bool ok, string msg) Install(nint vftableAddr, nint originalFn)
    {
        if (_state is { Installed: true })
            return (false, "already installed");
        if (vftableAddr == 0 || originalFn == 0)
            return (false, "vftable or original function address is 0");

        // Capture buffer — plain RW.
        nint capBuf = VirtualAlloc(0, 256, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (capBuf == 0) return (false, "VirtualAlloc(capture) failed");

        // Stub region — RWX.
        nint stub = VirtualAlloc(0, 256, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (stub == 0)
        {
            VirtualFree(capBuf, 0, MEM_RELEASE);
            return (false, "VirtualAlloc(stub) failed");
        }

        byte[] code = BuildStub(capBuf, originalFn);
        Marshal.Copy(code, 0, stub, code.Length);

        // Patch vftable[0]: temporarily flip the .rdata page to RW, swap, restore.
        if (!VirtualProtect(vftableAddr, 8, PAGE_READWRITE, out uint oldProt))
        {
            VirtualFree(stub, 0, MEM_RELEASE);
            VirtualFree(capBuf, 0, MEM_RELEASE);
            return (false, "VirtualProtect (->RW) failed");
        }

        // Verify the slot still holds the expected pointer before overwriting.
        nint observed = Marshal.ReadIntPtr(vftableAddr);
        if (observed != originalFn)
        {
            VirtualProtect(vftableAddr, 8, oldProt, out _);
            VirtualFree(stub, 0, MEM_RELEASE);
            VirtualFree(capBuf, 0, MEM_RELEASE);
            return (false,
                $"vftable[0] = 0x{(ulong)observed:X} ≠ expected 0x{(ulong)originalFn:X} (someone else hooked?)");
        }

        Marshal.WriteIntPtr(vftableAddr, stub);
        VirtualProtect(vftableAddr, 8, oldProt, out _);

        _state = new State
        {
            Installed   = true,
            VftableAddr = vftableAddr,
            OriginalFn  = originalFn,
            StubAddr    = stub,
            CaptureBuf  = capBuf,
        };
        return (true, $"hook installed: stub=0x{(ulong)stub:X}  cap=0x{(ulong)capBuf:X}");
    }

    public static (bool ok, string msg) Uninstall()
    {
        if (_state is not { Installed: true } s) return (false, "not installed");

        if (!VirtualProtect(s.VftableAddr, 8, PAGE_READWRITE, out uint oldProt))
            return (false, "VirtualProtect failed during uninstall");
        Marshal.WriteIntPtr(s.VftableAddr, s.OriginalFn);
        VirtualProtect(s.VftableAddr, 8, oldProt, out _);

        // Deliberately leak the stub + capture buffer. Another thread may be
        // mid-stub execution at the exact moment we restored the vftable; if
        // we VirtualFree the stub now, that thread fault. Memory cost is
        // 512 bytes — well worth it for the safety. The session is short.
        s.Installed = false;
        return (true, "hook uninstalled (stub + capture buf intentionally leaked)");
    }

    // ── Stub builder ──────────────────────────────────────────────────────
    //
    // The stub preserves all caller state except RAX (which is volatile and
    // would have been clobbered by the tail-called function anyway). After
    // capturing register + stack args it tail-jumps via `mov rax, imm64;
    // jmp rax` to the original function address.
    //
    //   push rax
    //   mov  rax, capBufVA
    //   inc  dword ptr [rax]            ; counter++
    //   mov  [rax+0x08], rcx            ; this
    //   mov  [rax+0x10], rdx            ; arg1
    //   mov  [rax+0x18], r8             ; arg2
    //   mov  [rax+0x20], r9             ; arg3
    //   ; stack args: caller's [rsp+0x28..+0x48] are now at [rsp+0x30..+0x50]
    //   mov  rcx, [rsp+0x30] ; arg4
    //   mov  [rax+0x28], rcx
    //   mov  rcx, [rsp+0x38] ; arg5
    //   mov  [rax+0x30], rcx
    //   mov  rcx, [rsp+0x40] ; arg6
    //   mov  [rax+0x38], rcx
    //   mov  rcx, [rsp+0x48] ; arg7
    //   mov  [rax+0x40], rcx
    //   mov  rcx, [rax+0x08]            ; restore RCX (this)
    //   pop  rax
    //   mov  rax, originalFnVA
    //   jmp  rax
    private static byte[] BuildStub(nint capBufVA, nint originalFnVA)
    {
        var b = new List<byte>(96);

        b.Add(0x50);                                                   // push rax
        b.AddRange(new byte[] { 0x48, 0xB8 });                         // mov  rax, imm64
        b.AddRange(BitConverter.GetBytes((ulong)capBufVA));
        b.AddRange(new byte[] { 0xFF, 0x00 });                         // inc  dword ptr [rax]
        b.AddRange(new byte[] { 0x48, 0x89, 0x48, 0x08 });             // mov  [rax+08], rcx
        b.AddRange(new byte[] { 0x48, 0x89, 0x50, 0x10 });             // mov  [rax+10], rdx
        b.AddRange(new byte[] { 0x4C, 0x89, 0x40, 0x18 });             // mov  [rax+18], r8
        b.AddRange(new byte[] { 0x4C, 0x89, 0x48, 0x20 });             // mov  [rax+20], r9

        // Stack args. Caller's shadow space is at [rsp+0x08..+0x20], so the
        // first stack-passed arg (arg4) is at caller's [rsp+0x28]. We pushed
        // RAX, so add 0x08 to every offset: it's now at [rsp+0x30].
        b.AddRange(new byte[] { 0x48, 0x8B, 0x4C, 0x24, 0x30 });       // mov  rcx, [rsp+0x30]  ; arg4
        b.AddRange(new byte[] { 0x48, 0x89, 0x48, 0x28 });             // mov  [rax+28], rcx
        b.AddRange(new byte[] { 0x48, 0x8B, 0x4C, 0x24, 0x38 });       // mov  rcx, [rsp+0x38]  ; arg5
        b.AddRange(new byte[] { 0x48, 0x89, 0x48, 0x30 });             // mov  [rax+30], rcx
        b.AddRange(new byte[] { 0x48, 0x8B, 0x4C, 0x24, 0x40 });       // mov  rcx, [rsp+0x40]  ; arg6
        b.AddRange(new byte[] { 0x48, 0x89, 0x48, 0x38 });             // mov  [rax+38], rcx
        b.AddRange(new byte[] { 0x48, 0x8B, 0x4C, 0x24, 0x48 });       // mov  rcx, [rsp+0x48]  ; arg7
        b.AddRange(new byte[] { 0x48, 0x89, 0x48, 0x40 });             // mov  [rax+40], rcx

        // Restore RCX = this (we clobbered it above when used as scratch).
        b.AddRange(new byte[] { 0x48, 0x8B, 0x48, 0x08 });             // mov  rcx, [rax+08]

        b.Add(0x58);                                                   // pop  rax

        b.AddRange(new byte[] { 0x48, 0xB8 });                         // mov  rax, imm64
        b.AddRange(BitConverter.GetBytes((ulong)originalFnVA));
        b.AddRange(new byte[] { 0xFF, 0xE0 });                         // jmp  rax

        return b.ToArray();
    }
}
