using System.Reflection;
using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.GoofyDemon;

/// <summary>
/// GOOFY DEMON. Replaces the Asylum Demon (entity 223200) AI with one that has
/// given up on being a boss: it mostly shimmies, breakdance-spins, sprints away
/// in terror, or stands around having an existential crisis — and only rarely
/// remembers to attack.
///
/// Same mechanism as DS1Mod.AsylumSlam: an <see cref="IGamePatcher"/> that, at
/// launch, swaps the compiled <c>223200_battle.lua</c> inside
/// <c>script/m18_01_00_00.luabnd.dcx</c> for our embedded bytecode and repacks
/// the archive (SoulsFormats). The vanilla archive is backed up first.
/// </summary>
public sealed class GoofyDemonMod : ModBase, IGamePatcher
{
    public override string Name    => "Goofy Demon";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    public void Patch(IPatchContext ctx)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string path = Path.Combine(ctx.GameDir, "script", LuaBnd);
        if (!File.Exists(path))
        {
            ctx.Log($"[GoofyDemon] {path} not found — is the game UXM-extracted? Skipping.");
            return;
        }

        byte[] replacement = ReadEmbedded(ResourceLua);
        if (replacement.Length == 0)
        {
            ctx.Log("[GoofyDemon] embedded bytecode missing — skipping.");
            return;
        }

        ctx.BackupFile(path);

        byte[] decompressed = DCX.Decompress(path, out DCX.Type dcxType);
        BND3 bnd = BND3.Read(decompressed);

        int matched = 0;
        foreach (BinderFile f in bnd.Files)
        {
            string leaf = Path.GetFileName(f.Name.Replace('\\', '/'));
            if (string.Equals(leaf, EntryLeaf, StringComparison.OrdinalIgnoreCase))
            {
                f.Bytes = replacement;
                matched++;
            }
        }

        if (matched != 1)
        {
            ctx.Log($"[GoofyDemon] expected exactly 1 '{EntryLeaf}' entry, found {matched} — aborting (archive untouched).");
            return;
        }

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcxType));
        ctx.Log($"[GoofyDemon] patched {EntryLeaf} ({replacement.Length} bytes). " +
                "The Asylum Demon would now rather dance than fight.");
    }

    private static byte[] ReadEmbedded(string logicalName)
    {
        Assembly asm = typeof(GoofyDemonMod).Assembly;
        using Stream? s = asm.GetManifestResourceStream(logicalName);
        if (s is null) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
