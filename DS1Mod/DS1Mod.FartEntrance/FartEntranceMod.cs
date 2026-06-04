using System.Text;
using DS1Mod.Core;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.FartEntrance;

/// <summary>
/// The Asylum Demon (boss entity 1810800) farts the instant he lands his
/// entrance. Two surgical edits at launch, both idempotent and backed up:
///
///   1. EMEVD — into the Undead Asylum's entrance event (11810310 in
///      event/m18_01_00_00.emevd.dcx), inserts a "Display Message" right after
///      the jump-down animation (9060), which has Wait-For-Completion set, so
///      the message fires the exact frame he touches down.
///   2. FMG — adds the line "*farts*" at a free message ID to the Event_text
///      FMG inside every msg/&lt;lang&gt;/menu.msgbnd.dcx, so the message has text.
///
/// Done with SoulsFormats (EMEVD + FMG), the same way the standalone edit was
/// built and round-trip-verified.
/// </summary>
public sealed class FartEntranceMod : ModBase, IGamePatcher
{
    public override string Name    => "Fart Entrance";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private const int    FartMsgId  = 6900690;
    private const string FartText   = "*farts*";
    private const int    EntranceEvent = 11810310;   // Asylum Demon intro
    private const int    DemonEntity   = 1810800;
    private const int    JumpAnim      = 9060;        // the jump-down landing

    private string? _logPath;

    public void Patch(IPatchContext ctx)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _logPath = SafeLogPath(ctx.ModsDir);

        try { PatchFmgs(ctx); }  catch (Exception e) { Log($"FMG patch failed: {e.Message}"); }
        try { PatchEmevd(ctx); } catch (Exception e) { Log($"EMEVD patch failed: {e.Message}"); }
    }

    // ── FMG: add "*farts*" to Event_text in every menu.msgbnd.dcx ──────────────
    private void PatchFmgs(IPatchContext ctx)
    {
        string msgRoot = Path.Combine(ctx.GameDir, "msg");
        if (!Directory.Exists(msgRoot)) { Log($"msg/ not found at {msgRoot} — skipping FMG."); return; }

        string[] bnds = Directory.GetFiles(msgRoot, "menu.msgbnd.dcx", SearchOption.AllDirectories);
        if (bnds.Length == 0) { Log("No menu.msgbnd.dcx found — skipping FMG."); return; }

        foreach (string path in bnds)
        {
            ctx.BackupFile(path);
            byte[] bytes = DCX.Decompress(path, out DCX.Type dcx);
            BND3 bnd = BND3.Read(bytes);
            int edits = 0;
            foreach (BinderFile f in bnd.Files)
            {
                if (!Path.GetFileName(f.Name).Contains("Event_text")) continue;
                FMG fmg = FMG.Read(f.Bytes);
                fmg.Entries.RemoveAll(e => e.ID == FartMsgId);   // idempotent
                fmg.Entries.Add(new FMG.Entry(FartMsgId, FartText));
                f.Bytes = fmg.Write();
                edits++;
            }
            File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcx));
            Log($"FMG: '{FartText}' @ {FartMsgId} into {edits} Event_text copy(ies) of {Path.GetFileName(Path.GetDirectoryName(path))}/menu.msgbnd.dcx");
        }
    }

    // ── EMEVD: insert Display Message after the 9060 landing ───────────────────
    private void PatchEmevd(IPatchContext ctx)
    {
        string path = Path.Combine(ctx.GameDir, "event", "m18_01_00_00.emevd.dcx");
        if (!File.Exists(path)) { Log($"{path} not found — skipping EMEVD."); return; }

        ctx.BackupFile(path);
        byte[] bytes = DCX.Decompress(path, out DCX.Type dcx);
        EMEVD evd = EMEVD.Read(bytes);

        EMEVD.Event? ev = evd.Events.FirstOrDefault(e => e.ID == EntranceEvent);
        if (ev is null) { Log($"Event {EntranceEvent} not found — skipping EMEVD."); return; }

        // Idempotent: bail if our Display Message is already present.
        if (ev.Instructions.Any(i => i.Bank == 2007 && i.ID == 4 && MsgIdOf(i) == FartMsgId))
        {
            Log("EMEVD already patched — skipping.");
            return;
        }

        int insertAt = -1;
        for (int i = 0; i < ev.Instructions.Count; i++)
        {
            EMEVD.Instruction ins = ev.Instructions[i];
            if (ins.Bank == 2003 && ins.ID == 18) // Force Animation Playback
            {
                var a = ins.UnpackArgs(new[]
                {
                    EMEVD.Instruction.ArgType.Int32, EMEVD.Instruction.ArgType.Int32,
                    EMEVD.Instruction.ArgType.Byte, EMEVD.Instruction.ArgType.Byte, EMEVD.Instruction.ArgType.Byte,
                });
                if (Convert.ToInt32(a[0]) == DemonEntity && Convert.ToInt32(a[1]) == JumpAnim) { insertAt = i + 1; break; }
            }
        }
        if (insertAt < 0) { Log($"Landing animation {JumpAnim} not found in event {EntranceEvent} — skipping EMEVD."); return; }

        // Display Message (2007:4): Message ID (int32), Screen Location Index (byte=0)
        ev.Instructions.Insert(insertAt, new EMEVD.Instruction(2007, 4, new List<object> { FartMsgId, (byte)0 }));
        foreach (EMEVD.Parameter p in ev.Parameters)
            if (p.InstructionIndex >= insertAt) p.InstructionIndex++;

        File.WriteAllBytes(path, DCX.Compress(evd.Write(), dcx));
        Log($"EMEVD: Display Message({FartMsgId}) inserted at index {insertAt} in event {EntranceEvent} (right after anim {JumpAnim}).");
    }

    private static int MsgIdOf(EMEVD.Instruction ins)
    {
        try
        {
            var a = ins.UnpackArgs(new[] { EMEVD.Instruction.ArgType.Int32, EMEVD.Instruction.ArgType.Byte });
            return Convert.ToInt32(a[0]);
        }
        catch { return -1; }
    }

    // ── logging ────────────────────────────────────────────────────────────────
    private static string? SafeLogPath(string modsDir)
    {
        try { return Path.Combine(modsDir, "FartEntrance.log"); } catch { return null; }
    }

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [FartEntrance] {msg}";
        Console.WriteLine(line);
        try { if (_logPath is not null) File.AppendAllText(_logPath, line + Environment.NewLine); } catch { }
    }
}
