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

    /// <summary>Edit a map's event script: <c>event/&lt;mapId&gt;.emevd.dcx</c>.</summary>
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
