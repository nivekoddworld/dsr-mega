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
        // 1. Ensure o0500 model is registered
        const string PickupModel = "o0500";
        if (!_msb.Models.Objects.Any(m => m.Name == PickupModel))
        {
            var model = new MSB1.Model.Object { Name = PickupModel };
            _msb.Models.Objects.Add(model);
        }

        // 2. Resolve collision — nearest existing o0500 object if not specified
        string col = collisionName ?? FindNearestCollision(position);

        // 3. Auto-generate unique object name: o0500_XXXX
        int nextSuffix = _msb.Parts.Objects
            .Select(o => {
                var p = o.Name.Split('_');
                return p.Length > 1 && int.TryParse(p[1], out int n) ? n : 0;
            })
            .DefaultIfEmpty(0).Max() + 1;

        string objName = $"{PickupModel}_{nextSuffix:D4}";

        // 4. Create the object part
        var obj = new MSB1.Part.Object
        {
            Name          = objName,
            ModelName     = PickupModel,
            Position      = position,
            Rotation      = Vector3.Zero,
            Scale         = Vector3.One,
            CollisionName = col,
            EntityID      = entityId,
        };
        // DrawGroups / DispGroups default to all-visible (0xFFFFFFFF)
        for (int i = 0; i < obj.DrawGroups.Length; i++)  obj.DrawGroups[i]  = 0xFFFFFFFF;
        for (int i = 0; i < obj.DispGroups.Length; i++) obj.DispGroups[i] = 0xFFFFFFFF;
        _msb.Parts.Objects.Add(obj);

        // 5. Create the Treasure event
        int nextEventId = _msb.Events.Treasures.Count > 0
            ? _msb.Events.Treasures.Max(t => t.EventID) + 1
            : 0;

        var treasure = new MSB1.Event.Treasure
        {
            Name             = "takara",
            TreasurePartName = objName,
            InChest          = inChest,
            StartDisabled    = false,
            EventID          = nextEventId,
        };
        treasure.ItemLots[0] = lotId;
        _msb.Events.Treasures.Add(treasure);

        return this;
    }

    private string FindNearestCollision(Vector3 pos)
    {
        // Walk existing o0500 objects and return the collision of the nearest one
        var nearest = _msb.Parts.Objects
            .Where(o => o.ModelName == "o0500" && !string.IsNullOrEmpty(o.CollisionName))
            .OrderBy(o => Vector3.DistanceSquared(o.Position, pos))
            .FirstOrDefault();

        return nearest?.CollisionName ?? string.Empty;
    }
}
