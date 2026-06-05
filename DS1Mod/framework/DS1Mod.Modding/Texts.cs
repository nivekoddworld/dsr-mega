using SoulsFormats;

namespace DS1Mod.Modding;

/// <summary>
/// FMG text helpers. Text lives in FMG files inside the msgbnd archives; an
/// entry id usually matches the row id of the thing it names (a goods id, an
/// event message id, …).
/// </summary>
public static class Texts
{
    /// <summary>
    /// Set entry <paramref name="id"/> = <paramref name="text"/> in every FMG inside
    /// <paramref name="msgbnd"/> whose name contains <paramref name="fmgNameContains"/>.
    /// Idempotent (removes any existing entry with that id first). DSR ships two
    /// copies of each FMG, so matching by name covers both.
    /// </summary>
    public static int Set(IBinder msgbnd, string fmgNameContains, int id, string text)
    {
        int n = 0;
        foreach (BinderFile f in msgbnd.Files)
        {
            if (!Binders.Leaf(f.Name).Contains(fmgNameContains)) continue;
            FMG fmg = FMG.Read(f.Bytes);
            fmg.Entries.RemoveAll(e => e.ID == id);
            fmg.Entries.Add(new FMG.Entry(id, text));
            f.Bytes = fmg.Write();
            n++;
        }
        return n;
    }

    /// <summary>Read entry <paramref name="id"/> from the first matching FMG, or null.</summary>
    public static string? Get(IBinder msgbnd, string fmgNameContains, int id)
    {
        foreach (BinderFile f in msgbnd.Files)
        {
            if (!Binders.Leaf(f.Name).Contains(fmgNameContains)) continue;
            FMG fmg = FMG.Read(f.Bytes);
            FMG.Entry? e = fmg.Entries.FirstOrDefault(x => x.ID == id);
            if (e != null) return e.Text;
        }
        return null;
    }

    // Common FMG name fragments (use with Set / Get).
    public const string EventText       = "Event_text";        // menu.msgbnd — Display Message / dialogs
    public const string GoodsName       = "Item_name";         // item.msgbnd — goods/consumables/key items
    public const string GoodsDescription = "Item_description";
    public const string GoodsLongDesc   = "Item_long_desc";
    public const string WeaponName      = "Weapon_name";
    public const string ArmorName       = "Armor_name";
    public const string RingName        = "Accessory_name";
    public const string SpellName       = "Magic_name";
}
