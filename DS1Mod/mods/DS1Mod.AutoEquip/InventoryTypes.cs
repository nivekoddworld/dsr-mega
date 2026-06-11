using System.Runtime.InteropServices;

namespace DS1Mod.AutoEquip;

/// <summary>High nibble of the item id encodes its category.</summary>
public enum ItemType : uint
{
    Weapon     = 0x00000000,
    Armor      = 0x10000000,
    Ring       = 0x20000000,
    Consumable = 0x40000000,
    None       = 0xFFFFFFFF,
}

public enum ArmorType : uint { Head = 0, Chest = 1, Hands = 2, Legs = 3, Unknown }

public enum WeaponType : uint { RightHand, LeftHand, Arrow, Bolt }

/// <summary>
/// One slot of the in-memory inventory array (28 bytes, 2048 slots).
/// Layout matches DSR's GameDataMan inventory; verified against the
/// AutoEquip reference implementation.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InvSlot : IEquatable<InvSlot>
{
    public uint Type;        // ItemType category bits
    public uint Id;          // item param id
    public uint Count;
    public uint SortId;      // display-order counter
    public uint Valid;       // nonzero while the slot holds an item
    public uint Durability;
    public uint Hits;

    public readonly bool Equals(InvSlot o) =>
        Type == o.Type && Id == o.Id && Count == o.Count && SortId == o.SortId &&
        Valid == o.Valid && Durability == o.Durability && Hits == o.Hits;

    public override readonly bool Equals(object? o) => o is InvSlot s && Equals(s);
    public override readonly int GetHashCode() => HashCode.Combine(Type, Id, Valid, Count);
}

public static class ItemClassifier
{
    public static ArmorType ArmorTypeFromId(uint armorId)
    {
        uint type = armorId % 10000u / 1000u;
        return type < 4 ? (ArmorType)type : ArmorType.Unknown;
    }

    public static WeaponType WeaponTypeFromId(uint weaponId)
    {
        static bool InRange(uint v, uint a, uint b) => v >= a && v < b;

        if (InRange(weaponId,  101000,  102000)) return WeaponType.LeftHand; // parry dagger
        if (InRange(weaponId, 2000000, 2100000)) return WeaponType.Arrow;
        if (InRange(weaponId, 2100000, 2200000)) return WeaponType.Bolt;
        if (InRange(weaponId, 1300000, 1600000)) return WeaponType.LeftHand; // catalysts + shields
        if (InRange(weaponId, 9000000, 9010000)) return WeaponType.LeftHand; // more shields
        if (InRange(weaponId, 9014000, 9015000)) return WeaponType.LeftHand; // Cleansing Greatshield
        if (InRange(weaponId, 9017000, 9019000)) return WeaponType.LeftHand; // Manus / Oolacile catalysts
        return WeaponType.RightHand;
    }
}
