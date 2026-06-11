# DS1Mod.AutoEquip

In-process port of [cboyo's DS1Remastered-AutoEquipMod](https://github.com/cboyo/DS1Remastered-AutoEquipMod)
(Beta branch). Whenever you pick up a weapon, armor piece, ring, or ammo, it is
equipped automatically into the matching slot. Rings alternate between the two
ring slots; shields/catalysts/parry daggers go to the left hand, everything
else to the right.

## How it works

- Scans private heap memory once for the inventory signature: an 8-byte
  self-pointer followed by the slot capacity (2048). The inventory array starts
  16 bytes past the signature; the equip-slot tables sit at fixed negative
  offsets from it. The scan is throttled (`scanIntervalTicks`) and repeated
  whenever the self-pointer breaks (quit-out / reload).
- Each tick (500 ms) the 2048-slot inventory is diffed against the previous
  snapshot. A slot going empty → occupied is a new pickup; the item id and
  inventory index are written directly into the equip slot. Count, durability,
  and in-place upgrade changes are ignored.
- The first read after acquiring the signature only takes a baseline, so your
  existing inventory is never mass-equipped.

## Config

`<game>/mods/config/Auto Equip.json` (generated on first run):

| Key | Default | Meaning |
|---|---|---|
| `enabled` | true | master switch |
| `equipWeapons` | true | weapons (right hand) and shields/catalysts (left hand) |
| `equipArmor` | true | head/chest/hands/legs |
| `equipRings` | true | rings, alternating slots |
| `equipAmmo` | true | arrows and bolts |
| `logEquips` | true | log each equip to the debug console |
| `scanIntervalTicks` | 10 | ticks between heap scans while searching |
