using System.Numerics;
using SoulsFormats;

namespace DS1Mod.Modding;

// ── ItemDef ───────────────────────────────────────────────────────────────────

public sealed class ItemDef
{
    /// <summary>
    /// EquipParamGoods row ID. Must be unique and unused (commonly 8000+ for mods).
    /// This is the actual item entry in the game database.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Base row to clone from (copies all underlying DS1 item behaviour).
    /// Example: Estus Flask (384) for healing consumables.
    /// </summary>
    public int DonorId { get; set; } = 384;

    /// <summary>Display name shown in inventory.</summary>
    public string Name { get; set; } = "Unnamed Item";

    /// <summary>Short inventory description (first line).</summary>
    public string Description { get; set; } = "";

    /// <summary>Full lore description shown in detailed view.</summary>
    public string LongDesc { get; set; } = "";

    /// <summary>
    /// Maximum stack size.
    /// 1 = key item style, higher values = consumables (Estus-like, throwables, etc.)
    /// </summary>
    public ushort MaxCount { get; set; } = 1;

    // ─────────────────────────────────────────────────────────────
    // CORE GAMEPLAY BEHAVIOUR
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// SpEffect triggered on use.
    /// If set, item behaves like a consumable trigger.
    /// -1 = no effect (key item style or scripted behavior)
    /// </summary>
    public int SpEffectId { get; set; } = -1;

    /// <summary>
    /// Explicit goods type override.
    /// Controls classification in DS1 systems.
    /// 0 = Consumable (quick-use eligible)
    /// 1 = Event (usually NOT quick-use)
    /// 2 = Material (non-usable)
    /// 3 = Soul
    /// </summary>
    public byte? GoodsType { get; set; } = null;

    /// <summary>
    /// Whether item can be equipped into quick-use slots (D-pad cycling).
    /// NOTE: DS1 also applies hidden filters (use animation + type + behavior).
    /// </summary>
    public bool AllowQuickUse { get; set; } = true;

    /// <summary>
    /// Whether item is consumable (removed on use).
    /// Required for Estus-style / healing items.
    /// </summary>
    public bool IsConsume { get; set; } = true;

    /// <summary>
    /// Whether item can be stored in bottomless box.
    /// </summary>
    public bool IsDeposit { get; set; } = true;

    /// <summary>
    /// Whether item can be dropped into the world.
    /// </summary>
    public bool IsDrop { get; set; } = true;

    // ─────────────────────────────────────────────────────────────
    // UI / INPUT BEHAVIOUR
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Animation used when item is consumed.
    /// Critical for quick-use eligibility.
    /// Example: Drink, Throw, Repair, etc.
    /// </summary>
    public byte? UseAnim { get; set; } = null;

    /// <summary>
    /// Menu behaviour when item is used.
    /// 0 = none
    /// 1 = Yes/No prompt
    /// 11 = warp selection etc.
    /// </summary>
    public byte? OpenMenuType { get; set; } = null;

    /// <summary>
    /// Behavior ID for special scripted actions.
    /// Used for non-SpEffect item logic.
    /// </summary>
    public int BehaviorId { get; set; } = 0;

    // ─────────────────────────────────────────────────────────────
    // ADVANCED / ENGINE EDGE CASES
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Callback after cloning donor row.
    /// Use for raw PARAM overrides not exposed in this abstraction.
    /// </summary>
    public Action<PARAM.Row>? Configure { get; set; }

    /// <summary>
    /// Internal sorting ID for inventory ordering.
    /// </summary>
    public int SortId { get; set; } = 0;

    /// <summary>
    /// Icon ID used in inventory UI.
    /// </summary>
    public ushort IconId { get; set; }

    /// <summary>
    /// Model ID used for world representation.
    /// </summary>
    public ushort ModelId { get; set; }

    /// <summary>
    /// Maximum number of this item player can hold.
    /// </summary>
    public short MaxHold { get; set; } = 999;
}

// ── LotDef ────────────────────────────────────────────────────────────────────

/// <summary>
/// DS1 ItemLotParam definition (supports multi-slot drops).
/// </summary>
public sealed class LotDef
{
    /// <summary>ItemLotParam row ID. Must be unique.</summary>
    public int LotId { get; set; }

    /// <summary>
    /// Primary item ID to drop (Goods / Weapon / Armor / Ring).
    /// Used for slot 01 unless multi-slot is configured.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Item category bitmask (IMPORTANT: DS1 uses bitfield values).
    /// Examples:
    /// Goods = 1073741824
    /// Weapons = 0
    /// Armor = 268435456
    /// Rings = 536870912
    /// </summary>
    public int Category { get; set; } = LotCategory.Goods;

    /// <summary>How many items to give per successful roll.</summary>
    public byte Count { get; set; } = 1;

    /// <summary>
    /// If >= 0, sets a flag after item acquisition (prevents re-drop).
    /// -1 = repeatable drop.
    /// </summary>
    public int OnceOnlyFlag { get; set; } = -1;

    /// <summary>
    /// Drop weight (higher = more likely slot selection).
    /// Default matches vanilla baseline.
    /// </summary>
    public ushort Weight { get; set; } = 100;

    /// <summary>
    /// Whether item is affected by Item Discovery / Luck stat.
    /// </summary>
    public bool EnableLuck { get; set; } = true;

    /// <summary>
    /// Optional rarity SFX tier (0–3 typical in DS1).
    /// </summary>
    public byte Rarity { get; set; } = 0;

    /// <summary>
    /// Optional multi-slot support (1–8 entries).
    /// If null, only slot 01 is used.
    /// </summary>
    public List<LotEntry>? Entries { get; set; }
}

/// <summary>
/// Represents a single slot inside an ItemLotParam row.
/// </summary>
public sealed class LotEntry
{
    public int ItemId { get; set; }
    public int Category { get; set; }
    public byte Count { get; set; } = 1;
    public ushort Weight { get; set; } = 100;
}

// ── MsbEditor ─────────────────────────────────────────────────────────────────

/// <summary>
/// High-level editor for a DS1 MSB (map studio binary).
/// Obtain via <see cref="GamePatch.EditMsb"/>.
/// </summary>
public sealed class MsbEditor
{
    private readonly MSB1 _msb;
    private readonly string _mapId;

    internal MsbEditor(MSB1 msb, string mapId)
    {
        _msb   = msb;
        _mapId = mapId;
    }

    /// <summary>
    /// Place a ground-pickup item (the glowing item-on-floor object) in the world.
    ///
    /// <para>Creates one <c>o0500</c> object part at <paramref name="position"/>
    /// and one Treasure event pointing to it with <paramref name="lotId"/>.</para>
    ///
    /// <para><paramref name="collisionName"/> controls which collision mesh loads
    /// this object. If omitted, the nearest existing <c>o0500</c> object's
    /// collision is reused — safe when placing near an existing pickup.</para>
    ///
    /// <para>Example:</para>
    /// <code>
    /// g.EditMsb("m18_01_00_00", msb => msb
    ///     .PlaceTreasure(lotId: 8500, position: new(52f, -2f, 103f)));
    /// </code>
    /// </summary>
    public MsbEditor PlaceTreasure(int lotId, Vector3 position,
        string? collisionName = null, bool inChest = false, int entityId = -1)
    {
        // Idempotency: if a Treasure event for this lot already exists (from a
        // previous Patch() run on the same game file), remove it and its
        // associated o0500 object before re-adding. Without this, each game
        // launch appends a duplicate — two Treasure events for the same spot
        // both fire on pickup, doubling the phantom item drops.
        var existing = _msb.Events.Treasures.FirstOrDefault(t => t.ItemLots[0] == lotId);
        if (existing is not null)
        {
            _msb.Parts.Objects.RemoveAll(o => o.Name == existing.TreasurePartName);
            _msb.Events.Treasures.Remove(existing);
        }

        // 1. Ensure o0500 model is registered. When the map has no o0500 yet
        //    we register a bare entry; vanilla maps already have one with the
        //    correct SibPath, which we leave untouched.
        const string PickupModel = "o0500";
        if (!_msb.Models.Objects.Any(m => m.Name == PickupModel))
            _msb.Models.Objects.Add(new MSB1.Model.Object { Name = PickupModel });

        // 2. Pick a donor o0500 pickup to clone — this gives us the correct
        //    DrawGroups/DispGroups, BreakTerm, NetSyncType, and all the other
        //    ObjectBase fields the engine cares about for free. A fresh
        //    MSB1.Part.Object() also leaves InitAnimID at 0, which leaves the
        //    prop invisible (vanilla pickups use 10/20/50; 50 = glow-bubble
        //    pose). When no o0500 exists we synthesise one with InitAnimID=50.
        MSB1.Part.Object obj;
        MSB1.Part.Object? donor = _msb.Parts.Objects
            .Where(o => o.ModelName == PickupModel)
            .OrderBy(o => Vector3.DistanceSquared(o.Position, position))
            .FirstOrDefault();

        if (donor is not null)
        {
            obj = (MSB1.Part.Object)donor.DeepCopy();
            obj.InitAnimID = 50;
        }
        else
        {
            obj = new MSB1.Part.Object
            {
                ModelName  = PickupModel,
                Scale      = Vector3.One,
                InitAnimID = 50,
            };
        }

        // 3. Override placement-specific fields.
        obj.Name          = NextObjectName(PickupModel);
        obj.Position      = position;
        obj.Rotation      = Vector3.Zero;
        obj.CollisionName = collisionName ?? FindNearestCollision(position);
        obj.EntityID      = entityId;
        _msb.Parts.Objects.Add(obj);

        // 4. Create the Treasure event pointing at the new part.
        int nextEventId = _msb.Events.Treasures.Count > 0
            ? _msb.Events.Treasures.Max(t => t.EventID) + 1
            : 0;

        var treasure = new MSB1.Event.Treasure
        {
            Name             = $"takara_{lotId}",
            TreasurePartName = obj.Name,
            InChest          = inChest,
            StartDisabled    = false,
            EventID          = nextEventId,
        };
        treasure.ItemLots[0] = lotId;
        // SoulsFormats initialises unused slots to -1, which the engine
        // treats as a valid lot ID and tries to award — giving 4 phantom
        // "invalid item" pickups alongside the real one. Zero means no lot.
        for (int i = 1; i < treasure.ItemLots.Length; i++)
            treasure.ItemLots[i] = 0;
        _msb.Events.Treasures.Add(treasure);

        return this;
    }

    private string NextObjectName(string model)
    {
        int next = _msb.Parts.Objects
            .Select(o =>
            {
                string[] p = o.Name.Split('_');
                return p.Length > 1 && int.TryParse(p[1], out int n) ? n : 0;
            })
            .DefaultIfEmpty(0).Max() + 1;
        return $"{model}_{next:D4}";
    }

    private string FindNearestCollision(Vector3 pos)
    {
        // Prefer the actual nearest collision mesh by world position. This
        // is the collision the player will be standing on when they walk
        // up to the pickup — and DSR o0500 pickups are culled by
        // collision-group rather than draw-group, so binding to a far-
        // away collision makes the prop invisible up close (only visible
        // once the player stands on that distant collision).
        var nearestCol = _msb.Parts.Collisions
            .OrderBy(c => Vector3.DistanceSquared(c.Position, pos))
            .FirstOrDefault();
        if (nearestCol is not null) return nearestCol.Name;

        // Fall back to borrowing from the nearest existing o0500 — used
        // only when the map has no collision parts (essentially never in
        // real DSR maps, but keeps the function total).
        var nearestObj = _msb.Parts.Objects
            .Where(o => o.ModelName == "o0500" && !string.IsNullOrEmpty(o.CollisionName))
            .OrderBy(o => Vector3.DistanceSquared(o.Position, pos))
            .FirstOrDefault();
        return nearestObj?.CollisionName ?? string.Empty;
    }
}
