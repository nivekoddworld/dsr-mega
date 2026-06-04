# Plan: Randomizer-as-mod and mod manager UI

Convert the DS1 Mega Randomizer into a `DS1Randomizer.dll` patcher mod, and
evolve the UI from a bespoke randomizer app into a general mod manager that
knows how to configure it.

**Status: unstarted**  
**Depends on:** DS1Mod framework additions (Phase 2), no blockers for Phase 1

---

## Goal

Users should be able to:
1. Drop `DS1Randomizer.dll` into `mods/` like any other mod
2. Configure it via the UI (seed, fog/item/enemy toggles and settings)
3. Launch the game — randomization runs at the title screen alongside all other
   installed mods, in the same `IGamePatcher` pipeline
4. Install, configure, enable/disable *any* mod from the same UI

---

## Why the three randomizers stay as one DLL

The tempting split — three separate `IGamePatcher` mods — breaks on three hard
problems:

1. **Shared write targets.** Both FogGate and Enemies write to the same MSB and
   EMEVD files. They currently coordinate through a single in-memory `GameData`
   object loaded once. Two separate patchers hitting the same files would
   clobber each other.

2. **Softlock detection is cross-system.** The retry loop in `MegaRandomizer.cs`
   increments the seed and re-runs all three as a unit. Fog's output
   (`AreaRatios`, world graph) feeds both Item and Enemy. A fog-only re-seed
   would invalidate the other two.

3. **No inter-patcher data bus in the framework.** Passing `FogGateResult` to
   Item and Enemy across separate DLL boundaries would need a shared-state
   mechanism that doesn't exist and isn't worth building just for this case.

**Decision:** Keep the three randomizers coupled internally. Package them as
`DS1Randomizer.dll` — one mod, one entry point, existing pipeline unchanged
inside. This is the same pattern as `DS1Mod.GoofyDemon`: one `IGamePatcher`
that internally coordinates Lua + EMEVD + FMG + PARAM edits.

---

## Architecture

### Runtime

```
mods/
  DS1Randomizer.dll          ← IGamePatcher; runs at title screen
  DS1Randomizer.settings.json ← written by the UI; read by the patcher
  DS1Mod.GoofyDemon.dll       ← any other mods, unaffected
  ...
```

`DS1Randomizer.dll` at patch time:
1. Loads `DS1Randomizer.settings.json` from `ctx.ModsDir`
2. Runs the existing `MegaRandomizer` pipeline (Fog → Items → Enemies, retry
   loop, softlock check)
3. Writes game files via `GamePatch` / `GameFileWriter`

Nothing inside the three randomizers changes. The only difference from today:
settings arrive from a JSON file instead of an in-process call from the UI.

### UI (mod manager mode)

```
DS1MegaRando.UI
  ├── Mod list  (existing MODS tab, extended)
  │     ├── Enable / Disable toggle per mod
  │     ├── Configure button  → per-mod settings panel
  │     └── Install / Remove  (already exists)
  ├── DS1Randomizer config panel  (existing settings pages, unchanged)
  │     ├── Seed + Roll
  │     ├── FogGate page
  │     ├── Item page
  │     └── Enemy page
  │     Saves to: mods/DS1Randomizer.settings.json
  └── Global settings  (GameDir, language, launch)
```

The "LINK THE FIRE" button changes meaning: from "run randomization now and
write files" to "save settings JSON and launch the game." Actual randomization
happens inside `DS1Randomizer.dll` at title-screen time.

The existing FogGate/Item/Enemy settings pages don't go away — they become the
configuration panel for the randomizer mod, embedded in the manager UI.

---

## Framework additions required

### 1. `IModContext.GameDir`

Runtime mods (`IGameMod`) currently only receive `ModsDir` via `IModContext`.
Add `GameDir` for consistency with `IPatchContext`:

```csharp
public interface IModContext
{
    IGameHooks Hooks  { get; }
    IGameReader Reader { get; }
    IGameWriter Writer { get; }
    string ModsDir    { get; }
    string GameDir    { get; }   // add
}
```

File: `DS1Mod/framework/DS1Mod.Core/IModContext.cs`  
Also update `DS1Mod/framework/DS1Mod.Host/ModLifecycleManager.cs` to pass it.

### 2. `ModConfig<T>` helper (convention, not interface)

Establish the convention: `{ModsDir}/{AssemblyName}.settings.json`.  
Add a small helper to `DS1Mod.Modding`:

```csharp
public static class ModConfig
{
    public static T Load<T>(string modsDir, string assemblyName) where T : new()
    {
        var path = Path.Combine(modsDir, $"{assemblyName}.settings.json");
        if (!File.Exists(path)) return new T();
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? new T();
    }

    public static void Save<T>(string modsDir, string assemblyName, T config)
    {
        var path = Path.Combine(modsDir, $"{assemblyName}.settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(config,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
```

File: `DS1Mod/framework/DS1Mod.Modding/ModConfig.cs` (new)

No changes to any interface. Mods opt in by calling `ModConfig.Load` in `Patch()` or `OnLoad()`.

### 3. Patch ordering

`ModLifecycleManager` currently runs all patchers without a guaranteed order.
Add a `[PatchOrder(int)]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class PatchOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
```

`ModLifecycleManager` sorts patchers by this value (ascending) before invoking
`Patch()`. Mods that don't declare it get order `0`.

File: `DS1Mod/framework/DS1Mod.Core/PatchOrderAttribute.cs` (new)  
File: `DS1Mod/framework/DS1Mod.Host/ModLifecycleManager.cs` (update sort)

### 4. Enable/disable per mod

Add a `disabled-mods.json` list to the mods directory. `ModLifecycleManager`
skips DLLs whose assembly name appears in the list.

```json
["DS1Mod.GoofyDemon", "DS1Mod.DiscordRPC"]
```

The UI writes this file when the user toggles a mod off.

---

## UI changes

### Mod list extensions (ModsPage)

| Add | Notes |
|---|---|
| Enable/disable toggle | Writes to `disabled-mods.json` |
| "Configure" button | Opens mod's config panel (see below) |
| Mod metadata (Name, Version, Author, Description) | Read from `ModBase` properties at load — need a lightweight assembly inspector in the UI |
| Load order (drag-to-reorder) | Writes `load-order.json`; read by `ModLifecycleManager` |

### Per-mod config panel

Two tiers:

1. **Bespoke panel** — for `DS1Randomizer.dll`, the existing FogGate/Item/Enemy
   settings pages. The "Configure" button opens them in a side-panel or dialog.
   Saving writes `DS1Randomizer.settings.json`.

2. **Generic JSON editor fallback** — for any other mod that ships no custom
   UI, the "Configure" button opens a simple text editor for
   `{ModName}.settings.json`. Enough for power users.

A future enhancement (not required for v1) would be a schema-driven auto-
generated form, similar to BepInEx Configuration Manager or Archipelago's
options UI. Defer this until at least two mods need it.

### "LINK THE FIRE" → "Save & Launch"

The button flow changes:

```
Before: validate → run MegaRandomizer in-process → write files → show result
After:  validate → serialize settings JSON → install DS1Randomizer.dll (if needed)
        → launch game → title screen triggers DS1Randomizer.dll.Patch()
```

Keep the old "run in-process" path as a **dry-run / preview mode** accessible
from the debug page. It's useful for spoiler log generation without launching
the game.

---

## Settings JSON format

`DS1Randomizer.settings.json` in `mods/` is the serialized `MegaSettings`
object. Version it from day one to allow forward-compatible migration:

```json
{
  "version": 1,
  "seed": "1A2B3C4D",
  "global": { "language": "English", "createSpoilerLog": true },
  "fogGate": { "enabled": true, "logicMode": "Normal", ... },
  "items":   { "enabled": true, "difficulty": "Medium", ... },
  "enemies": { "enabled": true, "scalingMode": "FogGateDepth", ... }
}
```

Add a `version` field to `MegaSettings`. Migration: on load, if version < current,
apply migration functions before deserializing into the typed model.

---

## Migration phases

### Phase 1 — Config file bridge (one afternoon, no visible change)

- Add `ModConfig<T>` helper to `DS1Mod.Modding`
- Have the UI serialize `MegaSettings` to `mods/DS1Randomizer.settings.json`
  after every run (in addition to running in-process as today)
- No behaviour change for users; just builds the file as a side effect

### Phase 2 — Framework additions

- Add `IModContext.GameDir`
- Add `PatchOrderAttribute` + sort in `ModLifecycleManager`
- Add `disabled-mods.json` skip logic

### Phase 3 — `DS1Randomizer.dll` patcher mod

- Create new project `DS1Randomizer` (or rename `DS1MegaRando.Core` entry point)
- Implements `IGamePatcher`, reads settings JSON, delegates to existing
  `MegaRandomizer` class — minimal new code
- Ship in `bundled-mods/`
- Add to `DS1Mod.Mods.slnx`

### Phase 4 — UI mod manager evolution

- Extend MODS tab: enable/disable toggle, Configure button
- Wire "Configure" for `DS1Randomizer.dll` to the existing settings pages
- Change "LINK THE FIRE" to write JSON + launch; keep in-process as dry-run
- Add generic JSON editor fallback for other mods

### Phase 5 — Clean up

- Remove the direct `MegaRandomizer.RandomizeAsync()` call from the UI's main
  flow (replaced by Phase 4)
- `DS1MegaRando.Core` / `.FogGate` / `.Items` / `.Enemies` become internal
  implementation details of `DS1Randomizer.dll`
- `DS1MegaRando.UI` retains settings pages purely as a config frontend

---

## What this unlocks

- **Randomizer is a mod.** Users can disable it for a vanilla playthrough, enable
  it alongside GoofyDemon, etc. All mods co-exist in the same `mods/` folder.
- **Third-party randomizer mods.** Someone can write `MyItemRando.dll` as a
  standalone `IGamePatcher` using the same framework.
- **Clean separation.** The UI does not know or care about game-file formats. It
  writes a JSON; the DLL handles everything else.
- **Spoiler log on demand.** Add a "dry-run" mode to `DS1Randomizer.dll` that
  generates the spoiler log without writing game files (triggered from the UI's
  existing debug/spoiler page).

---

## Risks and open questions

| Risk | Mitigation |
|---|---|
| Settings JSON schema changes break existing configs | Version field + migration functions from day one |
| Title-screen timing — patcher runs before maps load but after some UI | Already how all `IGamePatcher` mods work; no new risk |
| Assembly inspector for mod metadata in UI (need to read DLL without loading it) | Use `MetadataLoadContext` (reflection-only) to read `Name`/`Version`/`Author` without executing the DLL |
| Load order between `DS1Randomizer.dll` and other patchers that also touch MSBs | `PatchOrderAttribute` (Phase 2) solves this; randomizer runs first at order `-100` |
| Dry-run / spoiler-log without launching the game | Keep in-process path in the UI as a debug mode; `DS1Randomizer.dll` can also accept a dry-run flag in its settings JSON |
