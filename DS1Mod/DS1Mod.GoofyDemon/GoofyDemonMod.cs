using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;
using ArgType = SoulsFormats.EMEVD.Instruction.ArgType;

namespace DS1Mod.GoofyDemon;

/// <summary>
/// GOOFY DEMON — the Asylum Demon (AI entity 223200, boss entity 1810800) has
/// given up on being a boss. This one mod does everything:
///
///   • AI swap   — 10 random "moods" (script/m18_01_00_00.luabnd.dcx).
///   • Mood HUD  — the AI broadcasts its mood over event flags 11815700..09;
///                 new EMEVD events (11819000..09) watch them and pop the mood
///                 name on-screen. Mood text lives in the Event_text FMG.
///   • Console   — the same mood, also printed to the mod console / log file.
///   • Fart      — a big "*farts*" message the instant he lands his entrance.
///
/// All file edits are surgical, idempotent, and backed up (SoulsFormats).
/// </summary>
public sealed class GoofyDemonMod : ModBase, IGamePatcher
{
    public override string Name    => "Goofy Demon";
    public override string Version => "1.2.0";
    public override string Author  => "DS1MegaRando";

    // ── AI swap ──
    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    // ── mood broadcast (must match goofy_demon.lua) ──
    private const int MoodFlagBase  = 11815700;   // AI sets one of these per cycle (section 5 — actually allocated)
    private const int MoodMsgBase   = 6900700;    // FMG ids for the HUD text
    private const int MoodEventBase = 11819000;   // new EMEVD watcher events

    // ── fart entrance ──
    private const int FartMsgId   = 6900690;
    private const int EntranceEvt = 11810310;
    private const int DemonEntity = 1810800;
    private const int JumpAnim    = 9060;

    // ── "Demon's Dignity (lost)" trinket, dropped when the demon dies ──
    private const int  DignityGoodsId = 8000;
    private const int  DignityLotId    = 8500;
    private const int  DignityGetFlag  = 50009000;   // dedicated "item obtained" range
    private const int  DemonDeadFlag   = 16;         // Asylum Demon kill flag
    private const long DignityEvent    = 11819100;
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

        try { PatchLua(ctx); }       catch (Exception e) { Log($"AI swap failed: {e.Message}"); }
        try { PatchFmgs(ctx); }      catch (Exception e) { Log($"HUD FMG patch failed: {e.Message}"); }
        try { PatchParams(ctx); }    catch (Exception e) { Log($"PARAM patch failed: {e.Message}"); }
        try { PatchItemFmg(ctx); }   catch (Exception e) { Log($"item FMG patch failed: {e.Message}"); }
        try { PatchEmevd(ctx); }     catch (Exception e) { Log($"EMEVD patch failed: {e.Message}"); }
    }

    // ── "Demon's Dignity" item: PARAMS (goods + item lot) ──────────────────────
    private void PatchParams(IPatchContext ctx)
    {
        string path = Path.Combine(ctx.GameDir, "param", "GameParam", "GameParam.parambnd.dcx");
        if (!File.Exists(path)) { Log("GameParam.parambnd not found — skipping dignity item."); return; }

        var defs = new Dictionary<string, PARAMDEF>();
        foreach (BinderFile f in BND3.Read(ReadEmbedded("paramdef.paramdefbnd.dcx")).Files)
            try { PARAMDEF d = PARAMDEF.Read(f.Bytes); defs[d.ParamType] = d; } catch { }
        if (defs.Count == 0) { Log("no paramdefs embedded — skipping dignity item."); return; }

        ctx.BackupFile(path);
        BND3 bnd = BND3.Read(DCX.Decompress(path, out DCX.Type dcx));

        void Edit(string name, Action<PARAM> edit)
        {
            foreach (BinderFile f in bnd.Files)
                if (BaseName(f.Name) == name)
                {
                    PARAM p = PARAM.Read(f.Bytes);
                    if (!defs.TryGetValue(p.ParamType, out PARAMDEF? def)) return;
                    p.ApplyParamdef(def); edit(p); f.Bytes = p.Write();
                }
        }

        Edit("EquipParamGoods", p =>
        {
            p.Rows.RemoveAll(r => r.ID == DignityGoodsId);
            PARAM.Row row = CloneRow(p, 384, DignityGoodsId, DignityName);  // 384 = a vanilla valuable
            row["maxNum"].Value = (ushort)1;
            p.Rows.Add(row); p.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));
        });
        Edit("ItemLotParam", p =>
        {
            p.Rows.RemoveAll(r => r.ID == DignityLotId);
            PARAM.Row row = CloneRow(p, 1000, DignityLotId, "Demon's Dignity drop");  // 1000 = a vanilla goods gift
            row["lotItemId01"].Value = DignityGoodsId;
            row["lotItemNum01"].Value = (byte)1;
            row["getItemFlagId"].Value = DignityGetFlag;
            p.Rows.Add(row); p.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));
        });

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
        Log($"PARAMS: dignity goods {DignityGoodsId} + lot {DignityLotId} added.");
    }

    private static PARAM.Row CloneRow(PARAM p, int donorId, int newId, string newName)
    {
        PARAM.Row src = p[donorId];
        var row = new PARAM.Row(newId, newName, p.AppliedParamdef);
        for (int i = 0; i < src.Cells.Count; i++) row.Cells[i].Value = src.Cells[i].Value;
        return row;
    }

    // ── "Demon's Dignity" item: name/description (item.msgbnd) ──────────────────
    private void PatchItemFmg(IPatchContext ctx)
    {
        string msgRoot = Path.Combine(ctx.GameDir, "msg");
        if (!Directory.Exists(msgRoot)) return;
        foreach (string path in Directory.GetFiles(msgRoot, "item.msgbnd.dcx", SearchOption.AllDirectories))
        {
            ctx.BackupFile(path);
            BND3 bnd = BND3.Read(DCX.Decompress(path, out DCX.Type dcx));
            void Put(string fmgName, string text)
            {
                foreach (BinderFile f in bnd.Files)
                    if (Path.GetFileName(f.Name.Replace('\\', '/')).Contains(fmgName))
                    {
                        FMG fmg = FMG.Read(f.Bytes);
                        fmg.Entries.RemoveAll(e => e.ID == DignityGoodsId);
                        fmg.Entries.Add(new FMG.Entry(DignityGoodsId, text));
                        f.Bytes = fmg.Write();
                    }
            }
            Put("Item_name", DignityName);
            Put("Item_description", DignityDesc);
            Put("Item_long_desc", DignityLong);
            File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
            Log($"item text: dignity @ {DignityGoodsId} into {Path.GetFileName(Path.GetDirectoryName(path))}/item.msgbnd.");
        }
    }

    private void PatchLua(IPatchContext ctx)
    {
        string path = Path.Combine(ctx.GameDir, "script", LuaBnd);
        if (!File.Exists(path)) { Log($"{LuaBnd} not found — skipping AI swap."); return; }
        byte[] lua = ReadEmbedded(ResourceLua);
        if (lua.Length == 0) { Log("embedded AI bytecode missing — skipping AI swap."); return; }

        ctx.BackupFile(path);
        byte[] dec = DCX.Decompress(path, out DCX.Type dcx);
        BND3 bnd = BND3.Read(dec);
        int hit = 0;
        foreach (BinderFile f in bnd.Files)
            if (string.Equals(Path.GetFileName(f.Name.Replace('\\', '/')), EntryLeaf, StringComparison.OrdinalIgnoreCase))
            { f.Bytes = lua; hit++; }
        if (hit != 1) { Log($"expected 1 '{EntryLeaf}', found {hit} — aborting AI swap."); return; }
        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
        Log($"AI: swapped {EntryLeaf} ({lua.Length} bytes).");
    }

    private void PatchFmgs(IPatchContext ctx)
    {
        string msgRoot = Path.Combine(ctx.GameDir, "msg");
        if (!Directory.Exists(msgRoot)) { Log("msg/ not found — skipping HUD text."); return; }
        foreach (string path in Directory.GetFiles(msgRoot, "menu.msgbnd.dcx", SearchOption.AllDirectories))
        {
            ctx.BackupFile(path);
            byte[] dec = DCX.Decompress(path, out DCX.Type dcx);
            BND3 bnd = BND3.Read(dec);
            int edits = 0;
            foreach (BinderFile f in bnd.Files)
            {
                if (!Path.GetFileName(f.Name).Contains("Event_text")) continue;
                FMG fmg = FMG.Read(f.Bytes);
                void Put(int id, string t) { fmg.Entries.RemoveAll(e => e.ID == id); fmg.Entries.Add(new FMG.Entry(id, t)); }
                Put(FartMsgId, "*farts*");
                for (int i = 0; i < MoodHud.Length; i++) Put(MoodMsgBase + i, MoodHud[i]);
                f.Bytes = fmg.Write();
                edits++;
            }
            File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
            Log($"HUD text: wrote fart + {MoodHud.Length} moods into {edits} Event_text copy(ies) of {Path.GetFileName(Path.GetDirectoryName(path))}.");
        }
    }

    private void PatchEmevd(IPatchContext ctx)
    {
        string path = Path.Combine(ctx.GameDir, "event", "m18_01_00_00.emevd.dcx");
        if (!File.Exists(path)) { Log("m18_01 emevd not found — skipping events."); return; }

        ctx.BackupFile(path);
        byte[] dec = DCX.Decompress(path, out DCX.Type dcx);
        EMEVD evd = EMEVD.Read(dec);

        // (a) fart Display Message after the 9060 landing
        EMEVD.Event? ent = evd.Events.FirstOrDefault(e => e.ID == EntranceEvt);
        if (ent != null && !ent.Instructions.Any(i => i.Bank == 2007 && i.ID == 4 && MsgId(i) == FartMsgId))
        {
            int at = -1;
            for (int i = 0; i < ent.Instructions.Count; i++)
            {
                EMEVD.Instruction ins = ent.Instructions[i];
                if (ins.Bank == 2003 && ins.ID == 18)
                {
                    var a = ins.UnpackArgs(new[] { ArgType.Int32, ArgType.Int32, ArgType.Byte, ArgType.Byte, ArgType.Byte });
                    if (Convert.ToInt32(a[0]) == DemonEntity && Convert.ToInt32(a[1]) == JumpAnim) { at = i + 1; break; }
                }
            }
            if (at >= 0)
            {
                ent.Instructions.Insert(at, new EMEVD.Instruction(2007, 4, new List<object> { FartMsgId, (byte)0 }));
                foreach (EMEVD.Parameter p in ent.Parameters) if (p.InstructionIndex >= at) p.InstructionIndex++;
                Log($"fart: Display Message inserted in event {EntranceEvt}.");
            }
        }

        // (b) 10 mood-watcher events + register them at the TOP of the
        //     constructor (event 0). It MUST be the top: event 0 has multiplayer
        //     SKIPs and an END IF partway through, so registrations appended at
        //     the end can be skipped/terminated and never run. Event 0 has no
        //     parameters, and SKIP offsets are relative, so prepending is safe.
        EMEVD.Event? ev0 = evd.Events.FirstOrDefault(e => e.ID == 0);
        // Rebuild the mood events every run (remove + re-add) so flag/message
        // changes take effect on already-patched installs.
        evd.Events.RemoveAll(e => e.ID >= MoodEventBase && e.ID < MoodEventBase + MoodHud.Length);
        int added = 0;
        for (int i = 0; i < MoodHud.Length; i++)
        {
            long eid = MoodEventBase + i; int flag = MoodFlagBase + i, msg = MoodMsgBase + i;
            var me = new EMEVD.Event(eid, EMEVD.Event.RestBehaviorType.Restart);
            me.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object> { (sbyte)0, (byte)1, (byte)0, flag })); // IF flag ON
            me.Instructions.Add(new EMEVD.Instruction(2007, 4, new List<object> { msg, (byte)0 }));                  // Display Message
            me.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object> { (sbyte)0, (byte)0, (byte)0, flag })); // IF flag OFF
            evd.Events.Add(me); added++;
        }
        int regs = 0;
        if (ev0 != null)
        {
            // drop stale mood registrations (incl. v1.1.0's broken end-appended
            // ones), then prepend fresh ones so they always initialize.
            ev0.Instructions.RemoveAll(x => x.Bank == 2000 && x.ID == 0 && IsMoodInit(x));
            var inits = new List<EMEVD.Instruction>();
            for (int i = 0; i < MoodHud.Length; i++)
                inits.Add(new EMEVD.Instruction(2000, 0, new List<object> { (int)0, (uint)(MoodEventBase + i), (uint)0 }));
            ev0.Instructions.InsertRange(0, inits);
            regs = inits.Count;
        }

        // (c) "Demon's Dignity" drop: a new event that Awards the item lot once
        //     the demon is dead (flag 16); registered at the top of event 0. The
        //     lot's getItemFlagId keeps it once-only.
        evd.Events.RemoveAll(e => e.ID == DignityEvent);
        var dignity = new EMEVD.Event(DignityEvent, EMEVD.Event.RestBehaviorType.Default);
        dignity.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object> { (sbyte)0, (byte)1, (byte)0, DemonDeadFlag })); // IF flag 16 ON
        dignity.Instructions.Add(new EMEVD.Instruction(2003, 4, new List<object> { DignityLotId }));                          // Award Item Lot
        evd.Events.Add(dignity);
        if (ev0 != null)
        {
            ev0.Instructions.RemoveAll(x => x.Bank == 2000 && x.ID == 0 && InitId(x) == DignityEvent);
            ev0.Instructions.Insert(0, new EMEVD.Instruction(2000, 0, new List<object> { (int)0, (uint)DignityEvent, (uint)0 }));
        }

        File.WriteAllBytes(path, DCX.Compress(evd.Write(), dcx));
        Log($"mood HUD: {added} new events, {regs} registrations. + dignity drop event {DignityEvent}.");
    }

    private static string BaseName(string nm)
    {
        nm = nm.Replace('\\', '/'); nm = nm.Substring(nm.LastIndexOf('/') + 1);
        int dot = nm.IndexOf('.'); return dot < 0 ? nm : nm.Substring(0, dot);
    }

    private static int MsgId(EMEVD.Instruction i) { try { return Convert.ToInt32(i.UnpackArgs(new[] { ArgType.Int32, ArgType.Byte })[0]); } catch { return -1; } }
    private static long InitId(EMEVD.Instruction i) { try { return Convert.ToInt64(i.UnpackArgs(new[] { ArgType.Int32, ArgType.UInt32, ArgType.UInt32 })[1]); } catch { return -1; } }
    private static bool IsMoodInit(EMEVD.Instruction i) { long id = InitId(i); return id >= MoodEventBase && id < MoodEventBase + 10; }

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
