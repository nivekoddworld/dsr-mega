using DS1Mod.Core;
using DS1Mod.Core.Memory;
using DS1Mod.SDK;

namespace DS1Mod.AutoEquip;

/// <summary>
/// Auto-equips items as you pick them up — a port of cboyo's standalone
/// DS1Remastered-AutoEquipMod into the DS1Mod framework. Each tick the mod
/// diffs the 2048-slot inventory against the previous snapshot; any slot that
/// goes from empty to occupied is a new pickup, and the matching equip slot
/// (weapon hand, armor piece, ring, or ammo) is pointed at it directly in
/// game memory. Rings alternate between the two ring slots.
/// </summary>
public sealed class AutoEquipMod : ModBase
{
    public override string Name    => "Auto Equip";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando (after cboyo)";

    private InventoryAddresses _addrs;
    private InvSlot[]? _inventory;          // last snapshot; null until baselined
    private readonly InvSlot[] _current = new InvSlot[InventoryScanner.SlotCount];
    private int  _ringToggle;               // alternates left/right ring slot
    private long _tick;
    private long _nextScanTick;             // heap scans are expensive — throttle

    public override void InitializeConfig(ModConfig config)
    {
        config.AddBool("enabled", true)
            .Tooltip("Enable or disable Auto Equip");

        config.AddBool("equipWeapons", true)
            .Tooltip("Auto-equip newly picked-up weapons (right hand) and shields/catalysts (left hand)");

        config.AddBool("equipArmor", true)
            .Tooltip("Auto-equip newly picked-up armor into the matching slot (head/chest/hands/legs)");

        config.AddBool("equipRings", true)
            .Tooltip("Auto-equip newly picked-up rings, alternating between the two ring slots");

        config.AddBool("equipAmmo", true)
            .Tooltip("Auto-equip newly picked-up arrows and bolts");

        config.AddBool("logEquips", true)
            .Tooltip("Log each auto-equip action to the debug console");

        config.AddInt("scanIntervalTicks", 10)
            .Tooltip("How many ticks (500 ms each) to wait between heap scans while searching for the inventory");
    }

    public override void OnLoad(IModContext ctx)
    {
        string gameDir = Directory.GetParent(ctx.ModsDir)?.FullName ?? ctx.ModsDir;
        LoadConfigAsync(gameDir).GetAwaiter().GetResult();
        Console.WriteLine("[AutoEquip] Loaded. Waiting for the inventory signature...");
    }

    public override void OnUnload()
    {
        _addrs = default;
        _inventory = null;
    }

    public override void OnTick()
    {
        _tick++;
        if (Config?.GetBool("enabled", true) != true) return;

        // (Re)acquire the inventory signature. It dies on quit-out, so
        // re-validate the self-pointer every tick and rescan when it breaks.
        if (!_addrs.StillValid)
        {
            _addrs = default;
            _inventory = null;
            if (_tick < _nextScanTick) return;
            _nextScanTick = _tick + Math.Max(1, Config.GetInt("scanIntervalTicks", 10));

            nint sig = InventoryScanner.FindSignature(Console.WriteLine);
            if (sig == 0) return;

            _addrs = new InventoryAddresses(sig);
            Console.WriteLine($"[AutoEquip] Inventory signature found at 0x{(ulong)sig:X}.");
        }

        // In-game sanity check (0xFFFFFFFF = main menu). EventPump already
        // gates ticks to in-game, but the inventory has its own flag — trust it.
        if (GameMemory.Read<uint>(_addrs.InGame) == 0xFFFFFFFF) { _inventory = null; return; }

        if (!InventoryScanner.ReadInventory(_addrs.Inventory, _current)) return;

        if (_inventory == null)
        {
            // First successful read after (re)acquiring: baseline only, so we
            // don't "auto-equip" the whole existing inventory.
            _inventory = (InvSlot[])_current.Clone();
            Console.WriteLine("[AutoEquip] Initial inventory snapshot taken.");
            return;
        }

        for (int i = 0; i < _current.Length; i++)
        {
            if (!_inventory[i].Equals(_current[i]))
                HandleChange(_inventory[i], _current[i], i);
        }
        _current.CopyTo(_inventory, 0);
    }

    /// <summary>A slot that goes empty→occupied is a new pickup worth equipping.
    /// Count/durability changes and weapon upgrades in place are ignored.</summary>
    private void HandleChange(in InvSlot old, in InvSlot cur, int index)
    {
        if (old.Valid != 0 || cur.Valid == 0) return;

        switch ((ItemType)cur.Type)
        {
            case ItemType.Weapon: EquipWeapon(cur, index); break;
            case ItemType.Armor:  EquipArmor(cur, index);  break;
            case ItemType.Ring:   EquipRing(cur, index);   break;
        }
    }

    private void EquipWeapon(in InvSlot item, int index)
    {
        var type = ItemClassifier.WeaponTypeFromId(item.Id);
        bool isAmmo = type is WeaponType.Arrow or WeaponType.Bolt;
        if (isAmmo  && Config?.GetBool("equipAmmo", true)    != true) return;
        if (!isAmmo && Config?.GetBool("equipWeapons", true) != true) return;

        int offset = type switch
        {
            WeaponType.LeftHand  => 0,
            WeaponType.RightHand => 4,
            WeaponType.Arrow     => 16,
            WeaponType.Bolt      => 20,
            _                    => 4,
        };
        Equip(_addrs.WeaponId + offset, _addrs.WeaponSlot + offset, item.Id, index, type.ToString());
    }

    private void EquipArmor(in InvSlot item, int index)
    {
        if (Config?.GetBool("equipArmor", true) != true) return;

        var type = ItemClassifier.ArmorTypeFromId(item.Id);
        if (type == ArmorType.Unknown)
        {
            Log($"[AutoEquip] Unknown armor type for id {item.Id}, skipped.");
            return;
        }
        int offset = 4 * (int)type;
        Equip(_addrs.ArmorId + offset, _addrs.ArmorSlot + offset, item.Id, index, type.ToString());
    }

    private void EquipRing(in InvSlot item, int index)
    {
        if (Config?.GetBool("equipRings", true) != true) return;

        int offset = _ringToggle == 1 ? 4 : 0;
        _ringToggle ^= 1;
        Equip(_addrs.RingId + offset, _addrs.RingSlot + offset, item.Id, index,
              offset == 0 ? "ring (left)" : "ring (right)");
    }

    private void Equip(nint idAddr, nint slotAddr, uint itemId, int invIndex, string what)
    {
        GameMemory.Write(idAddr, itemId);
        GameMemory.Write(slotAddr, (uint)invIndex);
        Log($"[AutoEquip] Equipped {what}: id {itemId} (inventory slot {invIndex}).");
    }

    private void Log(string message)
    {
        if (Config?.GetBool("logEquips", true) == true)
            Console.WriteLine(message);
    }
}
