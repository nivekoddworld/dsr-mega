using System.Numerics;
using DS1Mod.Core;
using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Entry point for editing DSR game files. Wraps the boilerplate every patcher
/// repeats: resolve a path under the game dir, back it up once, decompress the
/// DCX, hand you the parsed object, then re-compress and write back (preserving
/// the original DCX type).
///
/// Preferred construction from an <c>IPatchContext</c> — wires conflict detection automatically:
/// <code>
///   var g = new GamePatch(ctx);
/// </code>
///
/// Everything below is designed so re-running is safe (idempotent) — see the
/// Fmg / ParamRepository / EmevdEditor helpers.
/// </summary>
public sealed class GamePatch
{
    public string GameDir { get; }
    private readonly Action<string> _backup;
    private readonly Action<string>? _log;
    private readonly Action<string, string>? _recordEdit;

    /// <summary>
    /// Construct from an <see cref="IPatchContext"/>. Wires backup, logging, and
    /// conflict-detection recording automatically.
    /// </summary>
    public GamePatch(IPatchContext ctx)
        : this(ctx.GameDir, ctx.BackupFile, ctx.Log)
    {
        _recordEdit = ctx.RecordEdit;
    }

    /// <summary>
    /// Low-level constructor. Prefer <see cref="GamePatch(IPatchContext)"/> where
    /// possible — conflict detection is only active when a recorder is wired.
    /// </summary>
    public GamePatch(string gameDir, Action<string> backupFile, Action<string>? log = null)
    {
        GameDir = gameDir;
        _backup = backupFile;
        _log = log;
    }

    public void Log(string msg) => _log?.Invoke(msg);

    private string Resolve(string rel) =>
        Path.Combine(GameDir, rel.Replace('/', Path.DirectorySeparatorChar));

    private void Record(string filePath, string selector) =>
        _recordEdit?.Invoke(filePath, selector);

    /// <summary>Edit one DCX-wrapped BND3 archive in place (script luabnd, a msgbnd, params…).</summary>
    public bool EditBnd3(string relPath, Action<BND3> edit)
    {
        string path = Resolve(relPath);
        if (!File.Exists(path)) { Log($"not found: {relPath}"); return false; }
        _backup(path);
        Record(path, $"BND3:{relPath}");
        byte[] dec = DCX.Decompress(path, out DCX.Type type);
        BND3 bnd = BND3.Read(dec);
        edit(bnd);
        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), type));
        return true;
    }

    /// <summary>
    /// Edit every archive named <paramref name="fileName"/> under <paramref name="relDir"/>
    /// (recursively) — e.g. all languages' menu.msgbnd.dcx. Returns the count edited.
    /// </summary>
    public int EditBnd3Glob(string relDir, string fileName, Action<BND3> edit)
    {
        string dir = Resolve(relDir);
        if (!Directory.Exists(dir)) { Log($"not found: {relDir}"); return 0; }
        int n = 0;
        foreach (string path in Directory.GetFiles(dir, fileName, SearchOption.AllDirectories))
        {
            _backup(path);
            Record(path, $"BND3:{Path.GetRelativePath(GameDir, path)}");
            byte[] dec = DCX.Decompress(path, out DCX.Type type);
            BND3 bnd = BND3.Read(dec);
            edit(bnd);
            File.WriteAllBytes(path, DCX.Compress(bnd.Write(), type));
            n++;
        }
        return n;
    }

    /// <summary>
    /// Edit a map's event script: <c>event/&lt;mapId&gt;.emevd.dcx</c>.
    /// </summary>
    public bool EditEmevd(string mapId, Action<EmevdEditor> edit)
    {
        string path = Resolve($"event/{mapId}.emevd.dcx");
        if (!File.Exists(path)) { Log($"emevd not found: {mapId}"); return false; }
        _backup(path);
        byte[] dec = DCX.Decompress(path, out DCX.Type type);
        EMEVD evd = EMEVD.Read(dec);
        var editor = new EmevdEditor(evd, (selector) => Record(path, selector));
        edit(editor);
        File.WriteAllBytes(path, DCX.Compress(evd.Write(), type));
        return true;
    }

    /// <summary>
    /// Compile and inject an NPC AI script using the <see cref="AiBuilder"/> C# DSL.
    /// <para>The builder emits Lua 5.0 source, compiles it with <see cref="Luac50"/>,
    /// and injects the bytecode into <c>script/&lt;mapId&gt;.luabnd.dcx</c>.</para>
    /// <code>
    /// g.EditAi("m18_01_00_00", "223200", ai => ai
    ///     .Goal("Battle", goal => goal
    ///         .OnActivate(q => q
    ///             .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 10)
    ///             .Attack(animId: 3008, cancelTime: 5))
    ///         .OnInterrupt(_ => true)));
    /// </code>
    /// </summary>
    /// <param name="mapId">Map identifier, e.g. <c>"m18_01_00_00"</c>.</param>
    /// <param name="npcFileId">The numeric NPC file ID, e.g. <c>"223200"</c>. Used for the .lua filename inside the luabnd.</param>
    /// <param name="build">Builder action.</param>
    /// <param name="luaId">
    /// Optional Lua identifier prefix for function names, e.g. <c>"AsylumSlam"</c>.
    /// Defaults to <c>"Npc" + npcFileId</c> when <c>npcFileId</c> starts with a digit.
    /// </param>
    public bool EditAi(string mapId, string npcFileId, Action<AiBuilder> build, string? luaId = null)
    {
        var builder = new AiBuilder();
        build(builder);
        string source   = builder.EmitLua(npcFileId, luaId);
        byte[] bytecode = Luac50.Compile(source);
        Log($"[AI] {npcFileId}: compiled {bytecode.Length} bytes");
        return EditBnd3($"script/{mapId}.luabnd.dcx",
            bnd => bnd.SetFileContaining($"{npcFileId}_battle.lua", bytecode));
    }

    /// <summary>
    /// Edit a map's MSB (map studio binary): <c>map/MapStudio/&lt;mapId&gt;.msb</c>.
    /// <code>
    /// g.EditMsb("m18_01_00_00", msb => msb
    ///     .PlaceTreasure(lotId: 8500, position: new(52f, -2f, 103f)));
    /// </code>
    /// </summary>
    public bool EditMsb(string mapId, Action<MsbEditor> edit)
    {
        string path = Resolve($"map/MapStudio/{mapId}.msb");
        if (!File.Exists(path)) { Log($"msb not found: {mapId}"); return false; }
        _backup(path);
        Record(path, $"MSB:{mapId}");
        MSB1 msb = MSB1.Read(path);
        edit(new MsbEditor(msb, mapId));
        msb.Write(path);
        return true;
    }

    /// <summary>
    /// Add a new goods item: writes a row in <c>EquipParamGoods</c> and three FMG
    /// strings (name, description, long description).
    /// </summary>
    /// <param name="paramdefBnd">DS1 paramdefbnd bytes — embed in your mod and pass here.</param>
    /// <param name="def">Item definition.</param>
    public void DefineGoods(byte[] paramdefBnd, ItemDef def)
    {
        // ── PARAM ──────────────────────────────────────────────────────────────
        EditParams(paramdefBnd, repo =>
        {
            repo.Edit("EquipParamGoods", p =>
            {
                if (p[def.Id] != null) return; // idempotent
                ParamRepository.AddClone(p, def.DonorId, def.Id, def.Name, row =>
                {
                    row["maxNum"].Value        = def.MaxCount;
                    row["refId_default"].Value = def.Id;
                    def.Configure?.Invoke(row);
                });
            });
        });

        // ── FMG ────────────────────────────────────────────────────────────────
        EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
        {
            SetFmgEntry(bnd, "GoodsName",    def.Id, def.Name);
            SetFmgEntry(bnd, "GoodsInfo",    def.Id, def.Description);
            SetFmgEntry(bnd, "GoodsCaption", def.Id, def.LongDesc);
        });
    }

    /// <summary>
    /// Add an ItemLotParam row so the item can be awarded via <c>AwardItemLot</c>
    /// or linked from a Treasure event.
    /// </summary>
    /// <param name="paramdefBnd">DS1 paramdefbnd bytes.</param>
    /// <param name="def">Lot definition.</param>
    public void DefineLot(byte[] paramdefBnd, LotDef def)
    {
        EditParams(paramdefBnd, repo =>
        {
            repo.Edit("ItemLotParam", p =>
            {
                if (p[def.LotId] != null) return; // idempotent

                // Use any existing lot row as donor (first row in table)
                int donorId = p.Rows[0].ID;
                ParamRepository.AddClone(p, donorId, def.LotId, $"lot_{def.LotId}", row =>
                {
                    // Clear all slots first, then set slot 0
                    foreach (var cell in row.Cells)
                        if (cell.Def.InternalName.StartsWith("lotItem"))
                            cell.Value = cell.Def.Default;

                    row["lotItemId01"].Value        = def.ItemId;
                    row["lotItemCategory01"].Value  = def.Category;
                    row["lotItemBasePoint01"].Value = (ushort)100;
                    row["lotItemNum01"].Value       = (byte)def.Count;

                    if (def.OnceOnlyFlag >= 0)
                        row["getItemFlagId"].Value = def.OnceOnlyFlag;
                });
            });
        });
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static void SetFmgEntry(BND3 bnd, string fmgName, int id, string text)
    {
        var file = bnd.Files.FirstOrDefault(
            f => Path.GetFileNameWithoutExtension(f.Name)
                      .Equals(fmgName, StringComparison.OrdinalIgnoreCase));
        if (file == null) return;
        var fmg = FMG.Read(file.Bytes);
        var entry = fmg.Entries.FirstOrDefault(e => e.ID == id);
        if (entry != null) entry.Text = text;
        else               fmg.Entries.Add(new FMG.Entry(id, text));
        file.Bytes = fmg.Write();
    }

    /// <summary>
    /// Edit the main param archive (<c>param/GameParam/GameParam.parambnd.dcx</c>).
    /// Pass the DS1 paramdefbnd bytes (the game doesn't ship them, so embed them
    /// in your mod and pass the embedded resource here).
    /// </summary>
    public bool EditParams(byte[] paramdefBnd, Action<ParamRepository> edit)
    {
        string path = Resolve("param/GameParam/GameParam.parambnd.dcx");
        if (!File.Exists(path)) { Log("GameParam.parambnd not found"); return false; }
        _backup(path);
        byte[] dec = DCX.Decompress(path, out DCX.Type type);
        BND3 bnd = BND3.Read(dec);
        var repo = new ParamRepository(bnd, ParamRepository.LoadDefs(paramdefBnd),
            (paramName, rowId) => Record(path, $"PARAM:{paramName}:{rowId}"));
        edit(repo);
        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), type));
        return true;
    }
}
