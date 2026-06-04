# Adding a new item

A new item spans three systems, all editable on Linux with SoulsFormats. The
worked example is **"Demon's Dignity (lost)"** — a trinket the Asylum Demon drops
when he dies (see `DS1Mod.GoofyDemon`).

| Step | File | Role |
|---|---|---|
| 1. Define it | `param/GameParam/GameParam.parambnd.dcx` | the item's stats/model/icon (a PARAM row) |
| 2. Name it | `msg/<lang>/item.msgbnd.dcx` | name + description (FMG) |
| 3. Give it | `ItemLotParam` row + EMEVD / shop / treasure | how the player gets it |

Easy tier = **reuse existing visuals** (point at an existing model + icon). A
truly new-looking item needs a new FLVER model + TPF icon (DSAnimStudio/Blender,
out of scope here). Almost every "new" item reuses an existing model.

## Step 0: paramdefs (the catch)

Params are raw rows; to edit fields you need the **PARAMDEF** (the layout). The
**game does not ship paramdefs** — they're a modding resource
(`paramdef.paramdefbnd.dcx`). Get them once and apply/embed:

```csharp
var defs = new Dictionary<string, PARAMDEF>();
foreach (var f in BND3.Read(DCX.Decompress(defbndPath)).Files)
    { var d = PARAMDEF.Read(f.Bytes); defs[d.ParamType] = d; }   // keyed by ParamType
```

(A mod that edits params at runtime should **embed** the paramdefbnd as a
resource, since it won't exist on the player's machine.)

## Step 1: the item — a PARAM row

Items live in different params:

| Param | Item kind |
|---|---|
| `EquipParamGoods` | consumables, **key items / trinkets** |
| `EquipParamWeapon` | weapons |
| `EquipParamProtector` | armor |
| `EquipParamAccessory` | rings |
| `Magic` | spells |

Read the param, apply its def, **clone a donor row** (so every field is valid),
change what you need, add it back:

```csharp
PARAM goods = PARAM.Read(file.Bytes);
goods.ApplyParamdef(defs[goods.ParamType]);   // ParamType e.g. EQUIP_PARAM_GOODS_ST

PARAM.Row src = goods[384];                    // 384 = a vanilla valuable (goodsType 1 = key item)
var row = new PARAM.Row(8000, "Demon's Dignity (lost)", goods.AppliedParamdef);
for (int i = 0; i < src.Cells.Count; i++) row.Cells[i].Value = src.Cells[i].Value;  // clone
row["maxNum"].Value = (ushort)1;
goods.Rows.RemoveAll(r => r.ID == 8000);       // idempotent
goods.Rows.Add(row);
goods.Rows.Sort((a, b) => a.ID.CompareTo(b.ID));
file.Bytes = goods.Write();
```

Useful Goods fields: `goodsType` (1 = key item), `iconId`, `modelId`, `maxNum`,
`sortId`, `refId`, `basicPrice`/`sellValue`. Access cells by name:
`row["fieldName"].Value`. (DS1 paramdefs are an old format — fields are keyed by
`DisplayName`; SoulsFormats handles this.)

> **Linux filename gotcha:** BND entry names use `\` separators. On Linux,
> `Path.GetFileName` won't split them — normalize first:
> `name.Replace('\\','/')`.

## Step 2: name + description — FMG

Goods text is keyed by the **goods row id**, in `item.msgbnd.dcx`:

```csharp
void Put(string fmgName, string text) {
  foreach (var f in bnd.Files)
    if (Path.GetFileName(f.Name.Replace('\\','/')).Contains(fmgName)) {
      var fmg = FMG.Read(f.Bytes);
      fmg.Entries.RemoveAll(e => e.ID == 8000);
      fmg.Entries.Add(new FMG.Entry(8000, text));
      f.Bytes = fmg.Write();
    }
}
Put("Item_name", "Demon's Dignity (lost)");
Put("Item_description", "All that remains of a demon's self-respect.");
Put("Item_long_desc", "...the flavor text...");
```

(Weapons/armor/rings/spells use `Weapon_*`, `Armor_*`, `Accessory_*`, `Magic_*`
FMGs instead.)

## Step 3: give it to the player

An **`ItemLotParam`** row is a drop/reward bundle. Clone a known-good gift lot
and repoint it:

```csharp
PARAM.Row row = clone(itemLot[1000], 8500);    // 1000 = a vanilla goods gift
row["lotItemId01"].Value = 8000;               // our goods
row["lotItemCategory01"].Value = 1073741824;   // 0x40000000 = Goods  (from the donor)
row["lotItemNum01"].Value = (byte)1;
row["lotItemBasePoint01"].Value = 100;         // weight (100% here)
row["getItemFlagId"].Value = 50009000;         // free flag in the 50000000+ "obtained" range -> once-only
```

`lotItemCategory` is a bitfield: `0x10000000` weapon, `0x20000000` protector,
`0x30000000` accessory, `0x40000000` goods.

Then **award it**. Cleanest is an EMEVD event on a trigger — e.g. when the boss
dies (flag 16 = Asylum Demon killed):

```csharp
var ev = new EMEVD.Event(11819100, EMEVD.Event.RestBehaviorType.Default);  // run once
ev.Instructions.Add(new EMEVD.Instruction(3, 0, new List<object>{ (sbyte)0,(byte)1,(byte)0, 16 }));  // IF flag 16 ON
ev.Instructions.Add(new EMEVD.Instruction(2003, 4, new List<object>{ 8500 }));                        // Award Item Lot
evd.Events.Add(ev);
// ...register at the TOP of event 0 (see emevd-events.md)
```

The lot's `getItemFlagId` makes `Award Item Lot` **once-only** — even if the
event re-runs on reload, the game skips an already-obtained lot.

Other ways to hand it over: a shop (`ShopLineupParam` + an NPC), or place it as
treasure in the map (`MSB` + `ItemLotParam`).

## Picking IDs

- **Goods / weapon / etc. row id:** any unused id in that param (we used `8000`).
- **Item lot id:** any unused `ItemLotParam` id (we used `8500`; `8000` was taken).
- **`getItemFlagId`:** a free flag in the `50000000+` "item obtained" range
  (existing gift lots use `50000000`, `50000010`, …).
- **EMEVD event id / registration:** see [emevd-events.md](emevd-events.md).

## Verifying (since you can't playtest)

Re-read everything you wrote and assert:

- `goods.Rows.Count` went up by exactly 1; `goods[8000].Name` is your name.
- `itemLot[8500]["lotItemId01"].Value == 8000`.
- the `Item_name` FMG has your string at `8000`.
- the award event decompiles and is registered in event 0.

A bad PARAM edit usually just makes the game fail to boot — so keep the `.bak`
and it's safe to try.
