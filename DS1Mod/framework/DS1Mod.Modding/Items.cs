using System.Numerics;
using SoulsFormats;

namespace DS1Mod.Modding;

// ── ItemDef ───────────────────────────────────────────────────────────────────

/// <summary>
/// Everything needed to define a new goods item in one place.
/// Pass to <see cref="GamePatch.DefineGoods"/>.
/// </summary>
public sealed class ItemDef
{
    /// <summary>EquipParamGoods row ID. Pick something unused (e.g. 8000+).</summary>
    public int Id { get; set; }

    /// <summary>Existing goods row to clone as the base (copies all fields).</summary>
    public int DonorId { get; set; } = 384;

    public string Name        { get; set; } = "Unnamed Item";
    public string Description { get; set; } = "";
    public string LongDesc    { get; set; } = "";

    /// <summary>Stack size. Default 1 (key-item style).</summary>
    public ushort MaxCount { get; set; } = 1;

    /// <summary>
    /// SpEffectParam row ID to trigger when the item is used.
    /// When set, <see cref="GamePatch.DefineGoods"/> automatically sets
    /// <c>goodsType = 0</c> (consumable) and <c>refId_default</c> to this ID.
    /// Define the SpEffect row first via <see cref="GamePatch.DefineSpEffect"/>.
    /// Use -1 (default) for key items with no on-use effect.
    /// </summary>
    public int SpEffectId { get; set; } = -1;

    /// <summary>
    /// Called after cloning donor row — use to set additional PARAM fields.
    /// </summary>
    public Action<PARAM.Row>? Configure { get; set; }
}

// ── LotDef ────────────────────────────────────────────────────────────────────

/// <summary>
/// A single-item ItemLotParam row definition.
/// Pass to <see cref="GamePatch.DefineLot"/>.
/// </summary>
public sealed class LotDef
{
    /// <summary>ItemLotParam row ID. Must be unique.</summary>
    public int LotId { get; set; }

    /// <summary>EquipParamGoods / weapon / etc. ID to drop.</summary>
    public int ItemId { get; set; }

    /// <summary><see cref="LotCategory"/> bitfield.</summary>
    public int Category { get; set; } = LotCategory.Goods;

    /// <summary>How many to give. Default 1.</summary>
    public byte Count { get; set; } = 1;

    /// <summary>
    /// Flag ID that marks this lot as "already collected" — set to make the
    /// drop once-only. Use -1 for infinite.
    /// </summary>
    public int OnceOnlyFlag { get; set; } = -1;
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
