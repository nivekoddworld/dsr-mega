---
name: ds1-memory
description: Advanced in-process memory toolkit for DS1Mod — RTTI vftable resolution, heap scanning, per-hit enemy damage events, player HP write access, and native HUD bar control. Read this ONLY for mods that need live game-object access beyond Reader/Writer.
---

# DS1Mod Memory Toolkit — RTTI, Heap Scanning, HUD Control

`DS1Mod.Core.Memory` namespace (+ `PlayerBody` in `DS1Mod.Core`). Mods run **inside** the DSR process: reads are direct dereferences, and an unhandled access violation crashes the game. The rules below exist because each one fixed a real crash or false-positive.

## Safety Rules (non-negotiable)

1. **Never dereference heap memory found via VirtualQuery.** A page observed committed can be decommitted before you read it (TOCTOU) → corrupted-state exception → DSR dies. All scanners copy regions via `ReadProcessMemory` on our own handle — it returns `false` on unmapped pages instead of faulting. Use `GameMemory.Read<T>` (page-validated) for single reads.
2. **Heap addresses are session-local.** Objects move every launch and every area reload. Re-find by RTTI vftable scan; validate before each write (does the value still read as plausible?) and drop stale addresses.
3. **Scan buffers are themselves scannable heap.** A naive scanner finds copies of the values it's looking for inside its own (and prior GC'd) copy buffers — phantom hits that grow +1 per scan. `HeapScanner` already excludes its buffer and zeroes it after each walk; do the same in any new scanner.
4. **Watch out for NaN in filters.** NaN fails every comparison, so it survives "must decay / must be in range" candidate filters. Reject non-finite floats at the read layer.
5. **Threading:** heavy scans on a `ThreadPriority.Lowest` background thread, ~10 s interval, single pass for all values at once. Fast reaction (HP diffs) on a separate ~50 ms thread. Pinning a value the game rewrites per frame needs ~8 ms writes or it sawtooths.

## Rtti — Class Name ⇄ Vftable

DSR ships full MSVC RTTI. Mangled names look like `".?AVChrIns@NS_FRPG@@"`.

```csharp
nint vft = Rtti.FindVftable(".?AVEnemyIns@NS_FRPG@@");        // primary (offset-0) vftable
List<(nint Vftable, int Offset)> all = Rtti.FindVftables(cls); // ALL vftables + sub-object offsets
string? cls = Rtti.GetClassName(vptr);                         // reverse lookup (diagnostics)
```

Multiple inheritance ⇒ multiple vftables per class. A heap hit on a secondary vftable is NOT the object base — subtract its `Offset`. Always scan with `FindVftables` when enumerating instances of UI/engine classes.

Known classes: `PlayerIns` (player), `EnemyIns` (live enemies — use this; plain `ChrIns` hits are factory templates that fail HP sanity), `NpcIns`, `FrpgMenuDlgObjSmoothGauge` (HUD HP gauges), `FrpgMenuDlgFEEnemyGauge` (enemy HP bars), `EzMenuDialog`.

## HeapScanner — Find Live Instances

```csharp
List<nint> hits = HeapScanner.FindQwordValue((ulong)vft, max);     // vptr scan = instances
List<nint> hits = HeapScanner.FindQwordValues(vftArray, max);      // single pass, many values
```

RPM-based, 1 MB chunks, committed MEM_PRIVATE RW only, self-buffer excluded. To get object bases from multi-vftable hits: read the qword at each hit, match against your `(vft, offset)` list, `base = hit - offset`.

## EnemyDamaged Hook (consumer side: see ds1mod-sdk)

`ctx.Hooks.EnemyDamaged += hit => ...` — fires per HP drop on any tracked enemy, from a 50 ms diff thread. Architecture (if extending): vftable resolution once → Lowest-priority scan thread repopulates the tracked set every 10 s → diff thread re-validates each object's vptr before reading (object freed = vptr changed = drop), reads HP, dispatches events outside the lock with per-handler try/catch.

## PlayerBody — Player HP

```csharp
(int hp, int maxHp) = PlayerBody.ReadHp();   // (0,0) when not loaded
PlayerBody.WriteHp(value);
nint chr = PlayerBody.PlayerChr;             // ChrIns base, diagnostics
```

Includes `DsrVersion.ChrData1Boost2` (+0x10 on 1.03+). Raw DSR-Gadget offsets (0x3D8) without the boost read garbage. Neighbors of HP: +4 maxHP, +0x10 stamina (float-ish int), +0x14 max stamina.

## LagBar — Native HP Bar "Ghost" Segment

The HUD's lighter recently-lost-HP segment is a 0..1 ratio float at **+0x60 in `FrpgMenuDlgObjSmoothGauge`**. Pinning it shows arbitrary "recoverable HP" in the real game UI (Bloodborne-rally style) without touching real HP.

```csharp
// 500 ms tick, until adopted:
if (LagBar.Address == 0) LagBar.TryAutoLocate(Console.WriteLine);

// fast loop (~8 ms) while you want the segment held:
LagBar.Write(hp + poolHp, maxHp);   // validates + drops stale addresses itself
```

`TryAutoLocate` scans all gauge vftables, reads each instance's ratio, adopts every gauge currently matching the player's hp/maxHp (the game keeps 2 — HUD + menu copy; pin both). It defers at full HP (every idle gauge reads 1.0 — unidentifiable) and self-heals after reloads. `LagBarProbe.Start(preHp, postHp, maxHp, onFound, log)` is the calibration-by-differential-scan fallback; normally unneeded.

## Inventory Heap Signature (equip detection)

The inventory block is found by signature: 8-byte self-pointer (`*(ulong*)X == X`) followed by capacity dword 2048, padding 0. Inventory array at sig+16; equip slots at fixed negative offsets (RingId −0x318/−0x314, WeaponId −0x34C, ArmorId −0x32C, InGame −0x660). Reference implementations: `DS1Mod.AutoEquip/InventoryScanner.cs`, `DS1Mod.ChrumburGoofyRings/RingEquipDetector.cs`. Scan with RPM (rule 1); throttle rescans (~5 s) until found.

## External Diagnosis

`tools/lagbar_chain` — out-of-process pointer/RTTI walker (run while the game is alive): give it a heap address, it finds who points there and names owning classes via RTTI. Use it to identify an unknown value's owning object, then switch to in-process RTTI scan for the production locator.
