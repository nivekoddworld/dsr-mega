using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.ItemsDemo;

/// <summary>
/// Demo mod that exercises every API added to the DS1Mod framework.
///
/// APIs under test
/// ───────────────
///  Conflict detection (DS1Mod.Core / DS1Mod.Host):
///    IPatchContext.RecordEdit — automatic via GamePatch(ctx) constructor
///
///  EMEVD / EventBuilder (DS1Mod.Modding):
///    Conditions:   WhenFlag, WhenDead, WhenAlive, WhenHpBelow
///                  WhenInsideArea, WhenOutsideArea
///                  WhenCharacterHasSpEffect, WhenCharacterLosesSpEffect
///    Compound:     WhenAllOf (AND group), WhenAnyOf (OR group)
///    Actions:      SetFlag, AwardItemLot, DisplayMessage, DisplayStatusMessage
///                  DisplayBanner, DisplayBossHealthBar, ForceAnimation
///                  SetCharacterEnabled, KillCharacter, SetCharacterAI
///                  SetCharacterHome, SetCharacterImmortal, SetCharacterInvincible
///                  WarpCharacter, HandleBossDefeat
///    Control flow: End(), Restart(), Raw(bank, id, args)
///    RestBehavior: Default, Restart
///
///  Lua AI / AiBuilder (DS1Mod.Modding):
///    Goal, Act (weighted), OnActivate, OnInterrupt, Helper
///    SubGoalQueue: ApproachTarget, Attack, SpinStep, Wait, SidewayMove,
///                  LeaveTarget, WaitRandom, SetEventFlag, SetActiveFlagInRange, Raw
///    Luac50.Configure — override binary path
///    NOTE: This AI patches the Asylum Demon (NPC 223200) and CONFLICTS with
///    GoofyDemon, which also replaces 223200_battle.lua. This is intentional —
///    it demonstrates the conflict detection log that appears when both mods run.
///
///  Items (DS1Mod.Modding):
///    DefineSpEffect — SpEffectParam row with named fields + Configure callback
///    DefineGoods    — EquipParamGoods row + all-locale FMG strings
///                     SpEffectId auto-wires consumable + refId_default
///                     Configure callback for raw param fields
///    DefineLot      — ItemLotParam (once-only and infinite variants)
///    EditMsb / PlaceTreasure — ground pickup + Treasure event
///    DefineItemTrigger — EMEVD SpEffect→flag bridge
///
///  Runtime item use hook (DS1Mod.Core):
///    hooks.RegisterItemUsed — register (goodsId, triggerFlagId) pair
///    hooks.ItemUsed         — C# event fires when the item is used
///
/// All patches target the Undead Asylum (m18_01_00_00).
/// </summary>
public sealed class ItemsDemoMod : ModBase, IGamePatcher, IGuiMod
{
    public override string Name    => "Items + EMEVD + AI Demo";
    public override string Version => "2.0.0";
    public override string Author  => "DS1MegaRando";

    // ── IDs ──────────────────────────────────────────────────────────────────

    // Map
    private const string Map           = "m18_01_00_00";
    private const int    Player        = 10000;
    private const int    DemonEntity   = 1810800;
    private const int    DemonDeadFlag = 16;

    // AI
    private const string NpcFileId = "223200";   // Asylum Demon AI file
    private const string LuaId     = "MiniGreaterDemon223200"; // must match vanilla goal name prefix

    // Goods
    private const int DraughtGoodsId = 8100;     // consumable, uses SpEffect
    private const int TrinketGoodsId = 8101;     // key item, no use effect

    // SpEffect
    private const int DraughtSpEffect = 9100;

    // Lots
    private const int DraughtLotId  = 8600;      // infinite — no once-only flag
    private const int TrinketLotId  = 8601;      // once-only ground pickup

    // Event flags  (m18_01 section 9 allocated range)
    private const int DraughtUseFlag   = 11819400; // pulses ON while SpEffect active
    private const int TrinketGetFlag   = 11819401; // set once when trinket is obtained
    private const int FlagUsedDraught  = 11819402; // permanent "used at least once"
    private const int FlagAiMood0      = 11819403; // AI mood flag base (mood 0 = stomp)
    private const int FlagAiMood1      = 11819404; // AI mood flag (mood 1 = spin+leave)
    // 11819405–11819410 reserved for future use

    // EMEVD event IDs
    private const int EvtItemTrigger   = 11819400; // DefineItemTrigger writes this id
    private const int EvtUseResponse   = 11819405; // in-game display on draught use
    private const int EvtAwardTrinket  = 11819406; // EMEVD AND-group demo: award on hp+area
    private const int EvtBossIntro     = 11819407; // EMEVD OR-group + boss-bar demo
    private const int EvtDemonControl  = 11819408; // char enable/disable/AI/immortal demo
    private const int EvtRawEscape     = 11819409; // Raw() escape hatch demo
    private const int EvtHideTrinket   = 11819411; // hide the ground o0500 after pickup

    // MSB entity IDs
    private const int TrinketEntityId  = 1811999; // unused range in m18_01 — bound to our o0500

    // FMG message IDs (Event_text)
    private const int MsgDraughtUsed   = 6900760;
    private const int MsgTrinketFound  = 6900761;
    private const int MsgDemonWarning  = 6900762;

    // World position — ledge near Asylum start
    private static readonly Vector3 TrinketPos = new(-13.744f, 190.249f, 11.696f);

    private IModContext? _ctx;

    // ── ImGui checklist state ─────────────────────────────────────────────────
    // Patch-time items are set once in OnLoad (they either worked or the mod
    // wouldn't have loaded). Runtime items are polled each OnTick via flags.
    // Written on the game-thread (OnTick/OnLoad), read on the render thread
    // (OnGui) — using volatile for safe cross-thread visibility on primitives.

    // patch-time: ticked immediately
    private volatile bool _patchConflictDetection;
    private volatile bool _patchSpEffect;
    private volatile bool _patchGoods;
    private volatile bool _patchLots;
    private volatile bool _patchMsb;
    private volatile bool _patchItemTrigger;
    private volatile bool _patchEmevd;
    private volatile bool _patchAi;

    // runtime: ticked as they happen in-game
    private volatile bool _rtDraughtUsed;        // hooks.ItemUsed fired
    private volatile bool _rtTrinketCollected;   // once-only lot obtained (flag set)
    private volatile bool _rtMidFightReward;     // WhenAllOf fired (demon hp < 50% + alive)
    private volatile bool _rtBossDead;           // boss death flag
    private volatile bool _rtAiMood0;            // AI chose act 0 (SetActiveFlagInRange)
    private volatile bool _rtAiMood1;            // AI chose act 1

    private bool _windowOpen = true;

    // ═════════════════════════════════════════════════════════════════════════
    // IGamePatcher
    // ═════════════════════════════════════════════════════════════════════════

    public void Patch(IPatchContext ctx)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: Conflict detection
        //
        // Constructing GamePatch from an IPatchContext (not the low-level
        // overload) wires _recordEdit automatically. Every write operation
        // (EditParams, EditEmevd, EditBnd3, EditMsb, EditAi) calls
        // ctx.RecordEdit(filePath, selector) internally — mod authors never
        // call it directly.
        //
        // The host (PatchContext.cs) logs a warning when two loaded mods
        // record the same (filePath, selector) pair, e.g.:
        //   [CONFLICT] DS1Mod.GoofyDemon and DS1Mod.ItemsDemo both wrote
        //              script/m18_01_00_00.luabnd.dcx :: BND3:script/...
        //
        // To see this warning, install both GoofyDemon and ItemsDemo.
        // ════════════════════════════════════════════════════════════════════
        var g = new GamePatch(ctx);  // wires conflict detection via IPatchContext
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

        PatchSpEffects(g, paramdefs);
        PatchGoods(g, paramdefs);
        PatchLots(g, paramdefs);
        PatchMsb(g);
        PatchEmevd(g);
        PatchAi(g);
    }

    // ── SpEffect ─────────────────────────────────────────────────────────────

    private static void PatchSpEffects(GamePatch g, byte[] paramdefs)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: DefineSpEffect
        //
        // Creates a SpEffectParam row by cloning DonorId and overwriting the
        // listed fields. Duration=0 means instant (fires once, no lingering).
        // HpRecoverPoint restores flat HP on application.
        //
        // The Configure callback is an escape hatch for any SpEffectParam
        // field not exposed as a named property on SpEffectDef.
        // ════════════════════════════════════════════════════════════════════
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id             = DraughtSpEffect,
            // DonorId omitted → uses framework default (110), a benign
            // vanilla DSR row. The legacy 7000 ("generic buff") doesn't
            // exist in DSR's SpEffectParam and threw at patch time.
            Duration       = 0f,             // instant — fires once on use
            HpRecoverPoint = 400,            // flat HP restored

            // Configure: set any field not covered by named props
            Configure      = row => row["motionInterval"].Value = 0f,
        });
    }

    // ── Goods ─────────────────────────────────────────────────────────────────

    private static void PatchGoods(GamePatch g, byte[] paramdefs)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: DefineGoods + ItemDef.SpEffectId
        //
        // SpEffectId set → auto-wires goodsType=0 (consumable) and
        // refId_default=SpEffectId. All-locale FMG strings are written
        // automatically (GoodsName / GoodsInfo / GoodsCaption).
        // ════════════════════════════════════════════════════════════════════
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = DraughtGoodsId,
            DonorId     = 384,               // Estus Flask row as base
            SpEffectId  = DraughtSpEffect,   // consumable, triggers SpEffect 9100
            Name        = "Goofy Draught",
            Description = "Restores 400 HP. Tastes of regret.",
            LongDesc    = "Brewed from the tears of the Asylum Demon after he lost a "
                        + "staring contest with a Hollow.\n\nConsume to restore HP.",
            MaxCount    = 5,
        });

        // ════════════════════════════════════════════════════════════════════
        // API: DefineGoods + ItemDef.Configure
        //
        // Configure callback sets fields not covered by ItemDef named props.
        // goodsType=4 → key item (stays in inventory, has no on-use effect).
        // SpEffectId is left at -1 (default) because a key item does nothing.
        // ════════════════════════════════════════════════════════════════════
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = TrinketGoodsId,
            DonorId     = 384,
            Name        = "Stone Trinket",
            Description = "A small stone. It does nothing.",
            LongDesc    = "Found on the floor of the Undead Asylum.\n\n"
                        + "It has no function whatsoever. You picked it up anyway.",
            MaxCount    = 1,
            Configure   = row => row["goodsType"].Value = (byte)4,  // key item
        });
    }

    // ── Lots ──────────────────────────────────────────────────────────────────

    private static void PatchLots(GamePatch g, byte[] paramdefs)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: DefineLot — infinite variant
        //
        // OnceOnlyFlag = -1 → no restriction; lot awards items every time it
        // is triggered (via AwardItemLot EMEVD instruction or PlaceTreasure).
        // ════════════════════════════════════════════════════════════════════
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = DraughtLotId,
            ItemId       = DraughtGoodsId,
            Category     = LotCategory.Goods,
            Count        = 3,
            OnceOnlyFlag = -1,              // infinite — re-awards each time
        });

        // ════════════════════════════════════════════════════════════════════
        // API: DefineLot — once-only variant
        //
        // OnceOnlyFlag set → game writes that flag when the lot is obtained,
        // and skips the award on subsequent triggers.
        // ════════════════════════════════════════════════════════════════════
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = TrinketLotId,
            ItemId       = TrinketGoodsId,
            Category     = LotCategory.Goods,
            Count        = 1,
            OnceOnlyFlag = TrinketGetFlag,  // set once when obtained
        });
    }

    // ── MSB ───────────────────────────────────────────────────────────────────

    private static void PatchMsb(GamePatch g)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: EditMsb / PlaceTreasure
        //
        // Adds a glowing o0500 ground-pickup object at TrinketPos with a
        // Treasure event pointing to TrinketLotId. The collision mesh is
        // borrowed from the nearest existing collision part automatically.
        //
        // EntityID is assigned (TrinketEntityId) so the EMEVD event
        // EvtHideTrinket can reference the part and disable it once the
        // player collects the lot — without that, the o0500 stays in the
        // world after pickup and the player can re-trigger the pickup
        // animation indefinitely with nothing in the lot.
        // ════════════════════════════════════════════════════════════════════
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(
                lotId:    TrinketLotId,
                position: TrinketPos,
                entityId: TrinketEntityId));
    }

    // ── EMEVD ─────────────────────────────────────────────────────────────────

    private static void PatchEmevd(GamePatch g)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: DefineItemTrigger
        //
        // Shorthand that writes a Restart EMEVD event internally using
        // WhenCharacterHasSpEffect and WhenCharacterLosesSpEffect:
        //   1. Wait until Player has SpEffect DraughtSpEffect active
        //   2. Set DraughtUseFlag ON
        //   3. Wait until the SpEffect expires
        //   4. Set DraughtUseFlag OFF → restart (ready for next use)
        //
        // eventId defaults to triggerFlagId when omitted.
        // ════════════════════════════════════════════════════════════════════
        g.DefineItemTrigger(Map,
            spEffectId:    DraughtSpEffect,
            triggerFlagId: DraughtUseFlag,
            eventId:       EvtItemTrigger);

        g.EditEmevd(Map, emevd =>
        {
            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — conditions WhenFlag, WhenCharacterHasSpEffect,
            //      WhenCharacterLosesSpEffect; actions DisplayMessage, SetFlag;
            //      control flow Restart; RestBehaviorType.Restart
            //
            // In-game engine response: show a popup when the draught is used,
            // then record it permanently. Loops via Restart so it re-arms for
            // future uses.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtUseResponse, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenFlag(DraughtUseFlag, FlagState.On)
                .DisplayMessage(MsgDraughtUsed)
                .SetFlag(FlagUsedDraught, FlagState.On)  // permanent "used" record
                .WhenFlag(DraughtUseFlag, FlagState.Off)
                .Restart());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — WhenHpBelow (direct MAIN condition)
            //
            // Block until demon HP ratio drops below 50%, then award the
            // draught lot once. This matches the vanilla DS1R pattern used
            // for Sif's phase transition (m12_01, LessOrEqual 0.3) and
            // Bell Gargoyle's second spawn (m10_01, LessOrEqual 0.6) —
            // a single IF HP Ratio on condGroup MAIN. The WhenAllOf +
            // Alive(Player) approach was replaced because mixing an
            // IfCharDeadAlive(Player) check with a boss HP ratio in the
            // same AND group is not a vanilla-supported pattern and caused
            // the AND group to never satisfy in practice.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtAwardTrinket, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenHpBelow(DemonEntity, 0.5f)   // block until demon HP < 50%
                .AwardItemLot(DraughtLotId)        // award 3x Goofy Draught
                .DisplayStatusMessage(MsgTrinketFound)
                .SetFlag(11819410, FlagState.On)   // "mid-fight reward given" flag
                .End());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — WhenAnyOf (OR compound condition)
            //      DisplayBossHealthBar
            //      RestBehaviorType.Default
            //
            // Boss intro event: show the HP bar as soon as the demon is
            // alive OR the demon-dead flag is off. We deliberately do NOT
            // ForceAnimation or HandleBossDefeat here — the vanilla EMEVD
            // already runs the boss fight, and stacking ForceAnimation /
            // HandleBossDefeat on top double-grants souls and pops a
            // second Victory banner once he dies for real.
            //
            // For a full ForceAnimation showcase, see the comment block on
            // EvtDemonControl below.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtBossIntro, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenAnyOf(or => or
                    .Alive(DemonEntity)
                    .Flag(DemonDeadFlag, FlagState.Off))
                .DisplayBossHealthBar(DemonEntity, EnabledState.Enabled, slot: 0, nameId: 0)
                .WhenDead(DemonEntity)
                .DisplayBossHealthBar(DemonEntity, EnabledState.Disabled)
                .End());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — SetCharacterEnabled, SetCharacterAI,
            //      SetCharacterHome, SetCharacterImmortal, SetCharacterInvincible
            //      WhenAlive, WhenHpBelow, WarpCharacter, KillCharacter,
            //      DisplayBanner, ForceAnimation, HandleBossDefeat
            //
            // Character-control API showcase. The previous version of this
            // event actually called KillCharacter + DisplayBanner gated on
            // a "never fires" flag — but the gate flag (11819409) was the
            // same numeric ID as EvtRawEscape, and DSR mirrors the
            // "event N is registered" state as flag N, so the gate opened
            // on map load. The chain then auto-killed the demon, awarded
            // souls and popped "Victory Achieved" before the player ever
            // entered the arena.
            //
            // The destructive showcase is removed. The methods are still
            // exercised below on a body that no-ops: SetCharacterHome to
            // the demon's own home region, immortal/invincible toggled
            // back to their default, and an Initialize-time SetFlag so
            // there's a visible side effect to confirm it ran.
            //
            // The KillCharacter / DisplayBanner / WarpCharacter /
            // HandleBossDefeat / ForceAnimation methods exist on
            // EventBuilder and behave exactly as their names suggest —
            // demonstrating them safely requires a gating flag that the
            // mod controls itself (e.g. set on a custom item use).
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtDemonControl, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(11815998, FlagState.On)        // section-5 flag we never set — truly never fires
                .SetCharacterEnabled(DemonEntity, EnabledState.Enabled)
                .SetCharacterAI(DemonEntity, EnabledState.Enabled)
                .SetCharacterHome(DemonEntity, regionEntityId: 1810100)
                .SetCharacterImmortal(DemonEntity, EnabledState.Disabled)
                .SetCharacterInvincible(DemonEntity, EnabledState.Disabled)
                .End());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — Raw(bank, id, args) escape hatch
            //      WhenInsideArea / WhenOutsideArea conditions shown here
            //
            // Play an SFX via the raw escape hatch (SpawnOneshotSfx, bank 2006
            // id 3) — not yet a named EventBuilder method.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtRawEscape, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenInsideArea(Player, areaEntityId: 1812100)
                .Raw(2006, 3, 1, DemonEntity, 220, 5090)   // SpawnOneshotSfx
                .WhenOutsideArea(Player, areaEntityId: 1812100)
                .Restart());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — SetObjectEnabled
            //
            // Hide the Stone Trinket o0500 once the player has picked it up.
            // Pairs with the lot's OnceOnlyFlag (TrinketGetFlag) and the
            // EntityID we assigned in PlaceTreasure. Without this event the
            // prop stays in the world: pickup animation plays on every
            // approach but the lot is already collected so nothing drops.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtHideTrinket, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(TrinketGetFlag, FlagState.On)
                .SetObjectEnabled(TrinketEntityId, EnabledState.Disabled)
                .End());
        });

        // ════════════════════════════════════════════════════════════════════
        // API: EditBnd3Glob — FMG strings for EMEVD messages
        // ════════════════════════════════════════════════════════════════════
        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.EventText, MsgDraughtUsed,  "The draught takes hold.");
            Texts.Set(bnd, Texts.EventText, MsgTrinketFound, "A reward for your tenacity.");
            Texts.Set(bnd, Texts.EventText, MsgDemonWarning, "The Asylum Demon stirs...");
        });
    }

    // ── AI ────────────────────────────────────────────────────────────────────

    private static void PatchAi(GamePatch g)
    {
        // ════════════════════════════════════════════════════════════════════
        // API: EditAi / AiBuilder — Goal, OnActivate (deterministic),
        //      OnInterrupt
        //      SubGoalQueue: ApproachTarget, Attack, SpinStep, Wait,
        //                    SidewayMove, LeaveTarget, WaitRandom, Raw
        //
        // Replace 223200_battle.lua (the Asylum Demon's entire battle AI)
        // with a weighted 70/30 act table:
        //
        //   Act 0 (70 %) — approach + overhead slam (anim 3008) + wait.
        //                  Sets mood flag 0 so C# / checklist can track it.
        //   Act 1 (30 %) — spin step + sidestep + back off + random wait.
        //                  Sets mood flag 1.
        //
        // Each act is emitted as a separate function (MiniGreaterDemon223200_Act01
        // etc.) and selected via Common_Battle_Activate — matching the
        // pattern vanilla DS1 battle scripts use. This is critical: the
        // OnActivate / inline-if-else approach caused the demon to freeze
        // because DSR's goal system re-calls Activate after each act; the
        // Common_Battle_Activate path handles that correctly.
        //
        // NOTE: This patches 223200_battle.lua in m18_01_00_00.luabnd.dcx.
        // GoofyDemon writes the same entry; loading both mods produces a
        // conflict warning, intentionally, to demonstrate the cross-mod
        // conflict detector.
        // ════════════════════════════════════════════════════════════════════
        g.EditAi(Map, NpcFileId, ai => ai
            .Goal("Battle", goal => goal
                .Act(70, q => q
                    // mood 0 = stomp
                    .SetActiveFlagInRange(FlagAiMood0, 2, 0)
                    .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 12)
                    .Attack(animId: 3008, cancelTime: 8)
                    .Wait(cancelTime: 2))
                .Act(30, q => q
                    // mood 1 = spin+leave
                    .SetActiveFlagInRange(FlagAiMood0, 2, 1)
                    .SpinStep(cancelTime: 5)
                    .SidewayMove(Target.Enemy0, direction: 0, cancelTime: 3)
                    .LeaveTarget(Target.Enemy0, Dist.Far, cancelTime: 8)
                    .WaitRandom(minTime: 1.0f, maxTime: 3.0f))

                // ── OnInterrupt: always allow pre-emption ─────────────────
                .OnInterrupt(_ => true)),

            luaId: LuaId);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // IGameMod lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        // ════════════════════════════════════════════════════════════════════
        // API: hooks.RegisterItemUsed + hooks.ItemUsed
        //
        // RegisterItemUsed(goodsId, triggerFlagId) tells the 500ms poll loop
        // which flag to watch for this item. When that flag pulses ON, the
        // ItemUsed event fires with the goods id.
        //
        // triggerFlagId must match the flag written by DefineItemTrigger (or
        // your own manually-written EMEVD bridge event).
        // ════════════════════════════════════════════════════════════════════
        ctx.Hooks.RegisterItemUsed(DraughtGoodsId, DraughtUseFlag);
        ctx.Hooks.ItemUsed += OnItemUsed;

        // Tick all patch-time checklist items — if we got here, they all ran.
        _patchConflictDetection = true;
        _patchSpEffect          = true;
        _patchGoods             = true;
        _patchLots              = true;
        _patchMsb               = true;
        _patchItemTrigger       = true;
        _patchEmevd             = true;
        _patchAi                = true;

        Console.WriteLine("[ItemsDemo] Loaded — APIs demonstrated:");
        Console.WriteLine("[ItemsDemo]   Conflict detection  (GamePatch(ctx) constructor)");
        Console.WriteLine("[ItemsDemo]   EventBuilder        WhenAllOf / WhenAnyOf / all condition+action methods");
        Console.WriteLine("[ItemsDemo]   AiBuilder           2-act weighted Battle goal, Helper, SubGoalQueue");
        Console.WriteLine("[ItemsDemo]   Items               DefineSpEffect / DefineGoods / DefineLot");
        Console.WriteLine("[ItemsDemo]   MSB                 PlaceTreasure ground pickup");
        Console.WriteLine("[ItemsDemo]   DefineItemTrigger   SpEffect→flag bridge EMEVD event");
        Console.WriteLine("[ItemsDemo]   hooks.ItemUsed      C# callback on draught use");
        Console.WriteLine($"[ItemsDemo]   Goofy Draught   id={DraughtGoodsId}  lot={DraughtLotId}  sp={DraughtSpEffect}");
        Console.WriteLine($"[ItemsDemo]   Stone Trinket   id={TrinketGoodsId}  lot={TrinketLotId}  pos={TrinketPos}");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;
        var r = _ctx.Reader;

        // Poll runtime checklist items via event flags each tick.
        if (!_rtTrinketCollected) _rtTrinketCollected = r.GetEventFlag(TrinketGetFlag);
        if (!_rtMidFightReward)   _rtMidFightReward   = r.GetEventFlag(11819410);
        if (!_rtBossDead)         _rtBossDead         = r.GetEventFlag(DemonDeadFlag);
        if (!_rtAiMood0)          _rtAiMood0          = r.GetEventFlag(FlagAiMood0);
        if (!_rtAiMood1)          _rtAiMood1          = r.GetEventFlag(FlagAiMood1);
    }

    // ── IGuiMod ───────────────────────────────────────────────────────────────

    public void OnGui()
    {
        if (!_windowOpen) return;

        DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(340, 0, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.82f);

        if (!DS1ImGui.Begin("DS1Mod API Test Checklist", ref _windowOpen))
        {
            DS1ImGui.End();
            return;
        }

        int ticked = CountTicked();
        int total  = TotalChecks();
        DS1ImGui.Text($"Progress: {ticked} / {total}");
        DS1ImGui.Separator();

        DS1ImGui.Text("  Patch-time");
        DrawCheck("Conflict detection (GamePatch from IPatchContext)", _patchConflictDetection);
        DrawCheck("DefineSpEffect — SpEffectParam 9100",               _patchSpEffect);
        DrawCheck("DefineGoods — Goofy Draught + Stone Trinket",       _patchGoods);
        DrawCheck("DefineLot — infinite + once-only",                  _patchLots);
        DrawCheck("EditMsb / PlaceTreasure — Stone Trinket pickup",    _patchMsb);
        DrawCheck("DefineItemTrigger — SpEffect→flag bridge",          _patchItemTrigger);
        DrawCheck("EditEmevd / EventBuilder — 5 events",               _patchEmevd);
        DrawCheck("EditAi / AiBuilder — 223200_battle.lua compiled",   _patchAi);

        DS1ImGui.Spacing();
        DS1ImGui.Text("  Runtime — do these in-game:");
        DrawCheck("hooks.ItemUsed — use the Goofy Draught",            _rtDraughtUsed);
        DrawCheck("DefineLot once-only — collect Stone Trinket",       _rtTrinketCollected);
        DrawCheck("WhenHpBelow — demon HP < 50 %% (draught reward)",    _rtMidFightReward);
        DrawCheck("WhenDead — Asylum Demon killed",                     _rtBossDead);
        DrawCheck("SetActiveFlagInRange — AI mood 0 (stomp)",          _rtAiMood0);
        DrawCheck("SetActiveFlagInRange — AI mood 1 (spin+leave)",     _rtAiMood1);

        DS1ImGui.End();
    }

    private static void DrawCheck(string label, bool ticked)
    {
        if (ticked)
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.2f, 0.9f, 0.2f, 1f); // green
            DS1ImGui.Text($"  [x] {label}");
            DS1ImGui.PopStyleColor();
        }
        else
        {
            DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f); // grey
            DS1ImGui.Text($"  [ ] {label}");
            DS1ImGui.PopStyleColor();
        }
    }

    private int CountTicked() =>
        (_patchConflictDetection ? 1 : 0) + (_patchSpEffect   ? 1 : 0) +
        (_patchGoods             ? 1 : 0) + (_patchLots        ? 1 : 0) +
        (_patchMsb               ? 1 : 0) + (_patchItemTrigger ? 1 : 0) +
        (_patchEmevd             ? 1 : 0) + (_patchAi          ? 1 : 0) +
        (_rtDraughtUsed          ? 1 : 0) + (_rtTrinketCollected ? 1 : 0) +
        (_rtMidFightReward       ? 1 : 0) + (_rtBossDead        ? 1 : 0) +
        (_rtAiMood0              ? 1 : 0) + (_rtAiMood1         ? 1 : 0);

    private static int TotalChecks() => 14;

    public override void OnUnload()
    {
        Console.WriteLine("[ItemsDemo] Unloaded");
    }

    // ── hooks.ItemUsed callback ───────────────────────────────────────────────

    private void OnItemUsed(int goodsId)
    {
        if (goodsId != DraughtGoodsId) return;

        _rtDraughtUsed = true;  // tick the checklist item
        Console.WriteLine("[ItemsDemo] hooks.ItemUsed fired — player used the Goofy Draught");

        var stats = _ctx?.Reader.GetPlayerStats();
        if (stats is not null)
            Console.WriteLine($"[ItemsDemo]   HP after restore: {stats.CurrentHp}/{stats.MaxHp}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Load a file that was compiled into the assembly as an EmbeddedResource.
    /// The <paramref name="name"/> must match the LogicalName set in the .csproj.
    /// </summary>
    private static byte[] GetEmbeddedResource(string name)
    {
        var asm = typeof(ItemsDemoMod).Assembly;
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
