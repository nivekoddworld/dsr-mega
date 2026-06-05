using System.Numerics;
using DS1Mod.Core;
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
public sealed class ItemsDemoMod : ModBase, IGamePatcher
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
    private const string LuaId     = "DemoAI";   // Lua identifier prefix

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

    // FMG message IDs (Event_text)
    private const int MsgDraughtUsed   = 6900760;
    private const int MsgTrinketFound  = 6900761;
    private const int MsgDemonWarning  = 6900762;

    // World position — ledge near Asylum start
    private static readonly Vector3 TrinketPos = new(52f, -2f, 103f);

    private IModContext? _ctx;

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
            DonorId        = 7000,           // clone from generic buff row
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
        // borrowed from the nearest existing o0500 object automatically.
        // ════════════════════════════════════════════════════════════════════
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(lotId: TrinketLotId, position: TrinketPos));
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
            // API: EventBuilder — WhenAllOf (AND compound condition)
            //
            // Block until BOTH conditions are simultaneously true:
            //   • demon HP below 50 %
            //   • player is alive
            // Then award the infinite draught lot as a mid-fight reward.
            //
            // DS1 supports up to 7 AND groups per event.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtAwardTrinket, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenAllOf(and => and
                    .HpBelow(DemonEntity, 0.5f)   // demon at half health
                    .Alive(Player))               // player still alive
                .AwardItemLot(DraughtLotId)       // award infinite draught lot
                .DisplayStatusMessage(MsgTrinketFound)
                .SetFlag(11819410, FlagState.On)  // "mid-fight reward given" flag
                .End());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — WhenAnyOf (OR compound condition)
            //      DisplayBossHealthBar, ForceAnimation, SetCharacterImmortal,
            //      SetCharacterInvincible, HandleBossDefeat
            //      RestBehaviorType.Default
            //
            // Boss intro event: show HP bar as soon as the demon is alive OR
            // the player enters the arena. Then force an intro animation.
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtBossIntro, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenAnyOf(or => or
                    .Alive(DemonEntity)
                    .Flag(DemonDeadFlag, FlagState.Off))
                .DisplayBossHealthBar(DemonEntity, EnabledState.Enabled, slot: 0, nameId: 0)
                .ForceAnimation(DemonEntity, animId: 3000)
                // handle normal defeat sequence once dead
                .WhenDead(DemonEntity)
                .DisplayBossHealthBar(DemonEntity, EnabledState.Disabled)
                .HandleBossDefeat(DemonEntity)
                .End());

            // ════════════════════════════════════════════════════════════════
            // API: EventBuilder — SetCharacterEnabled, SetCharacterAI,
            //      SetCharacterHome, KillCharacter, WarpCharacter
            //      WhenAlive, WhenHpBelow, WhenInsideArea, WhenOutsideArea
            //
            // Character control demo — exercises every character-action method.
            // This is a synthetic event for demonstration; it is not triggered
            // in normal gameplay (it waits on flag 11819409 which nothing sets).
            // ════════════════════════════════════════════════════════════════
            emevd.DefineEvent(EvtDemonControl, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(11819409, FlagState.On)        // gated — never fires normally
                .SetCharacterEnabled(DemonEntity, EnabledState.Enabled)
                .SetCharacterAI(DemonEntity, EnabledState.Enabled)
                .SetCharacterHome(DemonEntity, regionEntityId: 1810100)
                .SetCharacterImmortal(DemonEntity, EnabledState.Disabled)
                .SetCharacterInvincible(DemonEntity, EnabledState.Disabled)
                .WhenAlive(DemonEntity)
                .WhenHpBelow(DemonEntity, 0.1f)
                .WarpCharacter(DemonEntity, destEntityId: 1810100)
                .KillCharacter(DemonEntity, awardSouls: true)
                .DisplayBanner(bannerType: 1)            // "Victory Achieved"
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
        // API: Luac50.Configure
        //
        // Override the luac50 binary path before any Compile call.
        // Only needed when the binary is not in tools/luac50 relative to the
        // app, or not on PATH.
        // ════════════════════════════════════════════════════════════════════
        Luac50.Configure("/home/user/dsr-mega/tools/luac50");

        // ════════════════════════════════════════════════════════════════════
        // API: EditAi / AiBuilder — Goal, Act (weighted), OnInterrupt, Helper
        //      SubGoalQueue: ApproachTarget, Attack, SpinStep, Wait,
        //                    SidewayMove, LeaveTarget, WaitRandom,
        //                    SetEventFlag, SetActiveFlagInRange, Raw
        //
        // Give the Asylum Demon a 2-act weighted battle AI:
        //   Act 1 (70 %): approach + slam attack
        //   Act 2 (30 %): spin-step + sideways dodge + leave + wait
        //
        // A Helper function ClearMoods is defined and called from each act to
        // keep a pair of mood flags consistent. SetActiveFlagInRange clears the
        // range and lights exactly one flag.
        //
        // NOTE: This patches 223200_battle.lua in m18_01_00_00.luabnd.dcx.
        // GoofyDemon writes the same entry. Running both mods will produce a
        // conflict warning in the host log — this is intentional, to demonstrate
        // that the conflict detection system catches the overlap.
        // ════════════════════════════════════════════════════════════════════
        g.EditAi(Map, NpcFileId, ai => ai
            .Goal("Battle", goal => goal

                // ── Helper ────────────────────────────────────────────────
                // A named Lua helper emitted before the goal functions.
                // Called from each act to synchronise the AI mood flags.
                .Helper("ClearMoods",
                    "for _i = 0, 1 do\n" +
                    "    ai:SetEventFlag(" + FlagAiMood0 + " + _i, false)\n" +
                    "end")

                // ── Act 1 (70 %): approach and slam ───────────────────────
                .Act(70, q => q
                    .Raw("ClearMoods(ai, goal)")             // call the helper
                    .SetActiveFlagInRange(FlagAiMood0, 2, 0) // mood=0 (stomp)
                    .ApproachTarget(Target.Enemy0, Dist.Middle, cancelTime: 12)
                    .Attack(animId: 3008, cancelTime: 8)     // overhead slam
                    .Wait(cancelTime: 2))

                // ── Act 2 (30 %): spin-step + sidestep + leave + rest ─────
                .Act(30, q => q
                    .Raw("ClearMoods(ai, goal)")
                    .SetActiveFlagInRange(FlagAiMood0, 2, 1) // mood=1 (spin)
                    .SpinStep(cancelTime: 5)
                    .SidewayMove(Target.Enemy0, direction: 0, cancelTime: 3)
                    .LeaveTarget(Target.Enemy0, Dist.Far, cancelTime: 8)
                    .WaitRandom(minTime: 1.0f, maxTime: 3.0f)
                    .SetEventFlag(FlagAiMood0, on: false))

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

    public override void OnUnload()
    {
        Console.WriteLine("[ItemsDemo] Unloaded");
    }

    // ── hooks.ItemUsed callback ───────────────────────────────────────────────

    private void OnItemUsed(int goodsId)
    {
        if (goodsId != DraughtGoodsId) return;

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
