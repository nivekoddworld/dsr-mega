using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// Blueprint for a new ring (EquipParamAccessory row + FMG text).
/// Used with <see cref="EquipPatchExtensions.DefineRing"/>.
/// </summary>
public sealed class RingDef
{
    /// <summary>EquipParamAccessory row id (allocate via "EquipParamAccessory" id space).</summary>
    public required int Id { get; init; }

    /// <summary>Row to clone. Default 100 = Havel's Ring (exists in every install).</summary>
    public int DonorId { get; init; } = 100;

    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string LongDesc { get; init; } = "";

    /// <summary>
    /// Passive SpEffectParam id applied while worn, or -1 for none (mods that
    /// implement their effect in C# at runtime should leave this -1).
    /// </summary>
    public int SpEffectId { get; init; } = -1;

    public float Weight { get; init; } = 0f;
    public int BasicPrice { get; init; } = 5000;

    /// <summary>Menu sort key. Default sorts to the bottom of the ring list.</summary>
    public int SortId { get; init; } = 990000;

    /// <summary>Last-chance hook to set any extra row fields.</summary>
    public Action<PARAM.Row>? Configure { get; init; }
}

/// <summary>
/// Blueprint for a one-time ground pickup: item lot + world treasure +
/// hide-after-pickup event, in one call. Works for any item category.
/// Used with <see cref="EquipPatchExtensions.PlaceWorldPickup"/>.
/// </summary>
public sealed class WorldPickupDef
{
    public required int LotId { get; init; }
    public required int ItemId { get; init; }

    /// <summary>One of <see cref="LotCategory"/>.</summary>
    public required int Category { get; init; }

    public int Count { get; init; } = 1;
    public int Rarity { get; init; } = 3;

    /// <summary>
    /// Event flag set when picked up (allocate via ItemObtainedFlags).
    /// Drives both once-only semantics and the hide event.
    /// </summary>
    public required int ObtainedFlag { get; init; }

    /// <summary>Map the treasure is placed in, e.g. <see cref="IdSpaces.Asylum"/>.</summary>
    public required string Map { get; init; }

    public required System.Numerics.Vector3 Position { get; init; }

    /// <summary>MSB entity id for the treasure object (allocate via MsbEntities).</summary>
    public required int EntityId { get; init; }

    /// <summary>EMEVD event id for hide-after-pickup (allocate via EmevdEvents).</summary>
    public required int HideEventId { get; init; }
}

/// <summary>
/// Equipment patch helpers — the standard recipes for adding wearable items
/// and world pickups, so each mod doesn't re-derive the param/FMG/MSB/EMEVD
/// plumbing. Style-matched to <see cref="GamePatch.DefineGoods"/>.
/// </summary>
public static class EquipPatchExtensions
{
    /// <summary>
    /// Add a new ring: EquipParamAccessory row (cloned from a donor, passive
    /// effect optional) plus name / description / long description FMG text.
    /// Re-running updates the existing row in place, so parameter changes
    /// take effect on redeploy without a vanilla restore.
    /// </summary>
    public static void DefineRing(this GamePatch g, byte[] paramdefBnd, RingDef def)
    {
        g.EditParams(paramdefBnd, repo =>
        {
            repo.Edit("EquipParamAccessory", p =>
            {
                static void ApplyDef(PARAM.Row row, RingDef def)
                {
                    row["refId"].Value      = def.SpEffectId;   // -1 = no passive effect
                    row["behaviorId"].Value = 0;
                    row["weight"].Value     = def.Weight;
                    row["basicPrice"].Value = def.BasicPrice;
                    row["sortId"].Value     = def.SortId;
                    def.Configure?.Invoke(row);
                }

                if (p[def.Id] != null)
                {
                    ApplyDef(p[def.Id]!, def);
                    return;
                }
                ParamRepository.AddClone(p, def.DonorId, def.Id, def.Name, row => ApplyDef(row, def));
            });
        });

        g.EditBnd3Glob("msg", "item.msgbnd.dcx", bnd =>
        {
            Texts.Set(bnd, Texts.RingName, def.Id, def.Name);
            Texts.Set(bnd, "Accessory_description", def.Id, def.Description);
            Texts.Set(bnd, "Accessory_long_desc", def.Id, def.LongDesc);
        });
    }

    /// <summary>
    /// Place a one-time ground pickup: ItemLotParam row, MSB treasure object
    /// at a world position, and an EMEVD event that hides the treasure once
    /// the obtained flag is set (so it doesn't reappear on reload).
    /// </summary>
    public static void PlaceWorldPickup(this GamePatch g, byte[] paramdefBnd, WorldPickupDef def)
    {
        g.DefineLot(paramdefBnd, new LotDef
        {
            LotId        = def.LotId,
            ItemId       = def.ItemId,
            Category     = def.Category,
            Count        = (byte)def.Count,
            Rarity       = (byte)def.Rarity,
            OnceOnlyFlag = def.ObtainedFlag,
        });

        g.EditMsb(def.Map, msb => msb
            .PlaceTreasure(lotId: def.LotId, position: def.Position, entityId: def.EntityId));

        g.EditEmevd(def.Map, emevd =>
        {
            emevd.DefineEvent(def.HideEventId, EMEVD.Event.RestBehaviorType.Default, ev => ev
                .WhenFlag(def.ObtainedFlag, FlagState.On)
                .SetObjectEnabled(def.EntityId, EnabledState.Disabled)
                .End());
        });
    }
}
