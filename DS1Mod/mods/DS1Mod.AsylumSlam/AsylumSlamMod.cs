using System.Reflection;
using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.AsylumSlam;

/// <summary>
/// Locks the Asylum Demon (entity 223200, "MiniGreaterDemon") to slam attacks
/// only — flying body slam (3007) from range, point-blank butt slam (3008) up
/// close.
///
/// Implemented as an <see cref="IGamePatcher"/>: at launch, before any map
/// loads, it swaps the compiled AI script <c>223200_battle.lua</c> inside
/// <c>script/m18_01_00_00.luabnd.dcx</c> for our slam-only version. The
/// replacement bytecode is precompiled Lua 5.0 (built with a DSR-compatible
/// luac) and embedded in this DLL, so no Lua compiler is needed at runtime.
///
/// The original archive is backed up (<c>.bak</c>) on first run, so removing
/// the mod and restoring the backup reverts cleanly.
/// </summary>
public sealed class AsylumSlamMod : ModBase, IGamePatcher
{
    public override string Name    => "Asylum Demon: Slam Only";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";  // Asylum Demon AI (see BossIds.AsylumDemon)
    private const string ResourceLua = "223200_battle.luac";

    public override void InitializeConfig(ModConfig config)
    {
        config.AddBool("enabled", true);
    }

    public void Patch(IPatchContext ctx)
    {
        // SoulsFormats reads shift-jis strings in BND entry names.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string path = Path.Combine(ctx.GameDir, "script", LuaBnd);
        if (!File.Exists(path))
        {
            ctx.Log($"[AsylumSlam] {path} not found — is the game UXM-extracted? Skipping.");
            return;
        }

        byte[] replacement = ReadEmbedded(ResourceLua);
        if (replacement.Length == 0)
        {
            ctx.Log("[AsylumSlam] embedded bytecode missing — skipping.");
            return;
        }

        // Back up the (current) archive before touching it. No-op if a .bak
        // already exists, so the very first backup preserves vanilla.
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
            ctx.Log($"[AsylumSlam] expected exactly 1 '{EntryLeaf}' entry, found {matched} — aborting (archive untouched).");
            return;
        }

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcxType));
        ctx.Log($"[AsylumSlam] patched {EntryLeaf} ({replacement.Length} bytes) in {LuaBnd}. " +
                "Asylum Demon will now only slam.");
    }

    private static byte[] ReadEmbedded(string logicalName)
    {
        Assembly asm = typeof(AsylumSlamMod).Assembly;
        using Stream? s = asm.GetManifestResourceStream(logicalName);
        if (s is null) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
