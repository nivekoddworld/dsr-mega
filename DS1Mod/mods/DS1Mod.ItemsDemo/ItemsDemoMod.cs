using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.ItemsDemo;

/// <summary>
/// Demo mod that exercises every API added on 2026-06-05.
///
/// APIs under test
/// ───────────────
///  Patch-time (DS1Mod.Modding):
///    DefineSpEffect       — create a SpEffectParam row with named fields
///    DefineGoods          — create an EquipParamGoods row + all-locale FMG strings
///    ItemDef.SpEffectId   — auto-wire consumable goodsType + refId_default
///    ItemDef.Configure    — raw row callback for fields not covered by named props
///    DefineLot            — create an ItemLotParam row (once-only and infinite)
///    EditMsb / PlaceTreasure — add a ground pickup object + Treasure event to a map
///    DefineItemTrigger    — write the EMEVD SpEffect→flag bridge event
///    WhenCharacterHasSpEffect / WhenCharacterLosesSpEffect — new EventBuilder methods
///
///  Runtime (DS1Mod.Core):
///    hooks.RegisterItemUsed — register (goodsId, triggerFlagId) pair
///    hooks.ItemUsed         — C# event fires when the item is used
///
/// All items are placed in the Undead Asylum (m18_01_00_00).
/// </summary>
public sealed class ItemsDemoMod : ModBase, IGamePatcher
{
    public override string Name    => "Items API Demo";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    // ── Item A: Goofy Draught ────────────────────────────────────────────────
    // A consumable HP potion. Tests: DefineSpEffect, DefineGoods+SpEffectId,
    // DefineLot (infinite), DefineItemTrigger, WhenCharacterHasSpEffect,
    // WhenCharacterLosesSpEffect, RegisterItemUsed, hooks.ItemUsed.
    private const int DraughtGoodsId   = 8100;
    private const int DraughtSpEffect  = 9100;   // SpEffectParam id
    private const int DraughtLotId     = 8600;   // infinite lot (no once-only flag)
    private const int DraughtUseFlag   = 11819401; // pulses ON every time item is used

    // ── Item B: Stone Trinket ────────────────────────────────────────────────
    // A key item with no use effect, placed as a ground pickup in the Asylum.
    // Tests: DefineGoods+Configure (goodsType=4), DefineLot (once-only),
    // EditMsb / PlaceTreasure.
    private const int TrinketGoodsId   = 8101;
    private const int TrinketLotId     = 8601;
    private const int TrinketGetFlag   = 11819402; // once-only obtained flag

    // ── Item C: Demon Drop ───────────────────────────────────────────────────
    // A key item awarded by EMEVD on Asylum Demon death (tests AwardItemLot
    // via DefineEvent, DefineLot once-only, DefineGoods).
    private const int DropGoodsId      = 8102;
    private const int DropLotId        = 8602;
    private const int DropGetFlag      = 11819403; // once-only obtained flag

    // ── EMEVD event IDs ──────────────────────────────────────────────────────
    private const int EvtAwardDrop     = 11819403; // award Demon Drop on boss death
    private const int EvtUseBridge     = 11819401; // DefineItemTrigger writes this
    private const int EvtUseResponse   = 11819404; // in-game engine response on use

    // ── Map constants ────────────────────────────────────────────────────────
    private const string Map           = "m18_01_00_00";
    private const int    DemonDeadFlag = 16;        // Asylum Demon kill flag
    private const int    Player        = 10000;     // player entity id

    // Trinket pickup position: ledge near the Asylum start
    private static readonly Vector3 TrinketPos = new(52f, -2f, 103f);

    private IModContext? _ctx;

    // ── IGamePatcher ──────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");
        var g = new GamePatch(ctx);

        // ════════════════════════════════════════════════════════════════════
        // API: DefineSpEffect
        // Creates a SpEffectParam row. Duration=0 → instant (fires once and
        // disappears). HpRecoverPoint restores flat HP on application.
        // ════════════════════════════════════════════════════════════════════
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id             = DraughtSpEffect,
            DonorId        = 7000,
            Duration       = 0f,
            HpRecoverPoint = 400,
        });

        // ════════════════════════════════════════════════════════════════════
        // API: DefineGoods + ItemDef.SpEffectId
        // SpEffectId set → auto-wires goodsType=0 (consumable) and
        // refId_default=SpEffectId. All locale FMG strings written automatically.
        // ════════════════════════════════════════════════════════════════════
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = DraughtGoodsId,
            DonorId     = 384,
            SpEffectId  = DraughtSpEffect,
            Name        = "Goofy Draught",
            Description = "Restores 400 HP. Tastes of regret.",
            LongDesc    = "Brewed from the tears of the Asylum Demon after he lost a staring contest with a Hollow.\n\n"
                        + "Consume to restore HP.",
            MaxCount    = 5,
        });

        // ════════════════════════════════════════════════════════════════════
        // API: DefineGoods + ItemDef.Configure
        // Configure callback sets fields not covered by ItemDef named props.
        // goodsType=4 → key item (no use effect, stays in inventory).
        // ════════════════════════════════════════════════════════════════════
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = TrinketGoodsId,
            DonorId     = 384,
            Name        = "Stone Trinket",
            Description = "A small stone. It does nothing.",
            LongDesc    = "Found on the floor of the Undead Asylum.\n\nIt has no function whatsoever. You picked it up anyway.",
            MaxCount    = 1,
            Configure   = row => row["goodsType"].Value = (byte)4,
        });

        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = DropGoodsId,
            DonorId     = 384,
            Name        = "Demon's Memo",
            Description = "A note from the Asylum Demon.",
            LongDesc    = "Scrawled in crayon: \"gone dancing. do not disturb.\"\n\n"
                        + "Left behind when the demon decided he had better things to do.",
            MaxCount    = 1,
            Configure   = row => row["goodsType"].Value = (byte)4,
        });

        // ════════════════════════════════════════════════════════════════════
        // API: DefineLot
        // Infinite lot (no OnceOnlyFlag) for the consumable draught.
        // Once-only lots for key items (flag prevents re-acquisition).
        // ════════════════════════════════════════════════════════════════════
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = DraughtLotId,
            ItemId       = DraughtGoodsId,
            Category     = LotCategory.Goods,
            Count        = 3,
            OnceOnlyFlag = -1,              // infinite — respawns each time
        });

        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = TrinketLotId,
            ItemId       = TrinketGoodsId,
            Category     = LotCategory.Goods,
            Count        = 1,
            OnceOnlyFlag = TrinketGetFlag,  // once-only
        });

        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = DropLotId,
            ItemId       = DropGoodsId,
            Category     = LotCategory.Goods,
            Count        = 1,
            OnceOnlyFlag = DropGetFlag,     // once-only
        });

        // ════════════════════════════════════════════════════════════════════
        // API: EditMsb / PlaceTreasure
        // Registers the o0500 pickup model, creates a Part.Object at TrinketPos,
        // and creates a Treasure event pointing to TrinketLotId.
        // Collision is borrowed from the nearest existing o0500 object.
        // ════════════════════════════════════════════════════════════════════
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(lotId: TrinketLotId, position: TrinketPos));

        // ════════════════════════════════════════════════════════════════════
        // API: DefineItemTrigger
        // Writes a Restart EMEVD event (id = DraughtUseFlag) that:
        //   1. Waits for SpEffect DraughtSpEffect to be active on entity Player
        //   2. Sets DraughtUseFlag ON
        //   3. Waits for the SpEffect to expire
        //   4. Sets DraughtUseFlag OFF → restart
        // Internally uses WhenCharacterHasSpEffect / WhenCharacterLosesSpEffect.
        // ════════════════════════════════════════════════════════════════════
        g.DefineItemTrigger(Map, spEffectId: DraughtSpEffect, triggerFlagId: DraughtUseFlag);

        // ════════════════════════════════════════════════════════════════════
        // API: EventBuilder — WhenCharacterHasSpEffect / WhenCharacterLosesSpEffect
        //      (also exercised internally by DefineItemTrigger above, but shown
        //       here explicitly so the usage is visible)
        //
        // Also tests: AwardItemLot via EMEVD (Demon Drop on boss death),
        //             standard WhenFlag, DisplayMessage, SetFlag.
        // ════════════════════════════════════════════════════════════════════
        g.EditEmevd(Map, emevd =>
        {
            // Award Demon Drop once when Asylum Demon dies
            emevd.DefineEvent(EvtAwardDrop, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(DemonDeadFlag, FlagState.On)
                .AwardItemLot(DropLotId)
                .End());

            // In-game engine response when draught is used:
            //   WhenFlag watches the flag pulsed by DefineItemTrigger
            //   DisplayMessage shows text inside the game engine (no .NET)
            //   SetFlag records "used at least once" permanently
            emevd.DefineEvent(EvtUseResponse, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenFlag(DraughtUseFlag, FlagState.On)
                .DisplayMessage(6900750)                // "The draught takes hold."
                .SetFlag(11819405, FlagState.On)        // permanent "used draught" flag
                .WhenFlag(DraughtUseFlag, FlagState.Off)
                .Restart());
        });

        // ── FMG: add the display message text ────────────────────────────────
        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
            Texts.Set(bnd, Texts.EventText, 6900750, "The draught takes hold."));
    }

    // ── IGameMod ──────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        // ════════════════════════════════════════════════════════════════════
        // API: hooks.RegisterItemUsed + hooks.ItemUsed
        // Register the (goodsId, triggerFlagId) pair written by DefineItemTrigger.
        // ItemUsed fires (in the 500ms poll loop) when the flag pulses ON.
        // ════════════════════════════════════════════════════════════════════
        ctx.Hooks.RegisterItemUsed(DraughtGoodsId, DraughtUseFlag);
        ctx.Hooks.ItemUsed += OnItemUsed;

        Console.WriteLine("[ItemsDemo] Loaded");
        Console.WriteLine($"[ItemsDemo]   Goofy Draught  id={DraughtGoodsId}  lot={DraughtLotId}  speffect={DraughtSpEffect}");
        Console.WriteLine($"[ItemsDemo]   Stone Trinket  id={TrinketGoodsId}  lot={TrinketLotId}  (ground pickup at {TrinketPos})");
        Console.WriteLine($"[ItemsDemo]   Demon's Memo   id={DropGoodsId}     lot={DropLotId}     (awarded on demon death)");
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[] GetEmbeddedResource(string name)
    {
        var asm = typeof(ItemsDemoMod).Assembly;
        using var s  = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
