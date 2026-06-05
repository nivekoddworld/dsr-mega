using System.Text;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.GoofyDemon;

/// <summary>
/// GOOFY DEMON — the Asylum Demon (AI entity 223200, boss entity 1810800) has
/// given up on being a boss. One mod, everything, built on the DS1Mod.Modding
/// helper library:
///
///   • AI swap   — 10 random "moods" (script/m18_01_00_00.luabnd.dcx).
///   • Mood HUD  — the AI broadcasts its mood over event flags 11815700..09;
///                 new EMEVD events (11819000..09) watch them and pop the mood
///                 name on-screen. Mood text lives in the Event_text FMG.
///   • Console   — the same mood, also printed to the mod console / log file.
///   • Fart      — a big "*farts*" message the instant he lands his entrance.
///   • Dignity   — drops the "Demon's Dignity (lost)" trinket when he dies.
///
/// All file edits are surgical, idempotent, and backed up (via GamePatch).
/// </summary>
public sealed class GoofyDemonMod : ModBase, IGamePatcher
{
    public override string Name    => "Goofy Demon";
    public override string Version => "1.3.0";
    public override string Author  => "DS1MegaRando";

    // ── AI swap ──
    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    // ── Allocated IDs (assigned at patch time from IPatchContext) ────────────

    // Mood broadcast (10 flags + 10 messages + 10 events = 1 & 10 ranges)
    private int MoodFlagBase;
    private int MoodMsgBase;
    private int MoodEventBase;

    // Fart entrance
    private int FartMsgId;

    // "Demon's Dignity (lost)" trinket
    private int DignityGoodsId;
    private int DignityLotId;
    private int DignityGetFlag;
    private long DignityEvent;

    // Constants (no allocation needed)
    private const int EntranceEvt = 11810310;
    private const int DemonEntity = 1810800;
    private const int JumpAnim    = 9060;
    private const int DemonDeadFlag = 16;
    private const string DignityName = "Demon's Dignity (lost)";
    private const string DignityDesc = "All that remains of a demon's self-respect.";
    private const string DignityLong = "The dignity of the Asylum Demon, irretrievably lost the day he chose "
                                     + "the hokey pokey over honest violence.\n\nWeighs nothing. Worth nothing. "
                                     + "He will never get it back. Now, neither will you.";

    // HUD text (game font — ASCII, no emoji). Indexed by mood.
    private static readonly string[] MoodHud =
    {
        "the demon shimmies", "the demon breakdances", "the demon flees in terror",
        "the demon questions its existence", "SURPRISE ATTACK", "the demon remembers it is a boss",
        "the demon has the zoomies", "the demon does the hokey pokey", "the demon has stage fright",
        "premature victory lap",
    };

    // Console/log text (console can show emoji). Indexed by mood.
    private static readonly string[] MoodConsole =
    {
        "💃 The Shimmy", "🕺 The Breakdance", "😱 The Coward (fleeing)", "🤔 Existential Crisis",
        "😈 SURPRISE ATTACK!", "👊 Fine. Fighting.", "🌀 The Zoomies", "🦵 Hokey Pokey",
        "😶 Stage Fright", "🏆 Victory Lap",
    };

    private IModContext? _ctx;
    private string? _logPath;
    private int _lastMood = -1;
    private long _tick;

    // ── IGamePatcher ──────────────────────────────────────────────────────────
    public void Patch(IPatchContext ctx)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _logPath = SafeLog(ctx.ModsDir);

        // ════════════════════════════════════════════════════════════════════
        // API: ID allocation
        // Request contiguous blocks of IDs to guarantee no conflicts with
        // other mods. Allocations are persistent (same mod always gets same
        // IDs) for save-game compatibility.
        // ════════════════════════════════════════════════════════════════════

        // Mood system: 10 flags + 10 messages + 10 events
        MoodFlagBase  = ctx.AllocateIds(IdSpaces.EventFlags(IdSpaces.Asylum), 10);
        MoodMsgBase   = ctx.AllocateIds(IdSpaces.EventText, 10);
        MoodEventBase = ctx.AllocateIds(IdSpaces.EmevdEvents(IdSpaces.Asylum), 10);

        // Fart message (1 FMG entry)
        FartMsgId     = ctx.AllocateId(IdSpaces.EventText);

        // Dignity trinket: 1 goods + 1 lot + 1 once-only flag + 1 event
        DignityGoodsId = ctx.AllocateId(IdSpaces.EquipParamGoods);
        DignityLotId   = ctx.AllocateId(IdSpaces.ItemLotParam);
        DignityGetFlag = ctx.AllocateId(IdSpaces.ItemObtainedFlags);
        DignityEvent   = ctx.AllocateIds(IdSpaces.EmevdEvents(IdSpaces.Asylum), 1);

        var g = new GamePatch(ctx);

        // 1) AI swap — drop our compiled bytecode into the luabnd.
        byte[] lua = ReadEmbedded(ResourceLua);
        if (lua.Length > 0)
            g.EditBnd3($"script/{LuaBnd}", bnd =>
                Log(bnd.SetFileContaining(EntryLeaf, lua) ? $"AI: swapped {EntryLeaf}" : $"AI: '{EntryLeaf}' not found"));

        // 2) HUD + fart text → Event_text FMG in every menu.msgbnd.
        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.EventText, FartMsgId, "*farts*");
            for (int i = 0; i < MoodHud.Length; i++) Texts.Set(bnd, Texts.EventText, MoodMsgBase + i, MoodHud[i]);
        });

        // 3) dignity name/description → item.msgbnd.
        g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.GoodsName, DignityGoodsId, DignityName);
            Texts.Set(bnd, Texts.GoodsDescription, DignityGoodsId, DignityDesc);
            Texts.Set(bnd, Texts.GoodsLongDesc, DignityGoodsId, DignityLong);
        });

        // 4) params — the dignity item (goods) + its once-only drop lot.
        byte[] defs = ReadEmbedded("paramdef.paramdefbnd.dcx");
        if (defs.Length > 0)
            g.EditParams(defs, repo =>
            {
                repo.Edit("EquipParamGoods", p =>
                    ParamRepository.AddClone(p, 384, DignityGoodsId, DignityName, r => r["maxNum"].Value = (ushort)1));
                repo.Edit("ItemLotParam", p =>
                    ParamRepository.AddClone(p, 1000, DignityLotId, "Demon's Dignity drop", r =>
                    {
                        r["lotItemId01"].Value = DignityGoodsId;
                        r["lotItemNum01"].Value = (byte)1;
                        r["getItemFlagId"].Value = DignityGetFlag;
                    }));
            });

        // 5) events — fart, the 10 mood watchers, and the death-drop.
        g.EditEmevd("m18_01_00_00", e =>
        {
            e.InsertAfter(EntranceEvt, Instr.IsForceAnimation(DemonEntity, JumpAnim),
                Instr.DisplayMessage(FartMsgId), alreadyPresent: Instr.IsDisplayMessage(FartMsgId));

            for (int i = 0; i < MoodHud.Length; i++)
                e.DefineEvent(MoodEventBase + i, EMEVD.Event.RestBehaviorType.Restart,
                    Instr.IfEventFlag(FlagState.On,  MoodFlagBase + i),
                    Instr.DisplayMessage(MoodMsgBase + i),
                    Instr.IfEventFlag(FlagState.Off, MoodFlagBase + i));

            e.DefineEvent(DignityEvent, EMEVD.Event.RestBehaviorType.Default,
                Instr.IfEventFlag(FlagState.On, DemonDeadFlag),
                Instr.AwardItemLot(DignityLotId));
        });

        Log("patch complete.");
    }

    // ── IGameMod (console readout) ──────────────────────────────────────────────
    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        _logPath = SafeLog(ctx.ModsDir);
        Log($"Loaded. Watching mood flags {MoodFlagBase}..{MoodFlagBase + MoodConsole.Length - 1}.");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;
        _tick++;
        int mood = -1;
        for (int i = 0; i < MoodConsole.Length; i++)
            if (_ctx.Reader.GetEventFlag(MoodFlagBase + i)) { mood = i; break; }
        if (mood >= 0 && mood != _lastMood) { _lastMood = mood; Log($"mood → {MoodConsole[mood]}"); }
        if (_tick % 20 == 0) Log($"(heartbeat #{_tick / 20}; current mood index = {mood})");
    }

    public override void OnUnload()
    {
        if (_ctx is null) return;
        for (int i = 0; i < MoodConsole.Length; i++) _ctx.Writer.SetEventFlag(MoodFlagBase + i, false);
        Log("Unloaded; cleared mood flags.");
    }

    // ── helpers ──
    private static string? SafeLog(string modsDir) { try { return Path.Combine(modsDir, "GoofyDemon.log"); } catch { return null; } }
    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [GoofyDemon] {msg}";
        Console.WriteLine(line);
        try { if (_logPath is not null) File.AppendAllText(_logPath, line + Environment.NewLine); } catch { }
    }

    private static byte[] ReadEmbedded(string logicalName)
    {
        using Stream? s = typeof(GoofyDemonMod).Assembly.GetManifestResourceStream(logicalName);
        if (s is null) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
