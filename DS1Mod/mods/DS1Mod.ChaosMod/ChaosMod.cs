using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.ChaosMod;

/// <summary>
/// CHAOS MOD — the Undead Asylum has completely lost the plot.
///
/// What this does
/// ──────────────
///  Patch-time (Asylum, m18_01_00_00):
///    DefineSpEffect  — "Fool's Blessing": 8s HP regen burst (HpRecoverPoint=300)
///    DefineGoods     — "Chaos Coin": consumable that applies Fool's Blessing
///    DefineGoods     — "Fragment of Dignity": key item proof you survived
///    DefineLot       — Chaos Coin lot (infinite; awarded at demon HP milestones)
///    DefineLot       — Fragment of Dignity lot (once-only; drops on demon death)
///    PlaceTreasure   — Fragment of Dignity placed in the Asylum courtyard
///    DefineItemTrigger — SpEffect→flag bridge so C# gets an ItemUsed callback
///    EditEmevd       — 8 events: coin-use response, SFX on arena entry, two HP
///                      milestone awards, fragment drop, fragment hide, a showcase
///                      of compound WhenAnyOf conditions, and a SetCharacterImmortal
///                      no-op (character control API demo)
///    EditBnd3Glob    — FMG strings for all messages and item names
///    DefineSpEffect  — (raw params EditParams path via GoofyDemon pattern reference)
///
///  Runtime (OnLoad + OnTick + all four hooks):
///    BossKilled      → increment boss kill count, log, compute chaos
///    FogGateEntered  → increment fog gate count, detect "in boss fight"
///    PlayerDied      → increment death count, log shame message
///    PlayerLeveledUp → increment level-up count, log congratulations
///    OnTick          → refresh player HP/pos/souls, read live flags, compute
///                      Chaos Score = deaths×15 + bossKills×100 + fogGates×5 + levelUps×3
///    IGameWriter     → SetEventFlag round-trip on OnLoad (writer API demo)
///
///  ImGui (IGuiMod):
///    Main window    — CHAOS SCORE progress bar, milestone rank, live counters
///    Debug window   — allocated IDs, live flags (colour-coded), HP, map, position
/// </summary>
public sealed class ChaosMod : ModBase, IGamePatcher, IGuiMod
{
    public override string Name    => "Chaos Mod";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    // ── Map constants ─────────────────────────────────────────────────────────
    private const string Map          = IdSpaces.Asylum;
    private const int    Player       = 10000;
    private const int    DemonEntity  = 1810800;
    private const int    DemonDeadFlag = 16;
    private const int    ArenaAreaId  = 1812100;
    private const int    DemonAnim    = 9060;

    // AI
    private const string NpcFileId = "223200";
    private const string LuaId     = "MiniGreaterDemon223200";

    // ── Allocated IDs (set at patch time) ────────────────────────────────────
    private int CoinGoodsId;
    private int FragmentGoodsId;
    private int CoinSpEffect;
    private int EvilAuraSpEffect;
    private int CoinLotId;
    private int FragmentLotId;
    private int CoinUseFlag;
    private int FragmentGetFlag;
    private int FlagCoinUsedEver;
    private int FlagMilestone50;
    private int FlagMilestone25;
    private int FlagNeverFires;
    private int FlagEvilAuraApplied;
    private int FragmentEntityId;
    private int EvtCoinTrigger;
    private int EvtCoinResponse;
    private int EvtArenaEntry;
    private int EvtMilestone50;
    private int EvtMilestone25;
    private int EvtFragmentDrop;
    private int EvtFragmentHide;
    private int EvtCharShowcase;
    private int EvtApplyEvilAura;
    private int MsgCoinUsed;
    private int MsgMilestone50;
    private int MsgMilestone25;
    private int MsgArenaEntry;

    // Fragment world position — tucked behind the fog wall pillar in the courtyard
    private static readonly Vector3 FragmentPos = new(-14.612f, 190.249f, 17.178f);

    // ── Runtime state ─────────────────────────────────────────────────────────
    private IModContext? _ctx;
    private int  _deaths;
    private int  _bossKills;
    private int  _fogGates;
    private int  _levelUps;
    private bool _inBossFight;
    private string _lastBoss  = "None";
    private string _lastFog   = "None";

    // Tick-cached reader values (volatile so ImGui render thread reads safely)
    private volatile int   _hp     = 0;
    private volatile int   _maxHp  = 0;
    private volatile int   _souls  = 0;
    private volatile int   _level  = 0;
    private          string _mapId = "";
    private volatile float _x      = 0f;
    private volatile float _y      = 0f;
    private volatile float _z      = 0f;

    // Live flag values for debug panel
    private bool _liveCoinFlag;
    private bool _liveMilestone50;
    private bool _liveMilestone25;
    private bool _liveFragmentGet;
    private bool _liveCoinUsedEver;
    private bool _liveEvilAuraApplied;

    // ImGui window toggles
    private bool _windowOpen = true;
    private bool _debugOpen  = true;

    // Patch-time checklist
    private bool _patchSpEffect;
    private bool _patchGoods;
    private bool _patchLots;
    private bool _patchMsb;
    private bool _patchEmevd;
    private bool _patchFmg;
    private bool _patchAi;

    // Runtime checklist
    private bool _rtCoinUsed;
    private bool _rtMilestone50Hit;
    private bool _rtMilestone25Hit;
    private bool _rtFragmentGot;
    private bool _rtBossDead;

    // Log
    private StreamWriter? _log;

    // ── Chaos Score & Ranks ───────────────────────────────────────────────────
    private static readonly (int threshold, string rank)[] Ranks =
    [
        (0,   "Perfectly Sane"),
        (15,  "Slightly Unhinged"),
        (30,  "Certified Gremlin"),
        (60,  "Chaos Apprentice"),
        (120, "Chaos Adept"),
        (250, "Lord of Absolute Chaos"),
    ];

    private int ChaosScore => _deaths * 15 + _bossKills * 100 + _fogGates * 5 + _levelUps * 3;

    private string ChaosRank
    {
        get
        {
            string rank = Ranks[0].rank;
            foreach (var (threshold, name) in Ranks)
                if (ChaosScore >= threshold) rank = name;
            return rank;
        }
    }

    // ── IGamePatcher ──────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx);
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

        // ── ID allocation ─────────────────────────────────────────────────────
        CoinSpEffect     = ctx.AllocateIds(IdSpaces.SpEffectParam, 2);
        EvilAuraSpEffect = CoinSpEffect + 1;

        CoinGoodsId    = ctx.AllocateIds(IdSpaces.EquipParamGoods, 2);
        FragmentGoodsId = CoinGoodsId + 1;

        CoinLotId      = ctx.AllocateIds(IdSpaces.ItemLotParam, 2);
        FragmentLotId  = CoinLotId + 1;

        // 7 flags: coin-use toggle, fragment once-only, coin-ever, 50% milestone,
        //          25% milestone, never-fires showcase, evil-aura-applied indicator
        CoinUseFlag       = ctx.AllocateIds(IdSpaces.EventFlags(Map), 7);
        FragmentGetFlag   = CoinUseFlag + 1;
        FlagCoinUsedEver  = CoinUseFlag + 2;
        FlagMilestone50   = CoinUseFlag + 3;
        FlagMilestone25   = CoinUseFlag + 4;
        FlagNeverFires    = CoinUseFlag + 5;
        FlagEvilAuraApplied = CoinUseFlag + 6;

        // 10 EMEVD events
        EvtCoinTrigger = ctx.AllocateIds(IdSpaces.EmevdEvents(Map), 10);
        EvtCoinResponse  = EvtCoinTrigger + 1;
        EvtArenaEntry    = EvtCoinTrigger + 2;
        EvtMilestone50   = EvtCoinTrigger + 3;
        EvtMilestone25   = EvtCoinTrigger + 4;
        EvtFragmentDrop  = EvtCoinTrigger + 5;
        EvtFragmentHide  = EvtCoinTrigger + 6;
        EvtCharShowcase  = EvtCoinTrigger + 7;
        EvtApplyEvilAura = EvtCoinTrigger + 8;
        // EvtCoinTrigger + 9 reserved

        FragmentEntityId = ctx.AllocateId(IdSpaces.MsbEntities(Map));

        int msgBase    = ctx.AllocateIds(IdSpaces.EventText, 4);
        MsgCoinUsed    = msgBase;
        MsgMilestone50 = msgBase + 1;
        MsgMilestone25 = msgBase + 2;
        MsgArenaEntry  = msgBase + 3;

        // ── SpEffect: Fool's Blessing (player consumable) ─────────────────────
        // 8-second window; HpRecoverPoint fires on application for an instant
        // 300 HP restore burst. The Duration gives EMEVD time to detect it
        // via WhenCharacterHasSpEffect before it expires.
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id             = CoinSpEffect,
            Duration       = 8f,
            HpRecoverPoint = 300,
            Configure      = row => row["motionInterval"].Value = 0f,
        });

        // ── SpEffect: Evil Aura (applied to the Asylum Demon) ─────────────────
        // Permanent (Duration=9999). Multiplies the demon's max HP by 2.5×
        // and his physical attack output by 1.8×. Applied by an EMEVD event
        // the instant he goes alive, so it stacks with his vanilla stats.
        // The Chaos Coin's 300 HP burst is the intended counter-measure —
        // without it, the demon is dramatically harder.
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id       = EvilAuraSpEffect,
            Duration = 9999f,
            Configure = row =>
            {
                row["motionInterval"].Value         = 0f;
                row["maxHpRate"].Value              = 2.5f;   // 2.5x max HP
                row["physicsAttackPowerRate"].Value = 1.8f;   // 1.8x physical damage
                row["magicAttackPowerRate"].Value   = 1.8f;   // also boosts magic slam residual
                row["changeHpRate"].Value = 0;   // also boosts magic slam residual
            },
        });
        _patchSpEffect = true;

        // ── Goods: Chaos Coin + Fragment of Dignity ───────────────────────────
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id           = CoinGoodsId,
            DonorId      = 384,            // Estus Flask as base
            SpEffectId   = CoinSpEffect,
            Name         = "Chaos Coin",
            Description  = "Restores 300 HP. Smells faintly of regret and poor choices.",
            LongDesc     = "A coin minted in the heat of absolute chaos.\n\n"
                         + "The inscription reads: 'YOU ASKED FOR THIS'.\n\n"
                         + "Consume to restore HP.",
            MaxCount     = 5,
            AllowQuickUse = true,
        });

        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = FragmentGoodsId,
            DonorId     = 384,
            Name        = "Fragment of Dignity",
            Description = "A piece of something that once mattered.",
            LongDesc    = "Recovered from the floor of the Undead Asylum.\n\n"
                        + "It is unclear whose dignity this belonged to.\n"
                        + "Probably yours.\n\nDoes nothing.",
            MaxCount    = 1,
            GoodsType   = 1,             // key item
        });
        _patchGoods = true;

        // ── Lots ──────────────────────────────────────────────────────────────
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = CoinLotId,
            ItemId       = CoinGoodsId,
            Category     = LotCategory.Goods,
            Rarity       = 3,
            Count        = 2,
            OnceOnlyFlag = -1,            // infinite — re-awards each milestone
        });

        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = FragmentLotId,
            ItemId       = FragmentGoodsId,
            Category     = LotCategory.Goods,
            Rarity       = 3,
            Count        = 1,
            OnceOnlyFlag = FragmentGetFlag,
        });
        _patchLots = true;

        // ── MSB: Fragment pickup object ───────────────────────────────────────
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(
                lotId:    FragmentLotId,
                position: FragmentPos,
                entityId: FragmentEntityId));
        _patchMsb = true;

        // ── EMEVD ─────────────────────────────────────────────────────────────
        g.DefineItemTrigger(Map,
            spEffectId:    CoinSpEffect,
            triggerFlagId: CoinUseFlag,
            eventId:       EvtCoinTrigger);

        g.EditEmevd(Map, emevd =>
        {

            emevd.DefineEvent(11810000, EMEVD.Event.RestBehaviorType.Default, ev => ev
                // 1. Trigger condition
                .WhenInsideArea(Player, 1812000)

                // 2. Set progression flags
                .SetFlag(11010501, FlagState.On)
                .SetFlag(11010000, FlagState.On)

                // 3. Force the inter-map warp to Kiln (m18_00_00_00) using verified 2003[14]
                // Parameters: (byte)areaId, (byte)blockId, (int)pointEntityId
                .Raw(2003, 14, (byte)18, (byte)0, 1802960)

                // 4. Fire your destination cutscene directly on the player slot at the target location 
                // Instruction 2002[03] (PlayCutsceneToPlayer) -> cutsceneId, playbackMethod, playerEntityId
                .Raw(2002, 3, 100225, 8, 1000)

                .End());

            // Coin used → show message + set permanent record; reset for next use
            emevd.DefineEvent(EvtCoinResponse, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenFlag(CoinUseFlag, FlagState.On)
                .DisplayMessage(MsgCoinUsed)
                .SetFlag(FlagCoinUsedEver, FlagState.On)
                .WhenFlag(CoinUseFlag, FlagState.Off)
                .Restart());

            // Arena entry SFX: raw escape hatch — SpawnOneshotSfx (bank 2006 id 3)
            // Fires the demon's landing-impact SFX on the player whenever they
            // step into the arena, then waits for them to leave before re-arming.
            emevd.DefineEvent(EvtArenaEntry, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenInsideArea(Player, areaEntityId: ArenaAreaId)
                .DisplayStatusMessage(MsgArenaEntry)
                .Raw(2006, 3, 1, DemonEntity, 220, 5090)  // SpawnOneshotSfx
                .WhenOutsideArea(Player, areaEntityId: ArenaAreaId)
                .Restart());

            // Demon HP < 50% → award 2x Chaos Coin once
            emevd.DefineEvent(EvtMilestone50, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenHpBelow(DemonEntity, 0.5f)
                .AwardItemLot(CoinLotId)
                .DisplayStatusMessage(MsgMilestone50)
                .SetFlag(FlagMilestone50, FlagState.On)
                .End());

            // Demon HP < 25% → award another 2x Chaos Coin once
            emevd.DefineEvent(EvtMilestone25, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenHpBelow(DemonEntity, 0.25f)
                .AwardItemLot(CoinLotId)
                .DisplayStatusMessage(MsgMilestone25)
                .SetFlag(FlagMilestone25, FlagState.On)
                .End());

            // Drop Fragment of Dignity when demon dies (WhenDead)
            emevd.DefineEvent(EvtFragmentDrop, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenAlive(DemonEntity)
                .AwardItemLot(FragmentLotId)
                .End());

            // Hide the Fragment o0500 pickup once collected
            emevd.DefineEvent(EvtFragmentHide, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(FragmentGetFlag, FlagState.On)
                .SetObjectEnabled(FragmentEntityId, EnabledState.Disabled)
                .End());

            // Character-control API showcase — gated behind FlagNeverFires so it
            // compiles and runs but never actually does anything.
            // Exercises: SetCharacterEnabled, SetCharacterAI, SetCharacterImmortal,
            //            SetCharacterInvincible, SetCharacterHome.
            emevd.DefineEvent(EvtCharShowcase, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(FlagNeverFires, FlagState.On)
                .SetCharacterEnabled(DemonEntity, EnabledState.Enabled)
                .SetCharacterAI(DemonEntity, EnabledState.Enabled)
                .SetCharacterImmortal(DemonEntity, EnabledState.Disabled)
                .SetCharacterInvincible(DemonEntity, EnabledState.Disabled)
                .SetCharacterHome(DemonEntity, regionEntityId: 1810100)
                .End());

            // Apply Evil Aura the moment the demon becomes alive.
            // SetSpEffect is safe to fire on a living character; using WhenAlive
            // ensures the entity is ready to receive the buff.
            // RestBehaviorType.Default so it only fires once per map load.
            emevd.DefineEvent(EvtApplyEvilAura, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenDead(DemonEntity)
                .SetSpEffect(DemonEntity, EvilAuraSpEffect)
                .SetFlag(FlagEvilAuraApplied, FlagState.On)
                .End());
        });
        _patchEmevd = true;

        // ── AI: Chaos Battle Script ───────────────────────────────────────────
        // Replaces 223200_battle.lua with a 6-act weighted table.
        // The demon attacks relentlessly in random patterns — no breathing room.
        // NOTE: conflicts with ItemsDemo and GoofyDemon (same luabnd entry).
        g.EditAi(Map, NpcFileId, ai => ai
            .Goal("Battle", goal => goal

                // Act 1 (30%): Berserker Rush — close-range slam, no recovery
                .Act(30, q => q
                    .ApproachTarget(Target.Enemy0, distMeters: 6f, cancelTime: 8)
                    .Attack(animId: 3007, cancelTime: 6)
                    .Wait(1))

                // Act 2 (22%): Double Slam — approach twice, slam twice
                .Act(22, q => q
                    .ApproachTarget(Target.Enemy0, distMeters: 9f, cancelTime: 8)
                    .Attack(animId: 3007, cancelTime: 6)
                    .ApproachTarget(Target.Enemy0, distMeters: 4f, cancelTime: 5)
                    .Attack(animId: 3007, cancelTime: 6))

                // Act 3 (18%): Spin Fury — erratic movement then slam
                .Act(18, q => q
                    .SpinStep(cancelTime: 4)
                    .SpinStep(cancelTime: 4)
                    .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 6)
                    .Attack(animId: 3007, cancelTime: 6))

                // Act 4 (15%): Hit and Run — slam then disengage to reset spacing
                .Act(15, q => q
                    .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 7)
                    .Attack(animId: 3007, cancelTime: 5)
                    .LeaveTarget(cancelTime: 5)
                    .WaitRandom(1, 2))

                // Act 5 (10%): Side Strafe — sidestep to unpredictable angle then slam
                .Act(10, q => q
                    .SidewayMove(cancelTime: 3)
                    .SidewayMove(cancelTime: 3)
                    .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 6)
                    .Attack(animId: 3007, cancelTime: 6))

                // Act 6 (5%): Slow Terror — long dramatic pause then a burst
                .Act(5, q => q
                    .WaitRandom(2, 4)
                    .SpinStep(cancelTime: 3)
                    .ApproachTarget(Target.Enemy0, distMeters: 4f, cancelTime: 5)
                    .Attack(animId: 3007, cancelTime: 5)
                    .Attack(animId: 3007, cancelTime: 5))), luaId: LuaId);
        _patchAi = true;

        // ── FMG strings ───────────────────────────────────────────────────────
        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.EventText, MsgCoinUsed,    "CHAOS REIGNS. (+300 HP)");
            Texts.Set(bnd, Texts.EventText, MsgMilestone50, "The demon is unwell. Coins appeared.");
            Texts.Set(bnd, Texts.EventText, MsgMilestone25, "Almost there, you absolute disaster.");
            Texts.Set(bnd, Texts.EventText, MsgArenaEntry,  "Something stirs in the darkness...");
        });
        _patchFmg = true;
    }

    // ── IGameMod lifecycle ────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        _log = new StreamWriter(Path.Combine(ctx.ModsDir, "ChaosMod.log"), append: false)
            { AutoFlush = true };
        Log("=== Chaos Mod v1.0.0 loaded ===");

        // Register item-used bridge: when CoinUseFlag pulses, ItemUsed fires
        ctx.Hooks.RegisterItemUsed(CoinGoodsId, CoinUseFlag);
        ctx.Hooks.ItemUsed        += OnItemUsed;

        // All four event hooks
        ctx.Hooks.BossKilled      += OnBossKilled;
        ctx.Hooks.FogGateEntered  += OnFogGateEntered;
        ctx.Hooks.PlayerDied      += OnPlayerDied;
        ctx.Hooks.PlayerLeveledUp += OnPlayerLeveledUp;

        // IGameWriter round-trip demo: read CoinUseFlag, write same value back
        bool before = ctx.Reader.GetEventFlag(CoinUseFlag);
        ctx.Writer.SetEventFlag(CoinUseFlag, before);
        bool after  = ctx.Reader.GetEventFlag(CoinUseFlag);
        Log($"Writer round-trip: CoinUseFlag = {before} → {after} ({(before == after ? "OK" : "MISMATCH!")})");

        Log($"Goods: CoinGoodsId={CoinGoodsId} FragmentGoodsId={FragmentGoodsId}");
        Log($"SpEff: CoinSpEffect={CoinSpEffect}");
        Log($"Lots:  CoinLotId={CoinLotId} FragmentLotId={FragmentLotId}");
        Log($"Flags: CoinUseFlag={CoinUseFlag} .. FlagNeverFires={FlagNeverFires}");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;
        var r = _ctx.Reader;

        // Refresh cached stats for ImGui render thread
        var stats = r.GetPlayerStats();
        if (stats is not null)
        {
            _hp    = stats.CurrentHp;
            _maxHp = stats.MaxHp;
        }

        var state = r.GetPlayerState();
        if (state is not null)
        {
            _x     = state.X;
            _y     = state.Y;
            _z     = state.Z;
            _mapId = state.MapId ?? "";
        }

        _souls = r.GetSouls();
        _level = r.GetSoulLevel();

        // Live flag reads for debug panel + runtime checklist
        _liveCoinFlag        = r.GetEventFlag(CoinUseFlag);
        _liveMilestone50     = r.GetEventFlag(FlagMilestone50);
        _liveMilestone25     = r.GetEventFlag(FlagMilestone25);
        _liveFragmentGet     = r.GetEventFlag(FragmentGetFlag);
        _liveCoinUsedEver    = r.GetEventFlag(FlagCoinUsedEver);
        _liveEvilAuraApplied = r.GetEventFlag(FlagEvilAuraApplied);

        if (!_rtCoinUsed)       _rtCoinUsed       = _liveCoinUsedEver;
        if (!_rtMilestone50Hit) _rtMilestone50Hit = _liveMilestone50;
        if (!_rtMilestone25Hit) _rtMilestone25Hit = _liveMilestone25;
        if (!_rtFragmentGot)    _rtFragmentGot    = _liveFragmentGet;
        if (!_rtBossDead)       _rtBossDead       = r.GetEventFlag(DemonDeadFlag);
    }

    public override void OnUnload()
    {
        Log($"=== Chaos Mod unloading — final score: {ChaosScore} ({ChaosRank}) ===");
        Log($"   deaths={_deaths} bossKills={_bossKills} fogGates={_fogGates} levelUps={_levelUps}");
        _log?.Dispose();
        _log = null;
    }

    // ── Hook handlers ─────────────────────────────────────────────────────────
    private void OnItemUsed(int goodsId)
    {
        if (goodsId != CoinGoodsId) return;
        _rtCoinUsed = true;
        Log($"Chaos Coin used! chaos score now {ChaosScore}");
    }

    private void OnBossKilled(BossKill kill)
    {
        _bossKills++;
        _inBossFight = false;
        _lastBoss    = kill.BossName;
        Log($"[BOSS] {kill.BossName} slain. chaos score → {ChaosScore}");
    }

    private void OnFogGateEntered(FogGate gate)
    {
        _fogGates++;
        _inBossFight = true;
        _lastFog     = gate.Name;
        Log($"[FOG] Entered: {gate.Name} (map {gate.MapId})");
    }

    private void OnPlayerDied()
    {
        _deaths++;
        _inBossFight = false;
        Log($"[DIED] You died. Shame count: {_deaths}. chaos score → {ChaosScore}");

        // Rank announcement on milestone deaths
        if (_deaths is 1 or 5 or 10 or 25 or 50)
            Log($"[DIED] Rank achieved: {ChaosRank}");
    }

    private void OnPlayerLeveledUp(int newLevel)
    {
        _levelUps++;
        int souls = _ctx?.Reader.GetSouls() ?? 0;
        Log($"[LEVEL] SL → {newLevel}. Souls remaining: {souls:N0}. chaos score → {ChaosScore}");
    }

    // ── IGuiMod ───────────────────────────────────────────────────────────────

    public void OnGui()
    {
        DrawMainWindow();
        DrawDebugWindow();
    }

    private void DrawMainWindow()
    {
        if (!_windowOpen) return;

        DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(370, 0, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.85f);

        if (!DS1ImGui.Begin("CHAOS MOD", ref _windowOpen))
        {
            DS1ImGui.End();
            return;
        }

        // ── Chaos Score bar ───────────────────────────────────────────────────
        int score = ChaosScore;
        int nextThreshold = 250;
        foreach (var (threshold, _) in Ranks)
            if (score < threshold) { nextThreshold = threshold; break; }

        float progress = nextThreshold > 0 ? Math.Min(1f, (float)score / nextThreshold) : 1f;

        DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0.85f, 0.2f, 0.05f, 1f);
        DS1ImGui.ProgressBar(progress, -1, 0, $"Chaos: {score}");
        DS1ImGui.PopStyleColor();

        DS1ImGui.PushStyleColor(ImGuiCol.Text, 1f, 0.6f, 0.1f, 1f);
        DS1ImGui.Text($"  Rank: {ChaosRank}");
        DS1ImGui.PopStyleColor();

        DS1ImGui.Spacing();
        DS1ImGui.Separator();

        // ── Live counters ─────────────────────────────────────────────────────
        DS1ImGui.Text("Session Stats");
        DS1ImGui.Text($"  Deaths:     {_deaths}   (x15 pts each)");
        DS1ImGui.Text($"  Boss kills: {_bossKills}   (x100 pts each)");
        DS1ImGui.Text($"  Fog gates:  {_fogGates}   (x5 pts each)");
        DS1ImGui.Text($"  Level-ups:  {_levelUps}   (x3 pts each)");

        if (_inBossFight)
        {
            DS1ImGui.Spacing();
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 1f, 0.2f, 0.2f, 1f);
            DS1ImGui.Text($"  !! IN BOSS FIGHT: {_lastFog} !!");
            DS1ImGui.PopStyleColor();
        }
        else if (_lastBoss != "None")
        {
            DS1ImGui.Text($"  Last boss killed: {_lastBoss}");
        }

        DS1ImGui.Spacing();
        DS1ImGui.Separator();

        // ── Demon status ──────────────────────────────────────────────────────
        DS1ImGui.Text("Asylum Demon");
        if (_liveEvilAuraApplied)
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.9f, 0.15f, 0.15f, 1f);
            DS1ImGui.Text("  !! EVIL AURA ACTIVE — 2.5x HP  1.8x DMG !!");
            DS1ImGui.PopStyleColor();
        }
        else
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.5f, 0.5f, 0.5f, 1f);
            DS1ImGui.Text("  Evil Aura: pending (fires when demon goes alive)");
            DS1ImGui.PopStyleColor();
        }
        DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.75f, 0.75f, 0.75f, 1f);
        DS1ImGui.Text("  AI: 6-act chaos (Rush/DoubleSam/Spin/HitRun/Strafe/Terror)");
        DS1ImGui.Text("  Chaos Coin (+300 HP) is your counter-measure.");
        DS1ImGui.PopStyleColor();

        DS1ImGui.Spacing();
        DS1ImGui.Separator();

        // ── Asylum checklist ──────────────────────────────────────────────────
        DS1ImGui.Text("Asylum Milestones");
        DrawCheck("Use a Chaos Coin",                _rtCoinUsed);
        DrawCheck("Demon HP < 50 % (coins awarded)", _rtMilestone50Hit);
        DrawCheck("Demon HP < 25 % (coins awarded)", _rtMilestone25Hit);
        DrawCheck("Collect Fragment of Dignity",      _rtFragmentGot);
        DrawCheck("Asylum Demon killed",              _rtBossDead);

        DS1ImGui.Spacing();
        DS1ImGui.Separator();
        DS1ImGui.Checkbox("Debug Info", ref _debugOpen);

        DS1ImGui.End();
    }

    private void DrawDebugWindow()
    {
        if (!_debugOpen) return;

        DS1ImGui.SetNextWindowPos(10, 360, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(420, 0, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.88f);

        bool open = true;
        if (!DS1ImGui.Begin("ChaosMod Debug", ref open))
        {
            DS1ImGui.End();
            return;
        }
        if (!open) _debugOpen = false;

        // ── Player state ──────────────────────────────────────────────────────
        DS1ImGui.Text("Player");
        DS1ImGui.Separator();
        if (_maxHp > 0)
        {
            float hpFrac = (float)_hp / _maxHp;
            DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0.7f, 0.1f, 0.1f, 1f);
            DS1ImGui.ProgressBar(hpFrac, -1, 0, $"HP  {_hp} / {_maxHp}");
            DS1ImGui.PopStyleColor();
            DS1ImGui.Text($"  SL {_level}  |  {_souls:N0} souls");
            DS1ImGui.Text($"  Map: {(_mapId.Length > 0 ? _mapId : "—")}");
            DS1ImGui.Text($"  Pos: ({_x:F1}, {_y:F1}, {_z:F1})");
        }
        else
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 1f, 0.5f, 0.1f, 1f);
            DS1ImGui.Text("  Not in-game (EventPump paused)");
            DS1ImGui.PopStyleColor();
        }

        // ── Patch-time checks ─────────────────────────────────────────────────
        DS1ImGui.Spacing();
        DS1ImGui.Text("Patch");
        DS1ImGui.Separator();
        DrawCheck("DefineSpEffect x2 (Coin + Evil Aura)", _patchSpEffect);
        DrawCheck("DefineGoods (Coin + Fragment)",         _patchGoods);
        DrawCheck("DefineLot (infinite + once-only)",      _patchLots);
        DrawCheck("EditMsb / PlaceTreasure",               _patchMsb);
        DrawCheck("EditEmevd (10 events incl. Evil Aura)", _patchEmevd);
        DrawCheck("EditBnd3Glob / FMG strings",            _patchFmg);
        DrawCheck("EditAi — 6-act Chaos Battle script",    _patchAi);

        // ── Allocated IDs ─────────────────────────────────────────────────────
        DS1ImGui.Spacing();
        DS1ImGui.Text("Allocated IDs");
        DS1ImGui.Separator();
        DS1ImGui.Text($"  CoinSpEffect:     {CoinSpEffect}");
        DS1ImGui.Text($"  EvilAuraSpEffect: {EvilAuraSpEffect}  (2.5x HP, 1.8x dmg)");
        DS1ImGui.Text($"  Coin goods:   {CoinGoodsId}    lot: {CoinLotId}");
        DS1ImGui.Text($"  Fragment:     {FragmentGoodsId}   lot: {FragmentLotId}");
        DS1ImGui.Text($"  FragmentEnt:  {FragmentEntityId}");
        DS1ImGui.Text($"  EMEVD events: {EvtCoinTrigger} .. {EvtApplyEvilAura}");
        DS1ImGui.Text($"  FMG msgs:     {MsgCoinUsed} .. {MsgArenaEntry}");

        // ── Live flags ────────────────────────────────────────────────────────
        DS1ImGui.Spacing();
        DS1ImGui.Text("Live Event Flags  (500 ms poll)");
        DS1ImGui.Separator();
        DrawFlagRow($"  EvilAuraApplied  [{FlagEvilAuraApplied}]", _liveEvilAuraApplied);
        DrawFlagRow($"  CoinUseFlag      [{CoinUseFlag}]",          _liveCoinFlag);
        DrawFlagRow($"  FlagCoinUsedEver [{FlagCoinUsedEver}]",     _liveCoinUsedEver);
        DrawFlagRow($"  FlagMilestone50  [{FlagMilestone50}]",      _liveMilestone50);
        DrawFlagRow($"  FlagMilestone25  [{FlagMilestone25}]",      _liveMilestone25);
        DrawFlagRow($"  FragmentGetFlag  [{FragmentGetFlag}]", _liveFragmentGet);
        DrawFlagRow($"  DemonDeadFlag    [16]",                _rtBossDead);

        DS1ImGui.End();
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private static void DrawFlagRow(string label, bool live)
    {
        if (live)
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.2f, 0.9f, 0.2f, 1f);
            DS1ImGui.Text($"{label}  ON");
            DS1ImGui.PopStyleColor();
        }
        else
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.5f, 0.5f, 0.5f, 1f);
            DS1ImGui.Text($"{label}  off");
            DS1ImGui.PopStyleColor();
        }
    }

    private static void DrawCheck(string label, bool done)
    {
        if (done)
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.2f, 0.9f, 0.2f, 1f);
            DS1ImGui.Text($"  [x] {label}");
            DS1ImGui.PopStyleColor();
        }
        else
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.5f, 0.5f, 0.5f, 1f);
            DS1ImGui.Text($"  [ ] {label}");
            DS1ImGui.PopStyleColor();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Log(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        _log?.WriteLine(line);
        Console.WriteLine($"[ChaosMod] {line}");
    }

    private static byte[] GetEmbeddedResource(string name)
    {
        using Stream? s = typeof(ChaosMod).Assembly.GetManifestResourceStream(name);
        if (s is null) return [];
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
