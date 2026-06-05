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
    /// Add a new goods item: writes a row in <c>EquipParamGoods</c> and FMG strings
    /// (name, description, long description).
    /// </summary>
    /// <param name="paramdefBnd">DS1 paramdefbnd bytes — embedded in mod.</param>
    /// <param name="def">Item definition blueprint.</param>
    public void DefineGoods(byte[] paramdefBnd, ItemDef def)
    {
        // ─────────────────────────────────────────────────────────────
        // PARAM: EquipParamGoods
        // ─────────────────────────────────────────────────────────────
        EditParams(paramdefBnd, repo =>
        {
            repo.Edit("EquipParamGoods", p =>
            {
                if (p[def.Id] != null)
                    return; // idempotent

                ParamRepository.AddClone(p, def.DonorId, def.Id, def.Name, row =>
                {
                    // ── CORE IDENTITY ───────────────────────────────────
                    row["maxNum"].Value = def.MaxCount;

                    // SpEffect / behavior binding
                    if (def.SpEffectId >= 0)
                    {
                        row["refId"].Value = def.SpEffectId;
                        row["refCategory"].Value = (byte)1; // SpEffect
                    }
                    else
                    {
                        row["refId"].Value = def.Id;
                        row["refCategory"].Value = (byte)0;
                    }

                    // ── QUICK-USE FIX (CRITICAL DS1 RULE SET) ────────────
                    // DS1 does NOT respect isEquip alone — enforce full pipeline
                    if (def.AllowQuickUse)
                    {
                        row["isEquip"].Value = (byte)1;

                        // Force consumable classification unless explicitly overridden
                        if (def.GoodsType.HasValue)
                            row["goodsType"].Value = def.GoodsType.Value;
                        else if (def.SpEffectId >= 0)
                            row["goodsType"].Value = (byte)0; // Consumable default
                    }
                    else
                    {
                        row["isEquip"].Value = (byte)0;
                    }

                    // ── CONSUMPTION BEHAVIOR ─────────────────────────────
                    row["isConsume"].Value = def.IsConsume ? (byte)1 : (byte)0;

                    // ── STORAGE / WORLD RULES ────────────────────────────
                    row["isDeposit"].Value = def.IsDeposit ? (byte)1 : (byte)0;
                    row["isDrop"].Value = def.IsDrop ? (byte)1 : (byte)0;

                    // ── UI / INPUT BEHAVIOR ──────────────────────────────
                    if (def.UseAnim.HasValue)
                        row["goodsUseAnim"].Value = def.UseAnim.Value;

                    if (def.OpenMenuType.HasValue)
                        row["opmeMenuType"].Value = def.OpenMenuType.Value;

                    if (def.BehaviorId != 0)
                        row["behaviorId"].Value = def.BehaviorId;

                    // ── SORT / INVENTORY ────────────────────────────────
                    row["sortId"].Value = def.SortId;

                    // ── CUSTOM OVERRIDES ────────────────────────────────
                    def.Configure?.Invoke(row);
                });
            });
        });

        // ─────────────────────────────────────────────────────────────
        // FMG TEXT
        // ─────────────────────────────────────────────────────────────
        EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.GoodsName, def.Id, def.Name);
            Texts.Set(bnd, Texts.GoodsDescription, def.Id, def.Description);
            Texts.Set(bnd, Texts.GoodsLongDesc, def.Id, def.LongDesc);
        });
    }


    /// <summary>
    /// Add a new ItemLotParam row.
    /// Supports single-item and multi-slot drops.
    /// </summary>
    public void DefineLot(byte[] paramdefBnd, LotDef def)
    {
        EditParams(paramdefBnd, repo =>
        {
            repo.Edit("ItemLotParam", p =>
            {
                if (p[def.LotId] != null)
                    return;

                int donorId = p.Rows[0].ID;

                ParamRepository.AddClone(p, donorId, def.LotId, $"lot_{def.LotId}", row =>
                {
                    // ─────────────────────────────────────────────
                    // CLEAR ALL SLOT DATA FIRST
                    // ─────────────────────────────────────────────
                    foreach (var cell in row.Cells)
                    {
                        if (cell.Def.InternalName.StartsWith("lotItem") ||
                            cell.Def.InternalName.StartsWith("cumulate") ||
                            cell.Def.InternalName.StartsWith("getItemFlagId") ||
                            cell.Def.InternalName.StartsWith("enableLuck"))
                        {
                            cell.Value = cell.Def.Default;
                        }
                    }

                    // ─────────────────────────────────────────────
                    // SLOT POPULATION
                    // ─────────────────────────────────────────────
                    var entries = def.Entries;

                    if (entries != null && entries.Count > 0)
                    {
                        // Multi-slot support (01–08)
                        for (int i = 0; i < Math.Min(entries.Count, 8); i++)
                        {
                            var e = entries[i];
                            int idx = i + 1;

                            row[$"lotItemId{idx:00}"].Value = e.ItemId;
                            row[$"lotItemCategory{idx:00}"].Value = e.Category;
                            row[$"lotItemNum{idx:00}"].Value = e.Count;
                            row[$"lotItemBasePoint{idx:00}"].Value = e.Weight;
                        }
                    }
                    else
                    {
                        // Single-slot fallback
                        row["lotItemId01"].Value = def.ItemId;
                        row["lotItemCategory01"].Value = def.Category;
                        row["lotItemNum01"].Value = def.Count;
                        row["lotItemBasePoint01"].Value = def.Weight;
                    }

                    // ─────────────────────────────────────────────
                    // FLAGS
                    // ─────────────────────────────────────────────
                    if (def.OnceOnlyFlag >= 0)
                        row["getItemFlagId"].Value = def.OnceOnlyFlag;

                    // ─────────────────────────────────────────────
                    // LUCK / RARITY
                    // ─────────────────────────────────────────────
                    for (int i = 1; i <= 8; i++)
                    {
                        row[$"enableLuck{i:00}"].Value = def.EnableLuck ? (ushort)1 : (ushort)0;
                    }

                    row["lotItem_Rarity"].Value = def.Rarity;
                });
            });
        });
    }

    /// <summary>
    /// Creates or clones a row in <c>SpEffectParam</c>.
    ///
    /// SpEffects define gameplay effects such as:
    /// - healing / damage over time
    /// - stat buffs and debuffs
    /// - status buildup
    /// - special behavior triggers
    ///
    /// The method clones an existing donor row (usually a safe vanilla effect)
    /// and applies the values defined in <see cref="SpEffectDef"/>.
    /// </summary>
    /// <param name="paramdefBnd">
    /// DS1 paramdefbnd archive bytes containing SpEffectParam definitions.
    /// </param>
    /// <param name="def">
    /// SpEffect definition describing the effect to create.
    /// </param>
    public void DefineSpEffect(byte[] paramdefBnd, SpEffectDef def)
    {
        EditParams(paramdefBnd, repo =>
        {
            repo.Edit("SpEffectParam", p =>
            {
                if (p[def.Id] != null)
                {
                    Console.WriteLine($"[DEBUG] Row {def.Id} already exists in SpEffectParam. Skipping (Idempotent).");
                    return; // idempotent
                }

                int donorId = def.DonorId;
                if (p[donorId] == null)
                {
                    // Fall back to first available row rather than hard-failing —
                    // same approach DefineLot uses. Specific donor IDs (e.g. 110)
                    // may not exist in every DSR installation.
                    donorId = p.Rows[0].ID;
                    Console.WriteLine($"[WARN] Donor ID {def.DonorId} not found in SpEffectParam. Falling back to row {donorId}.");
                }

                Console.WriteLine($"[DEBUG] Cloning Donor {donorId} to new ID {def.Id}...");

                ParamRepository.AddClone(p, donorId, def.Id, $"sp_{def.Id}", row =>
                {
                    // ── core timing ─────────────────────────────────────────────
                    row["effectEndurance"].Value = def.Duration;

                    // ── healing effects ────────────────────────────────────────
                    if (def.HpRecoverPoint != 0)
                        row["changeHpPoint"].Value = def.HpRecoverPoint;

                    if (def.HpRecoverRate != 0)
                        row["hpRecoverRate"].Value = def.HpRecoverRate;

                    if (def.StaminaRecoverPoint != 0)
                        row["changeStaminaPoint"].Value = def.StaminaRecoverPoint;

                    // ── stat multipliers ───────────────────────────────────────
                    if (def.MaxHpRate != 1f)
                        row["maxHpRate"].Value = def.MaxHpRate;

                    if (def.PhysAtkPowerRate != 1f)
                        row["physicsAttackPowerRate"].Value = def.PhysAtkPowerRate;

                    if (def.MagicAtkPowerRate != 1f)
                        row["magicAttackPowerRate"].Value = def.MagicAtkPowerRate;

                    if (def.FireAtkPowerRate != 1f)
                        row["fireAttackPowerRate"].Value = def.FireAtkPowerRate;

                    if (def.ThunderAtkPowerRate != 1f)
                        row["thunderAttackPowerRate"].Value = def.ThunderAtkPowerRate;

                    if (def.PhysDefRate != 1f)
                        row["physicsDiffenceRate"].Value = def.PhysDefRate;

                    if (def.MagicDefRate != 1f)
                        row["magicDiffenceRate"].Value = def.MagicDefRate;

                    if (def.FireDefRate != 1f)
                        row["fireDiffenceRate"].Value = def.FireDefRate;

                    if (def.ThunderDefRate != 1f)
                        row["thunderDiffenceRate"].Value = def.ThunderDefRate;

                    def.Configure?.Invoke(row);
                    Console.WriteLine($"[DEBUG] Row {def.Id} configuration complete.");
                });
            });
        });
    }

    /// <summary>
    /// Write an EMEVD event that bridges item use (SpEffect activation) to an event flag,
    /// enabling in-process C# code to react via <c>hooks.RegisterItemUsed</c> or
    /// <c>reader.GetEventFlag(triggerFlagId)</c>.
    ///
    /// <para>The emitted event:</para>
    /// <list type="number">
    ///   <item>Waits until the player (entity 10000) has <paramref name="spEffectId"/> active</item>
    ///   <item>Sets <paramref name="triggerFlagId"/> ON</item>
    ///   <item>Waits until the SpEffect expires</item>
    ///   <item>Sets <paramref name="triggerFlagId"/> OFF and restarts — ready for the next use</item>
    /// </list>
    /// </summary>
    /// <param name="mapId">Map that owns the EMEVD, e.g. <c>"m18_01_00_00"</c>.</param>
    /// <param name="spEffectId">The SpEffect applied when the item is used.</param>
    /// <param name="triggerFlagId">Event flag to pulse ON for one SpEffect cycle.</param>
    /// <param name="eventId">EMEVD event ID. Defaults to <paramref name="triggerFlagId"/>.</param>
    public bool DefineItemTrigger(string mapId, int spEffectId, int triggerFlagId, int eventId = -1)
    {
        const int Player = 10000;
        if (eventId < 0) eventId = triggerFlagId;

        return EditEmevd(mapId, emevd =>
            emevd.DefineEvent(eventId, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenCharacterHasSpEffect(Player, spEffectId)
                .SetFlag(triggerFlagId, FlagState.On)
                .WhenCharacterLosesSpEffect(Player, spEffectId)
                .SetFlag(triggerFlagId, FlagState.Off)
                .Restart()));
    }

    /// <summary>
    /// Edit the main param archive (<c>param/GameParam/GameParam.parambnd.dcx</c>).
    /// Pass the DS1 paramdefbnd bytes (the game doesn't ship them, so embed them
    /// in your mod and pass the embedded resource here).
    /// </summary>
    public bool EditParams(byte[] paramdefBnd, Action<ParamRepository> edit)
    {
        string path = Resolve("param/GameParam/GameParam.parambnd.dcx");
        Console.WriteLine($"[DEBUG] Attempting to open: {path}");

        if (!File.Exists(path))
        {
            Console.WriteLine($"[ERROR] GameParam.parambnd not found at: {Path.GetFullPath(path)}");
            return false;
        }

        try
        {
            _backup(path);
            byte[] dec = DCX.Decompress(path, out DCX.Type type);
            Console.WriteLine($"[DEBUG] DCX decompressed. Type: {type} | Size: {dec.Length} bytes");

            BND3 bnd = BND3.Read(dec);
            Console.WriteLine($"[DEBUG] BND3 loaded. File count: {bnd.Files.Count}");

            var repo = new ParamRepository(bnd, ParamRepository.LoadDefs(paramdefBnd),
                (paramName, rowId) => Console.WriteLine($"[DEBUG] Record change: {paramName} ID {rowId}"));

            edit(repo);

            byte[] finalBytes = DCX.Compress(bnd.Write(), type);
            File.WriteAllBytes(path, finalBytes);
            Console.WriteLine($"[SUCCESS] Saved GameParam.parambnd.dcx to disk.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL ERROR] Failed to edit params: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return false;
        }
    }
}
