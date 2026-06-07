using System.Text;
using DS1Mod.Core.Memory;

namespace DS1Mod.DemoMod;

/// <summary>
/// Locates <c>NS_FRPG::ChrInsFactory::create</c> in the live DSR process by
/// walking MSVC RTTI from the mangled class name. Static-analysis evidence
/// for the existence of this factory was extracted from the DSR binary:
///
///   .data 0x141afffd0  ".?AVChrInsFactory@NS_FRPG@@"   (TypeDescriptor name)
///   .rdata 0x141321060 vftable                          (single slot)
///   .text  0x14033b880 ChrInsFactory::create            (vftable[0])
///
/// Resolution path used here:
///   1. AOB-scan the main module for the mangled name string.
///      (Verified unique against the binary — exactly 1 hit.)
///   2. TypeDescriptor starts 0x10 bytes before the name string.
///   3. The RTTI Complete Object Locator (COL) holds the TypeDescriptor as
///      a 4-byte RVA at COL+0x0C. Scan for that RVA in non-exec sections.
///   4. The vftable slot at vftable[-1] holds the COL pointer. Scan for a
///      qword equal to the COL VA.
///   5. vftable = COL_ref + 8, and vftable[0] is the create function.
///
/// The first-bytes-of-function AOB is NOT unique (verified — 100+ collisions
/// due to VMProtect trampoline padding), so it can't be the primary anchor.
/// We use it only as a read-back sanity check (the first byte must be 0xE9,
/// since the function entry is a JMP trampoline into the obfuscated overlay).
/// </summary>
internal static class ChrInsFactoryProbe
{
    // The mangled RTTI name, null-terminated. ASCII; encoded inline.
    private const string MangledName = ".?AVChrInsFactory@NS_FRPG@@\0";

    public sealed class ProbeResult
    {
        public bool   Ok;
        public nint   NameStringVA;
        public nint   TypeDescriptorVA;
        public nint   ColVA;
        public nint   VftableVA;
        public nint   FunctionVA;
        public byte[] EntryBytes = [];
        public string Log = "";
    }

    public static ProbeResult Resolve()
    {
        var sb = new StringBuilder();
        var r  = new ProbeResult();

        sb.AppendLine($"ModuleBase = 0x{(ulong)GameMemory.ModuleBase:X}");
        sb.AppendLine($"ModuleSize = 0x{GameMemory.ModuleSize:X}");

        // Step 1: scan for the mangled name string.
        string nameAob = ToAOB(MangledName);
        r.NameStringVA = GameMemory.Scan(nameAob);
        sb.AppendLine($"[1] name string  : 0x{(ulong)r.NameStringVA:X}");
        if (r.NameStringVA == 0) { sb.AppendLine("    NOT FOUND — RTTI walk aborted."); r.Log = sb.ToString(); return r; }

        // Step 2: TypeDescriptor begins 0x10 bytes before the name.
        r.TypeDescriptorVA = r.NameStringVA - 0x10;
        sb.AppendLine($"[2] TypeDescriptor: 0x{(ulong)r.TypeDescriptorVA:X}");

        // Step 3: scan for a 4-byte RVA pointing to the TypeDescriptor.
        uint tdRva = (uint)((ulong)r.TypeDescriptorVA - (ulong)GameMemory.ModuleBase);
        string rvaAob = ToAOB(BitConverter.GetBytes(tdRva));
        nint rvaRef  = GameMemory.Scan(rvaAob);
        sb.AppendLine($"[3] RVA ref to TD: 0x{(ulong)rvaRef:X}  (TD RVA = 0x{tdRva:X})");
        if (rvaRef == 0) { sb.AppendLine("    NOT FOUND."); r.Log = sb.ToString(); return r; }

        // RTTI COL layout: signature(4) offset(4) cdOffset(4) pTypeDescriptor(4 RVA) ...
        // So COL = rvaRef - 0x0C.
        r.ColVA = rvaRef - 0x0C;
        sb.AppendLine($"[4] COL          : 0x{(ulong)r.ColVA:X}");

        // Step 5: scan for a qword equal to COL_VA — that's the vftable[-1] slot.
        string colAob = ToAOB(BitConverter.GetBytes((ulong)r.ColVA));
        nint colRef = GameMemory.Scan(colAob);
        sb.AppendLine($"[5] qword ref COL: 0x{(ulong)colRef:X}");
        if (colRef == 0) { sb.AppendLine("    NOT FOUND."); r.Log = sb.ToString(); return r; }

        r.VftableVA = colRef + 8;
        sb.AppendLine($"[6] vftable      : 0x{(ulong)r.VftableVA:X}");

        // Step 7: read vftable[0] → the create function.
        if (!GameMemory.CanRead(r.VftableVA, 8))
        {
            sb.AppendLine("    vftable address not readable."); r.Log = sb.ToString(); return r;
        }
        r.FunctionVA = GameMemory.Read<nint>(r.VftableVA);
        sb.AppendLine($"[7] create fn    : 0x{(ulong)r.FunctionVA:X}");

        // Step 8: read & verify the first 16 entry bytes. First byte should be 0xE9
        // (a JMP rel32 into the VMProtect overlay).
        if (GameMemory.CanRead(r.FunctionVA, 16))
        {
            r.EntryBytes = new byte[16];
            for (int i = 0; i < 16; i++)
                r.EntryBytes[i] = GameMemory.Read<byte>(r.FunctionVA + i);
            sb.AppendLine($"[8] entry bytes  : {Hex(r.EntryBytes)}");
            sb.AppendLine($"    first byte = 0x{r.EntryBytes[0]:X2} " +
                (r.EntryBytes[0] == 0xE9 ? "(JMP trampoline — expected)" : "(UNEXPECTED — not a JMP entry)"));
            r.Ok = r.EntryBytes[0] == 0xE9;
        }
        else
        {
            sb.AppendLine("    function address not readable.");
        }

        r.Log = sb.ToString();
        return r;
    }

    /// <summary>
    /// Invokes the resolved function pointer with the supplied arguments,
    /// using the Win64 calling convention (RCX, RDX, R8, R9 + stack).
    ///
    /// WARNING: we do not know the real signature of ChrInsFactory::create.
    /// The static-analysis evidence proves the function EXISTS; the parameter
    /// list can only be recovered by dynamic analysis (breakpoint + register
    /// capture). Calling with NULL arguments will almost certainly dereference
    /// a null pointer inside the obfuscated overlay and crash the host process.
    /// An AccessViolation thrown inside native code is a Corrupted-State
    /// Exception that .NET cannot reliably swallow.
    /// </summary>
    public static unsafe (bool returned, nint result) TryCall(
        nint funcVA, nint a0, nint a1, nint a2, nint a3)
    {
        if (funcVA == 0) return (false, 0);
        var fn = (delegate* unmanaged<nint, nint, nint, nint, nint>)funcVA;
        nint result = fn(a0, a1, a2, a3);
        return (true, result);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static string ToAOB(string ascii)
    {
        var sb = new StringBuilder(ascii.Length * 3);
        for (int i = 0; i < ascii.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(((byte)ascii[i]).ToString("X2"));
        }
        return sb.ToString();
    }

    private static string ToAOB(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }

    private static string Hex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
