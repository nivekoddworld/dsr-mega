# Plan: Multi-mod conflict resolution

How to handle multiple mods patching the same game files without corrupting each
other's changes or requiring central coordination.

**Status: unstarted**  
**Depends on:** DS1Mod framework additions (can be implemented incrementally)

---

## The problem

All mods in `mods/` run through `ModLifecycleManager` which calls `Patch()` on
each `IGamePatcher` in sequence. Every patcher opens the same game files, edits
them, and writes them back. The current model is last-write-wins: if two mods
touch the same file, the second one overwrites the first.

### Conflict surface (by severity)

| File type | Conflict mode | Severity |
|---|---|---|
| PARAM rows | Same row ID → second write clobbers first | Medium — different IDs auto-compose |
| FMG entries | Same entry ID → same as PARAM | Medium — different IDs auto-compose |
| EMEVD events | Same event ID → clobber; `Initialize Event` in Event 0 | Low — different IDs auto-compose; Event 0 prepend is already safe |
| BND3 file entries | Same filename inside the BND (e.g. Lua script) | High — only one version can win |
| MSB | Whole-file replacement | High — ordering matters; entities must survive other mods' edits |

The good news: PARAM, FMG, and EMEVD conflicts only happen when two mods use
the **same ID**. Mods that pick different IDs already compose correctly today
with zero framework changes.

---

## Goal

1. **Detect** conflicts before they corrupt game files.
2. **Compose** non-conflicting edits automatically (PARAM, FMG, EMEVD).
3. **Warn** loudly when a true conflict exists (BND3 entry, MSB).
4. **No hardcoding** of mod-specific data in the framework API — it must work
   for any number of mod authors without central registration.

---

## Solution overview

Three complementary mechanisms, each solving a different layer:

### 1. EditRecord tracking (detect conflicts at patch time)

`GamePatch` (or a wrapper around it) records every write made by each patcher:

```csharp
public class EditRecord
{
    public string ModName   { get; init; }
    public string FilePath  { get; init; }
    public string Selector  { get; init; }  // e.g. "PARAM:EquipParamGoods:8000"
}
```

Before applying a write, check whether another mod already wrote the same
selector. If so, log a conflict warning with both mod names and the selector. The
second write still proceeds (don't break the game), but the operator knows.

This handles PARAM, FMG, and EMEVD conflicts automatically — no mod author needs
to do anything extra.

**Implementation sketch:**

```csharp
// In ModLifecycleManager, before each Patch() call:
var recorder = new EditRecorder(modName);
ctx.SetRecorder(recorder);

// In GamePatch.EditParam / EditFmg / EditEmevd:
_recorder?.Record(filePath, $"PARAM:{paramName}:{rowId}");
if (_globalRecords.TryGetValue(selector, out var prev))
    _logger.Warn($"Conflict: {modName} and {prev} both write {selector}");
_globalRecords[selector] = modName;
```

### 2. Per-mod manifest (intentional ID reservation)

Each mod optionally ships `{ModName}.manifest.json` next to its DLL. The
framework reads all manifests before patching and warns on any overlap:

```json
{
  "name": "DS1Mod.GoofyDemon",
  "version": "1.3",
  "claims": {
    "EquipParamGoods": [[8000, 8099]],
    "ItemLotParam":    [[8500, 8599]],
    "EventText":       [[6900690, 6900720]],
    "EmevdEvents_m18_01": [[11819000, 11819200]]
  }
}
```

The manifest is **advisory** — the framework reports overlap but does not block
loading. It exists to catch conflicts early (before the game boots) and to give
mod authors a human-readable declaration of what IDs they own.

No framework code ships with any hardcoded mod names or ranges. The data lives
entirely in each mod's own file.

**Manifest loader:**

```csharp
// ModLifecycleManager startup:
var manifests = LoadAllManifests(modsDir);   // reads *.manifest.json
var claimed = new Dictionary<string, List<(string mod, int lo, int hi)>>();
foreach (var m in manifests)
    foreach (var (space, ranges) in m.Claims)
        foreach (var (lo, hi) in ranges)
        {
            var overlapping = claimed.GetValueOrDefault(space, [])
                .Where(c => c.lo <= hi && lo <= c.hi);
            foreach (var o in overlapping)
                _logger.Warn($"Manifest overlap: {m.Name} and {o.mod} both claim {space} [{lo}–{hi}]");
            claimed[space].Add((m.Name, lo, hi));
        }
```

### 3. Hash-derived ID helpers (zero-coordination option)

For mod authors who don't want to manually pick IDs, provide opt-in helpers that
derive a stable, unique range from the assembly name:

```csharp
public static class ModIdSpace
{
    // Returns a stable base offset in [10000, 9999999] derived from assemblyName.
    // Collision probability for N mods ≈ N²/2^17 — negligible for any realistic N.
    public static int BaseFor(string assemblyName, int blockSize = 100)
    {
        uint h = (uint)assemblyName.GetHashCode(StringComparison.OrdinalIgnoreCase);
        return (int)((h % 90_000u) * (uint)blockSize) + 10_000;
    }
}

// Mod author usage:
int goodsBase = ModIdSpace.BaseFor("DS1Mod.GoofyDemon"); // always stable
int myGoodsId = goodsBase + 0;   // first goods row this mod owns
int myLotId   = goodsBase + 50;  // first item lot row this mod owns
```

This is entirely opt-in. The helper lives in `DS1Mod.Modding`, not the core API.
Mods that prefer explicit IDs (for readability in debug output) ignore it.

---

## BND3 / MSB (the hard cases)

### BND3 (Lua scripts, luabnd.dcx)

A BND3 archive stores files by name. If two mods both add a file with the same
name inside the same BND, the second write wins. This is only a problem for Lua
scripts sharing an NPC model ID.

**Solution:** EditRecord detects same-filename writes inside the same BND and
warns. True resolution requires either:
- Mods use different NPC model IDs (always possible for truly independent enemies)
- One mod explicitly depends on and extends another's script (social convention)
- Future: a `MergeLua` helper that concatenates goal tables (not yet needed)

### MSB (map geometry, enemy placement)

`GamePatch.EditMsb` already loads the existing (possibly already patched) MSB
from disk and applies edits on top. As long as patchers use `EditMsb` rather than
wholesale file replacement, sequential edits compose correctly. The only
requirement is that every patcher reads from the current state, not a cached copy.

`PatchOrderAttribute` (see [randomizer-as-mod.md](randomizer-as-mod.md#3-patch-ordering))
ensures a deterministic order when ordering matters.

---

## ID range conventions (community, not framework)

Rather than baking ranges into the API, maintain a **community registry** as a
plain text file in the repo:

**`docs/id-registry.md`** — a table that mod authors can add to via PR:

| Mod | Space | Range |
|---|---|---|
| DS1Mod.GoofyDemon | EquipParamGoods | 8000–8099 |
| DS1Mod.GoofyDemon | ItemLotParam | 8500–8599 |
| DS1Mod.GoofyDemon | EventText FMG | 6900690–6900720 |
| DS1Mod.GoofyDemon | EMEVD events m18_01 | 11819000–11819200 |
| *(your mod here)* | | |

This file is documentation, not code. The framework never reads it. Mod authors
consult it to pick non-overlapping IDs; the manifest + EditRecord system catches
any mistakes at runtime.

---

## Implementation phases

### Phase 1 — EditRecord (high value, low risk)

Add `EditRecorder` to `GamePatch`. Log conflicts as warnings (not errors).
No change to any mod API — purely additive instrumentation.

### Phase 2 — Manifest loader

Read `*.manifest.json` in `ModLifecycleManager` startup, cross-check claimed
ranges, warn on overlap. Ship `DS1Mod.GoofyDemon.manifest.json` as the first
example.

### Phase 3 — `ModIdSpace` helper (optional)

Add `ModIdSpace.BaseFor()` to `DS1Mod.Modding`. Document it as an opt-in
alternative to manual ID picking.

### Phase 4 — Community ID registry

Add `docs/id-registry.md` with GoofyDemon's existing claims as the seed entry.
Link from `docs/writing-a-patcher-mod.md`.

---

## What this does NOT do

- **Does not enforce** ID exclusivity — mods can still collide if authors ignore
  warnings. Enforcement would require a central authority, which contradicts the
  open-ecosystem goal.
- **Does not solve** deep Lua script merging — two mods adding different goals to
  the same NPC model's luabnd require social coordination or explicit dependency
  declarations (future work).
- **Does not add** any mod-specific constants to the framework API — the framework
  remains generic; all mod-specific data lives in each mod's own files.
