using System.Reflection;
using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.GoofyDemon;

/// <summary>
/// GOOFY DEMON. Replaces the Asylum Demon (entity 223200) AI with one that has
/// given up on being a boss: it mostly shimmies, breakdance-spins, sprints away
/// in terror, gets the zoomies, does the hokey pokey, freezes with stage
/// fright, runs a premature victory lap, has existential crises — and only
/// rarely actually attacks.
///
/// Two jobs:
///   1. IGamePatcher.Patch() — at launch, swap the compiled 223200_battle.lua
///      inside script/m18_01_00_00.luabnd.dcx for our embedded bytecode and
///      repack (SoulsFormats). The vanilla archive is backed up first.
///   2. OnTick() — the demon's AI broadcasts its current mood over an unused
///      event-flag block each cycle; we poll it and print the live mood to the
///      mod console window.
/// </summary>
public sealed class GoofyDemonMod : ModBase, IGamePatcher
{
    public override string Name    => "Goofy Demon";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    // Mood broadcast: the AI clears all ten flags then sets exactly one each
    // cycle. Must match GoofyDemon_SetMood() in goofy_demon.lua. Unused Kiln
    // range per FogMod (vanilla never reads these).
    private const int MoodFlagBase = 15105610;
    private static readonly string[] MoodNames =
    {
        "💃 The Shimmy",          // 0
        "🕺 The Breakdance",      // 1
        "😱 The Coward (fleeing)",// 2
        "🤔 Existential Crisis",  // 3
        "😈 SURPRISE ATTACK!",    // 4
        "👊 Fine. Fighting.",     // 5
        "🌀 The Zoomies",         // 6
        "🦵 Hokey Pokey",         // 7
        "😶 Stage Fright",        // 8
        "🏆 Victory Lap",         // 9
    };

    private IModContext? _ctx;
    private int _lastMood = -1;

    // ── IGamePatcher ──────────────────────────────────────────────────────────

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

    // ── IGameMod ────────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        Console.WriteLine("[GoofyDemon] Loaded. Mood readout will appear here once " +
                          "you enter the Asylum Demon fight.");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;

        // Find which single mood flag is currently set.
        int mood = -1;
        for (int i = 0; i < MoodNames.Length; i++)
        {
            if (_ctx.Reader.GetEventFlag(MoodFlagBase + i))
            {
                mood = i;
                break;
            }
        }

        if (mood >= 0 && mood != _lastMood)
        {
            _lastMood = mood;
            Console.WriteLine($"[GoofyDemon] mood → {MoodNames[mood]}");
        }
    }

    public override void OnUnload()
    {
        // Tidy up: clear the broadcast flags so they don't linger in the save.
        if (_ctx is null) return;
        for (int i = 0; i < MoodNames.Length; i++)
            _ctx.Writer.SetEventFlag(MoodFlagBase + i, false);
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
