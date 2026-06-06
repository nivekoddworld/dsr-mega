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
    private readonly IPatchContext? _ctx;

    /// <summary>
    /// Construct from an <see cref="IPatchContext"/>. Wires backup, logging, and
    /// conflict-detection recording automatically.
    /// </summary>
    public GamePatch(IPatchContext ctx)
        : this(ctx.GameDir, ctx.BackupFile, ctx.Log)
    {
        _recordEdit = ctx.RecordEdit;
        _ctx = ctx;
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
    /// Edit a named ESD state machine inside a <c>.esdbnd.dcx</c> archive.
    ///
    /// DSR talk scripts live at <c>script/talk/t{id}.talkesdbnd.dcx</c>.
    /// Each archive contains one or more <c>.esd</c> files identified by name.
    ///
    /// <code>
    /// g.EditEsd("script/talk/t200000.talkesdbnd.dcx", "200000.esd", esd =>
    /// {
    ///     esd.AddTransition(groupId: 7, fromState: 1, toState: 10,
    ///         evaluator: EsdBytecode.Always());
    /// });
    /// </code>
    /// </summary>
    /// <param name="relPath">Path to the <c>.esdbnd.dcx</c> relative to the game directory.</param>
    /// <param name="esdName">
    /// Name of the <c>.esd</c> file inside the archive (case-insensitive suffix match, e.g.
    /// <c>"200000.esd"</c>). Pass <c>null</c> to edit the first file in the bundle.
    /// </param>
    /// <param name="edit">Callback that receives an <see cref="EsdEditor"/> wrapping the parsed ESD.</param>
    public bool EditEsd(string relPath, string? esdName, Action<EsdEditor> edit)
    {
        string path = Resolve(relPath);
        if (!File.Exists(path)) { Log($"esd not found: {relPath}"); return false; }
        _backup(path);
        Record(path, $"ESD:{relPath}:{esdName ?? "*"}");

        byte[] dec = DCX.Decompress(path, out DCX.Type type);
        BND3 bnd = BND3.Read(dec);

        BinderFile? file = esdName is null
            ? bnd.Files.FirstOrDefault()
            : bnd.Files.FirstOrDefault(f =>
                f.Name.EndsWith(esdName, StringComparison.OrdinalIgnoreCase));

        if (file is null)
        {
            Log($"esd entry '{esdName}' not found in {relPath}");
            return false;
        }

        ESD esd = ESD.Read(file.Bytes);
        edit(new EsdEditor(esd));
        file.Bytes = esd.Write();

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), type));
        return true;
    }

    /// <summary>
    /// Edit a player or enemy action ESD file: <c>chr/c0000.esd.dcx</c> (player)
    /// or <c>chr/enemyCommon.esd.dcx</c> (all enemies).
    ///
    /// Action ESD controls all in-game animations and state transitions: idle, attack,
    /// dodge, fall, sit at bonfire, cast spell, etc. Each state's conditions check
    /// for input (attack button held, roll requested), timing (animation duration),
    /// and world state (stamina, airborne, stunned) to decide the next action.
    ///
    /// <code>
    /// // Make the player unable to attack while stunned:
    /// g.EditActionEsd("c0000", esd =>
    /// {
    ///     var idleState = esd.GetOrAddState(groupId: 0, stateId: 0);
    ///     idleState.Conditions.Insert(0, new ESD.Condition(0,
    ///         ActionEsdBytecode.IsStunned()));
    /// });
    /// </code>
    /// </summary>
    /// <param name="esd">
    /// Base filename without extension: <c>"c0000"</c> for player,
    /// <c>"enemyCommon"</c> for all enemies. The method resolves to
    /// <c>chr/{esd}.esd.dcx</c> automatically.
    /// </param>
    /// <param name="edit">Callback that receives an <see cref="ActionEsdEditor"/> wrapping the parsed ESD.</param>
    public bool EditActionEsd(string esd, Action<ActionEsdEditor> edit)
    {
        string relPath = $"chr/{esd}.esd.dcx";
        string path = Resolve(relPath);
        if (!File.Exists(path)) { Log($"action esd not found: {relPath}"); return false; }
        _backup(path);
        Record(path, $"ActionESD:{relPath}");

        byte[] dec = DCX.Decompress(path, out DCX.Type type);
        ESD esdObj = ESD.Read(dec);
        edit(new ActionEsdEditor(esdObj));
        File.WriteAllBytes(path, DCX.Compress(esdObj.Write(), type));
        return true;
    }

    /// <summary>
    /// Edit every <c>.esd</c> entry whose raw byte length equals
    /// <paramref name="vanillaSize"/> across all <c>.esdbnd.dcx</c> files found
    /// recursively under <paramref name="relDir"/>.
    /// Returns the number of ESD entries edited.
    ///
    /// This is the programmatic equivalent of the DS1 Mega Randomizer's bonfire
    /// ESD patching, which detects all bonfire ESDs by their vanilla size (23012)
    /// and applies uniform changes:
    /// <code>
    /// // Remove the event-flag gate from "Level Up" in every bonfire ESD:
    /// g.EditEsdBySize("script/talk", vanillaSize: 23012, esd =>
    ///     esd.SetTalkListGateFlag(groupId: 1, stateId: 4, talkId: 15000100, newGateFlag: -1));
    /// </code>
    /// </summary>
    public int EditEsdBySize(string relDir, int vanillaSize, Action<EsdEditor> edit)
    {
        string dir = Resolve(relDir);
        if (!Directory.Exists(dir)) { Log($"not found: {relDir}"); return 0; }

        int count = 0;
        foreach (string path in Directory.GetFiles(dir, "*.esdbnd.dcx", SearchOption.AllDirectories))
        {
            byte[] dec = DCX.Decompress(path, out DCX.Type type);
            BND3 bnd = BND3.Read(dec);

            bool changed = false;
            foreach (BinderFile file in bnd.Files)
            {
                if (file.Bytes.Length != vanillaSize) continue;

                _backup(path);
                Record(path, $"ESD:{Path.GetRelativePath(GameDir, path)}:{Path.GetFileName(file.Name)}");
                ESD esd = ESD.Read(file.Bytes);
                edit(new EsdEditor(esd));
                file.Bytes = esd.Write();
                changed = true;
                count++;
            }

            if (changed)
                File.WriteAllBytes(path, DCX.Compress(bnd.Write(), type));
        }
        return count;
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
                // Helper: apply all ItemDef fields to an existing or newly cloned row.
                // refCategory=2 is the correct value for SpEffectParam lookup — refCategory=1
                // references a weapon-buff/behavior table (used by Dragon Stones, Pine Resin).
                static void ApplyDef(PARAM.Row row, ItemDef def)
                {
                    row["maxNum"].Value = def.MaxCount;

                    if (def.SpEffectId >= 0)
                    {
                        row["refId"].Value = def.SpEffectId;
                        row["refCategory"].Value = (byte)2; // SpEffectParam lookup
                    }
                    else
                    {
                        row["refId"].Value = def.Id;
                        row["refCategory"].Value = (byte)0;
                    }

                    if (def.AllowQuickUse)
                    {
                        row["isEquip"].Value = (byte)1;
                        if (def.GoodsType.HasValue)
                            row["goodsType"].Value = def.GoodsType.Value;
                        else if (def.SpEffectId >= 0)
                            row["goodsType"].Value = (byte)0;
                    }
                    else
                    {
                        row["isEquip"].Value = (byte)0;
                    }

                    row["isConsume"].Value = def.IsConsume ? (byte)1 : (byte)0;
                    row["isDeposit"].Value = def.IsDeposit ? (byte)1 : (byte)0;
                    row["isDrop"].Value = def.IsDrop ? (byte)1 : (byte)0;

                    if (def.UseAnim.HasValue)
                        row["goodsUseAnim"].Value = def.UseAnim.Value;
                    if (def.OpenMenuType.HasValue)
                        row["opmeMenuType"].Value = def.OpenMenuType.Value;
                    if (def.BehaviorId != 0)
                        row["behaviorId"].Value = def.BehaviorId;

                    row["sortId"].Value = def.SortId;
                    def.Configure?.Invoke(row);
                }

                if (p[def.Id] != null)
                {
                    // Update existing row in-place so parameter changes take effect
                    // on redeploy without requiring a vanilla restore.
                    Console.WriteLine($"[DEBUG] EquipParamGoods row {def.Id} exists — updating fields.");
                    ApplyDef(p[def.Id]!, def);
                    return;
                }

                ParamRepository.AddClone(p, def.DonorId, def.Id, def.Name, row => ApplyDef(row, def));
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
                // Helper that applies all SpEffectDef fields to an existing or newly cloned row.
                static void ApplyDef(PARAM.Row row, SpEffectDef def)
                {
                    row["effectEndurance"].Value = def.Duration;

                    if (def.HpRecoverPoint != 0)
                        row["changeHpPoint"].Value = def.HpRecoverPoint;
                    if (def.HpRecoverRate != 0)
                        row["hpRecoverRate"].Value = def.HpRecoverRate;
                    if (def.StaminaRecoverPoint != 0)
                        row["changeStaminaPoint"].Value = def.StaminaRecoverPoint;

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
                }

                if (p[def.Id] != null)
                {
                    // Row already exists — update it in-place so that parameter changes
                    // take effect on redeploy without requiring a vanilla restore.
                    Console.WriteLine($"[DEBUG] Row {def.Id} exists in SpEffectParam — updating fields.");
                    ApplyDef(p[def.Id]!, def);
                    return;
                }

                int donorId = def.DonorId;
                if (p[donorId] == null)
                {
                    donorId = p.Rows[0].ID;
                    Console.WriteLine($"[WARN] Donor ID {def.DonorId} not found in SpEffectParam. Falling back to row {donorId}.");
                }

                Console.WriteLine($"[DEBUG] Cloning Donor {donorId} to new ID {def.Id}...");
                ParamRepository.AddClone(p, donorId, def.Id, $"sp_{def.Id}", row =>
                {
                    ApplyDef(row, def);
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

    // ── Bonfire menu helpers (cooperative multi-mod editing) ────────────────────

    /// <summary>
    /// Add a menu item to the shared bonfire ESD. All mods can call this during
    /// their Patch() phase; the framework accumulates changes and writes the result
    /// to disk before the game starts.
    ///
    /// Menu item visibility is gated by <paramref name="gateFlag"/>: pass -1 to
    /// always show, or a flag ID to make it conditional.
    /// <code>
    /// // Add a "Fast Travel" option visible only after flag 11815700 is set:
    /// g.AddBonfireMenuItem(talkId: 15001200, gateFlag: 11815700);
    /// </code>
    /// </summary>
    /// <remarks>
    /// The bonfire ESD has a fixed state machine (group 1, state 4). This helper
    /// automatically allocates menu slot indices, starting after vanilla items (slot 5+).
    /// </remarks>
    public bool AddBonfireMenuItem(int talkId, int gateFlag = -1)
    {
        var esdObj = _ctx?.GetBonfireEsd();
        if (esdObj is not EsdEditor esd) return false;

        // Access the internal slot counter via reflection
        var ctxType = _ctx!.GetType();
        var slotField = ctxType.GetProperty("NextBonfireSlot");
        if (slotField == null) return false;

        int nextSlot = (int)slotField.GetValue(_ctx)!;
        esd.AddEntryCommand(groupId: 1, stateId: 4,
            TalkCmd.AddTalkListData(nextSlot, talkId, gateFlag));

        slotField.SetValue(_ctx, nextSlot + 1);
        return true;
    }

    /// <summary>
    /// Add a conditional bonfire menu item (visible only when <paramref name="condition"/> passes).
    /// Use this for complex visibility logic beyond simple flag gating.
    /// <code>
    /// // Show only when flag 11815700 is OFF:
    /// g.AddBonfireMenuItemIf(
    ///     EsdBytecode.Not(EsdBytecode.GetEventFlag(11815700)),
    ///     talkId: 15001200);
    /// </code>
    /// </summary>
    public bool AddBonfireMenuItemIf(byte[] condition, int talkId)
    {
        var esdObj = _ctx?.GetBonfireEsd();
        if (esdObj is not EsdEditor esd) return false;

        var ctxType = _ctx!.GetType();
        var slotField = ctxType.GetProperty("NextBonfireSlot");
        if (slotField == null) return false;

        int nextSlot = (int)slotField.GetValue(_ctx)!;
        esd.AddEntryCommand(groupId: 1, stateId: 4,
            TalkCmd.AddTalkListDataIf(condition, nextSlot, talkId));

        slotField.SetValue(_ctx, nextSlot + 1);
        return true;
    }
}
