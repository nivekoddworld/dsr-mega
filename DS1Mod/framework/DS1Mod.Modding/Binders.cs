using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>BND3 helpers — finding entries by name despite Windows `\` separators.</summary>
public static class Binders
{
    /// <summary>Leaf file name of a binder entry (handles `\` separators on any OS).</summary>
    public static string Leaf(string name)
    {
        name = name.Replace('\\', '/');
        return name.Substring(name.LastIndexOf('/') + 1);
    }

    /// <summary>Leaf name without extension(s), e.g. "EquipParamGoods" from "...\EquipParamGoods.param".</summary>
    public static string BaseName(string name)
    {
        string leaf = Leaf(name);
        int dot = leaf.IndexOf('.');
        return dot < 0 ? leaf : leaf.Substring(0, dot);
    }

    /// <summary>First entry whose leaf name contains <paramref name="nameContains"/>, or null.</summary>
    public static BinderFile? FileContaining(this IBinder bnd, string nameContains) =>
        bnd.Files.FirstOrDefault(f => Leaf(f.Name).Contains(nameContains));

    /// <summary>Replace the bytes of the (single) entry whose leaf name contains <paramref name="nameContains"/>.
    /// Returns true if exactly one entry matched.</summary>
    public static bool SetFileContaining(this IBinder bnd, string nameContains, byte[] bytes)
    {
        var matches = bnd.Files.Where(f => Leaf(f.Name).Contains(nameContains)).ToList();
        if (matches.Count != 1) return false;
        matches[0].Bytes = bytes;
        return true;
    }
}
