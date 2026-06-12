using System.Text;

namespace DS1Mod.Core.Memory;

/// <summary>
/// MSVC RTTI resolver. DSR ships full RTTI for its engine classes, so the
/// vftable of any polymorphic class (e.g. <c>NS_FRPG::ChrIns</c>) can be
/// recovered at runtime from its mangled name — no hardcoded addresses.
///
/// Walk (validated in-process by the ChrInsFactory investigation):
///   1. Find the mangled name string in the module (unique with its NUL).
///   2. TypeDescriptor starts 0x10 before the name.
///   3. Find the Complete Object Locator: it stores the TypeDescriptor as a
///      4-byte RVA at COL+0x0C. The TD RVA also appears in BaseClassDescriptor
///      arrays, so each candidate is validated: a 64-bit COL has signature 1
///      at +0x00 and its own RVA at +0x14 (pSelf).
///   4. vftable[-1] holds the COL pointer; vftable = colRef + 8.
/// </summary>
public static class Rtti
{
    /// <summary>
    /// Resolve the vftable address of <paramref name="className"/> (the
    /// mangled RTTI name, e.g. <c>".?AVChrIns@NS_FRPG@@"</c>). Returns 0 if
    /// any step of the walk fails.
    /// </summary>
    public static nint FindVftable(string className)
    {
        List<(nint Vftable, int Offset)> all = FindVftables(className);
        if (all.Count == 0) return 0;
        // Prefer the primary vftable (sub-object offset 0) — that's the vptr
        // at the start of every instance.
        foreach ((nint vft, int off) in all)
            if (off == 0) return vft;
        return all[0].Vftable;
    }

    /// <summary>
    /// Resolve ALL vftables of <paramref name="className"/>, each with the
    /// offset of its vptr inside an instance. Classes with multiple
    /// inheritance have one vftable per base sub-object; an object scan that
    /// only looks for the primary vftable misses instances whose primary COL
    /// resolution picked a secondary one — and a hit on a secondary vftable
    /// is NOT the object base (subtract the offset to get it).
    /// </summary>
    public static List<(nint Vftable, int Offset)> FindVftables(string className)
    {
        var results = new List<(nint, int)>();

        // 1. mangled name (NUL-terminated so prefixes like ChrInsFactory don't match)
        nint nameVA = GameMemory.Scan(ToAOB(className + "\0"));
        if (nameVA == 0) return results;

        // 2. TypeDescriptor
        nint tdVA = nameVA - 0x10;
        uint tdRva = (uint)((ulong)tdVA - (ulong)GameMemory.ModuleBase);
        nint lo = GameMemory.ModuleBase, hi = GameMemory.ModuleBase + GameMemory.ModuleSize;

        // 3. every valid COL referencing this TD (one per vftable)
        foreach (nint rvaRef in GameMemory.ScanAll(ToAOB(BitConverter.GetBytes(tdRva)), max: 64))
        {
            nint col = rvaRef - 0x0C;
            if (!GameMemory.CanRead(col, 0x18)) continue;
            if (GameMemory.Read<uint>(col) != 1) continue;                       // 64-bit COL signature
            uint selfRva = GameMemory.Read<uint>(col + 0x14);                    // pSelf
            if (selfRva != (uint)((ulong)col - (ulong)GameMemory.ModuleBase)) continue;
            int subObjOffset = GameMemory.Read<int>(col + 0x04);                 // vptr offset in object

            // 4. vftable[-1] → COL (a COL is referenced by exactly one vftable)
            foreach (nint colRef in GameMemory.ScanAll(ToAOB(BitConverter.GetBytes((ulong)col)), max: 8))
            {
                nint vftable = colRef + 8;
                nint slot0 = GameMemory.Read<nint>(vftable);
                if (slot0 >= lo && slot0 < hi)
                    results.Add((vftable, subObjOffset));
            }
        }
        return results;
    }

    /// <summary>
    /// Reverse lookup: given a vftable pointer (e.g. an object's vptr), return
    /// the mangled RTTI class name, or null if the pointer doesn't lead to
    /// valid RTTI. Useful for identifying what class an unknown object is.
    /// </summary>
    public static string? GetClassName(nint vftable)
    {
        nint lo = GameMemory.ModuleBase, hi = GameMemory.ModuleBase + GameMemory.ModuleSize;
        if (vftable < lo || vftable >= hi) return null;

        nint col = GameMemory.Read<nint>(vftable - 8);
        if (col < lo || col >= hi || !GameMemory.CanRead(col, 0x18)) return null;
        if (GameMemory.Read<uint>(col) != 1) return null;
        if (GameMemory.Read<uint>(col + 0x14) != (uint)((ulong)col - (ulong)lo)) return null;

        nint td = lo + (nint)GameMemory.Read<uint>(col + 0x0C);
        nint name = td + 0x10;
        if (!GameMemory.CanRead(name, 64)) return null;

        Span<char> chars = stackalloc char[64];
        int n = 0;
        for (; n < 64; n++)
        {
            byte b = GameMemory.Read<byte>(name + n);
            if (b == 0) break;
            if (b < 0x20 || b > 0x7E) return null;   // not a printable mangled name
            chars[n] = (char)b;
        }
        return n == 0 ? null : new string(chars[..n]);
    }

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
}
