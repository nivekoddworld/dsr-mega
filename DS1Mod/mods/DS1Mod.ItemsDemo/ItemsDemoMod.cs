using System.Numerics;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;

namespace DS1Mod.ItemsDemo;

/// <summary>
/// Items API demo mod — exercises everything added to DS1Mod.Modding on 2026-06-05:
///
///   DefineSpEffect    — SpEffectDef with HP restore
///   DefineGoods       — ItemDef wired to a SpEffect
///   DefineLot         — ItemLotParam row, once-only via flag
///   EditMsb           — PlaceTreasure places a ground pickup in the Asylum
///   DefineItemTrigger — EMEVD bridge: SpEffect active → flag pulse
///   EMEVD response    — in-game engine event fires when the item is used
///   hooks.ItemUsed    — C# callback fires when the item is used
///
/// Items are placed in the Undead Asylum (m18_01_00_00).
/// ID ranges used: goods 8100, SpEffect 9100, lots 8600/8601, flags 11819400-11819402.
/// </summary>
public sealed class ItemsDemoMod : ModBase, IGamePatcher
{
    public override string Name    => "Items API Demo";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    // ── Item 1: Goofy Draught — HP restore consumable ───────────────────────
    // Uses DefineSpEffect + DefineGoods (SpEffectId wiring) + DefineLot.
    // Dropped once on Asylum Demon death (same event as GoofyDemon's trinket,
    // but uses a different lot/flag so they don't conflict).
    private const int DraughtGoodsId  = 8100;
    private const int DraughtSpEffect = 9100;
    private const int DraughtLotId    = 8600;
    private const int DraughtGetFlag  = 11819400;  // once-only: obtained flag
    private const int DraughtUseFlag  = 11819401;  // pulses ON each time item is used
    private const int DraughtUseEvent = 11819401;  // EMEVD event id = flag id (convention)
    private const int DraughtAwardEvent = 11819400;

    // ── Item 2: Stone Trinket — key item placed as ground pickup ────────────
    // Uses DefineGoods (key item, no SpEffect) + DefineLot + EditMsb/PlaceTreasure.
    private const int TrinketGoodsId = 8101;
    private const int TrinketLotId   = 8601;
    private const int TrinketGetFlag = 11819402;

    // ── Map / entity constants ───────────────────────────────────────────────
    private const string Map        = "m18_01_00_00";
    private const int    DemonDead  = 16;       // Asylum Demon kill flag
    private const int    Player     = 10000;

    // Pickup position: on the ledge near the start of the Asylum, clearly reachable
    private static readonly Vector3 TrinketPos = new(52f, -2f, 103f);

    private IModContext? _ctx;

    // ── IGamePatcher ─────────────────────────────────────────────────────────

    public void Patch(IPatchContext ctx)
    {
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");
        var g = new GamePatch(ctx);

        // ── SpEffect for the Draught (400 HP restore, instant) ───────────────
        g.DefineSpEffect(paramdefs, new SpEffectDef
        {
            Id             = DraughtSpEffect,
            DonorId        = 7000,
            Duration       = 0f,
            HpRecoverPoint = 400,
        });

        // ── Goofy Draught: consumable wired to the SpEffect ──────────────────
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = DraughtGoodsId,
            DonorId     = 384,               // clone from Homeward Bone (also consumable)
            SpEffectId  = DraughtSpEffect,   // sets goodsType=0 + refId_default automatically
            Name        = "Goofy Draught",
            Description = "Restores 400 HP and a small amount of dignity.",
            LongDesc    = "Brewed from the tears of the Asylum Demon. "
                        + "He cried when he realised he'd been doing the hokey pokey wrong this whole time.\n\n"
                        + "Consume to restore HP.",
            MaxCount    = 5,
        });

        // ── Lot: one Draught, awarded once on demon death ─────────────────────
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = DraughtLotId,
            ItemId       = DraughtGoodsId,
            Category     = LotCategory.Goods,
            Count        = 1,
            OnceOnlyFlag = DraughtGetFlag,
        });

        // ── Stone Trinket: key item (no SpEffect) ─────────────────────────────
        g.DefineGoods(paramdefs, new ItemDef
        {
            Id          = TrinketGoodsId,
            DonorId     = 384,
            Name        = "Stone Trinket",
            Description = "A small stone. It does nothing.",
            LongDesc    = "Found on the floor of the Undead Asylum. Someone left it there.\n\n"
                        + "It has no function whatsoever. You picked it up anyway.",
            MaxCount    = 1,
            Configure   = row => row["goodsType"].Value = (byte)4,  // key item
        });

        // ── Lot: one Trinket, once-only ───────────────────────────────────────
        g.DefineLot(paramdefs, new LotDef
        {
            LotId        = TrinketLotId,
            ItemId       = TrinketGoodsId,
            Category     = LotCategory.Goods,
            Count        = 1,
            OnceOnlyFlag = TrinketGetFlag,
        });

        // ── MSB: place the Trinket as a ground pickup in the Asylum ───────────
        g.EditMsb(Map, msb => msb
            .PlaceTreasure(lotId: TrinketLotId, position: TrinketPos));

        // ── EMEVD: award Draught once when Asylum Demon dies ──────────────────
        g.EditEmevd(Map, emevd =>
        {
            emevd.DefineEvent(DraughtAwardEvent, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(DemonDead, FlagState.On)
                .AwardItemLot(DraughtLotId)
                .End());

            // ── EMEVD: bridge item use → event flag (for C# callback + in-game response) ──
            // DefineItemTrigger writes a Restart event:
            //   wait for SpEffect 9100 on player → set flag ON → wait for expiry → set flag OFF → restart
            emevd.DefineEvent(DraughtUseEvent, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenCharacterHasSpEffect(Player, DraughtSpEffect)
                .SetFlag(DraughtUseFlag, FlagState.On)
                .WhenCharacterLosesSpEffect(Player, DraughtSpEffect)
                .SetFlag(DraughtUseFlag, FlagState.Off)
                .Restart());

            // ── EMEVD: in-game response when the Draught is used ─────────────
            // This fires entirely inside the game engine — no .NET code involved.
            // When the player uses the Draught, show a message and set a permanent flag.
            emevd.DefineEvent(11819403, EMEVD.Event.RestBehaviorType.Restart, ev => ev
                .WhenFlag(DraughtUseFlag, FlagState.On)
                .DisplayMessage(6900750)                     // "The draught takes hold." (FMG id)
                .SetFlag(11819404, FlagState.On)             // permanent "used draught at least once" flag
                .WhenFlag(DraughtUseFlag, FlagState.Off)
                .Restart());
        });

        // ── FMG: add the "on use" response message (Event_text lives in menu.msgbnd) ──
        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
            Texts.Set(bnd, Texts.EventText, 6900750, "The draught takes hold."));
    }

    // ── IGameMod ──────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;

        // Register the Draught for the ItemUsed C# hook.
        // triggerFlagId must match what DefineItemTrigger (or our manual event) uses.
        ctx.Hooks.RegisterItemUsed(DraughtGoodsId, DraughtUseFlag);
        ctx.Hooks.ItemUsed += OnItemUsed;

        Console.WriteLine("[ItemsDemo] Loaded — Goofy Draught (id=8100) and Stone Trinket (id=8101) registered");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[ItemsDemo] Unloaded");
    }

    // ── Item used callback (C# / .NET domain) ────────────────────────────────

    private void OnItemUsed(int goodsId)
    {
        if (goodsId != DraughtGoodsId) return;

        // This runs in the 500ms poll loop — anything goes here.
        Console.WriteLine("[ItemsDemo] Player used Goofy Draught — C# callback fired!");

        // Example: check how much HP the player has now (after the 400 HP restore)
        var stats = _ctx?.Reader.GetPlayerStats();
        if (stats is not null)
            Console.WriteLine($"[ItemsDemo]   HP after use: {stats.CurrentHp}/{stats.MaxHp}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[] GetEmbeddedResource(string name)
    {
        var asm    = typeof(ItemsDemoMod).Assembly;
        using var s = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
