using System.Reflection;
using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.GoofyDemon;

/// <summary>
/// GOOFY DEMON. Replaces the Asylum Demon (entity 223200) AI with one that has
/// given up on being a boss, and prints its current "mood" live.
///
///   1. IGamePatcher.Patch() — at launch, swap the compiled 223200_battle.lua
///      inside script/m18_01_00_00.luabnd.dcx for our embedded bytecode.
///   2. OnTick() — the AI broadcasts its mood over event flags 11817000..09;
///      we poll them and report the active mood.
///
/// All output goes to BOTH the console window and a log file
/// (&lt;ModsDir&gt;/GoofyDemon.log), so it's visible even if the console isn't.
/// </summary>
public sealed class GoofyDemonMod : ModBase, IGamePatcher
{
    public override string Name    => "Goofy Demon";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    // Mood broadcast. Must match GoofyDemon_SetMood() in goofy_demon.lua.
    // m18_01 (area 181) local flags — chosen so the framework's event-flag
    // reader (group 1 / area 181) can actually decode them.
    private const int MoodFlagBase = 11817000;
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
    private string? _logPath;
    private int _lastMood = -1;
    private long _tick;

    // ── IGamePatcher ──────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        SetLogPath(ctx.ModsDir);

        string path = Path.Combine(ctx.GameDir, "script", LuaBnd);
        if (!File.Exists(path))
        {
            Log($"Patch: {path} not found — is the game UXM-extracted? Skipping.");
            return;
        }

        byte[] replacement = ReadEmbedded(ResourceLua);
        if (replacement.Length == 0) { Log("Patch: embedded bytecode missing — skipping."); return; }

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
            Log($"Patch: expected exactly 1 '{EntryLeaf}' entry, found {matched} — aborting (archive untouched).");
            return;
        }

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcxType));
        Log($"Patch: swapped {EntryLeaf} ({replacement.Length} bytes). The demon would rather dance than fight.");
    }

    // ── IGameMod ────────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        SetLogPath(ctx.ModsDir);
        Log("Loaded. Watching mood flags " +
            $"{MoodFlagBase}..{MoodFlagBase + MoodNames.Length - 1}.");

        // Self-test: prove the framework can read/write this flag range. If this
        // logs MISMATCH, the chosen flag IDs aren't decodable and the readout
        // won't work (this is exactly what was wrong with the old range).
        try
        {
            bool before = ctx.Reader.GetEventFlag(MoodFlagBase);
            ctx.Writer.SetEventFlag(MoodFlagBase, true);
            bool readBack = ctx.Reader.GetEventFlag(MoodFlagBase);
            ctx.Writer.SetEventFlag(MoodFlagBase, before);
            Log($"Flag self-test: wrote true to {MoodFlagBase}, read back {readBack} " +
                $"({(readBack ? "OK — flag range is readable" : "MISMATCH — flag range NOT readable!")}).");
        }
        catch (Exception e) { Log($"Flag self-test threw: {e.Message}"); }
    }

    public override void OnTick()
    {
        if (_ctx is null) return;
        _tick++;

        int mood = -1;
        for (int i = 0; i < MoodNames.Length; i++)
        {
            if (_ctx.Reader.GetEventFlag(MoodFlagBase + i)) { mood = i; break; }
        }

        // Mood changed → report it.
        if (mood >= 0 && mood != _lastMood)
        {
            _lastMood = mood;
            Log($"mood → {MoodNames[mood]}");
        }

        // Heartbeat every ~10s so it's obvious OnTick is running. If you see
        // heartbeats but never a "mood →", the AI isn't setting the flags
        // (wrong fight? archive not patched?). If you see no heartbeats at all,
        // OnTick isn't running (mod not loaded, or not in a loaded map).
        if (_tick % 20 == 0)
            Log($"(heartbeat #{_tick / 20}; current mood index = {mood})");
    }

    public override void OnUnload()
    {
        if (_ctx is null) return;
        for (int i = 0; i < MoodNames.Length; i++)
            _ctx.Writer.SetEventFlag(MoodFlagBase + i, false);
        Log("Unloaded; cleared mood flags.");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private void SetLogPath(string modsDir)
    {
        if (_logPath is not null) return;
        try { _logPath = Path.Combine(modsDir, "GoofyDemon.log"); } catch { _logPath = null; }
    }

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [GoofyDemon] {msg}";
        Console.WriteLine(line);
        try { if (_logPath is not null) File.AppendAllText(_logPath, line + Environment.NewLine); }
        catch { /* logging must never crash the game */ }
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
