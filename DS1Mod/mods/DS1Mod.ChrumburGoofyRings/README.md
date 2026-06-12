# Chrumbur Goofy Rings

A collection of new rings for Dark Souls Remastered, built on the DS1Mod
framework. New rings get added here over time.

## Ring #1 — Hunters Ring

Brings the **Bloodborne rally health mechanic** to Lordran, the real one:
while the ring is equipped, damage you take becomes a *rally pool*, and you
get it back **by landing hits on enemies** — each hit restores HP proportional
to the damage you dealt (1:1 by default), capped by the pool. Hesitate until
the rally window closes and the pool drains away for good. Healing from other
sources (estus, miracles) consumes the rallyable headroom, so you can't
double-dip.

**Where to find it:** ground pickup on the ledge near the Undead Asylum start
(a few meters from where you wake up). One-time pickup; the glow disappears
once collected.

## How it works

- **Patch time** — clones a vanilla ring row in `EquipParamAccessory`
  (donor: Havel's Ring, passive effect stripped), writes the Accessory FMG
  strings, defines an `ItemLotParam` lot with a once-only flag, places the
  treasure in `m18_01_00_00`, and adds an EMEVD event that hides the prop
  after pickup. IDs come from the host allocator (`EquipParamAccessory`
  space, base 900).
- **Runtime** — every 500 ms tick: equipped-ring detection reads the two
  ring-slot ids at a fixed offset from the inventory heap signature (the same
  self-pointer + capacity-2048 signature AutoEquip uses); HP is read and
  written through the WorldChrMan AOB pointer chain. HP drops feed the rally
  pool and open the window.
- **Hits** — the framework's `IGameHooks.EnemyDamaged` hook (added for this
  mod) fires for every loaded enemy that loses HP. Enemies are discovered by
  resolving the `NS_FRPG::ChrIns` vftable via the runtime RTTI walk and heap
  scanning for live instances (vptr match); their HP is diffed each poll.
  While the rally window is open, each hit near the player converts pool HP
  back into health at `damage x rallyRegainMultiplier`.

## Config

`<game>/mods/config/Chrumbur Goofy Rings.json` (generated on first run):

| Key | Default | Meaning |
|---|---|---|
| `enabled` | true | master switch |
| `requireRingEquipped` | true | rally only with the ring on (off = always, for testing) |
| `rallyWindowSeconds` | 6.0 | how long lost HP stays rallyable |
| `rallyPercent` | 0.85 | fraction of damage taken that becomes rally HP |
| `rallyRegainMultiplier` | 1.0 | HP regained per hit = damage dealt x this (1.0 = Bloodborne) |
| `maxRallyRangeMeters` | 20 | hits only count within this distance of the player (0 = no limit) |
| `damageRefreshesWindow` | true | taking new damage restarts the window |
| `logRally` | false | log rally events to the debug console |

## Notes / limitations

- The hook polls at the EventPump's 500 ms cadence, so several rapid hits on
  one enemy coalesce into a single rally event with the summed damage — the
  total HP regained is identical, it just lands in one chunk.
- Hit attribution is by proximity (`maxRallyRangeMeters`), since the poll
  sees HP loss, not the attacker. Enemy infighting right next to you counts
  as rally; in practice that's rare and reads as a bonus, not a bug.
