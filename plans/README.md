# Plans

Ideas and research for future work on the DS1 Mega Randomizer and DS1Mod framework.

| Document | Topic |
|---|---|
| [new-emevd-lua-loading.md](new-emevd-lua-loading.md) | Loading completely new EMEVD / Lua files without piggybacking on vanilla |
| [randomizer-as-mod.md](randomizer-as-mod.md) | Convert the three randomizers into a single patcher mod; evolve the UI into a general mod manager |
| [multi-mod-conflict-resolution.md](multi-mod-conflict-resolution.md) | How multiple mods patching the same files can detect and avoid conflicts without central ID registration |
| [csharp-emevd-lua-api.md](csharp-emevd-lua-api.md) | Pure-C# fluent API for EMEVD event scripting and Lua AI scripting — eliminate raw instruction construction and Lua files from mod projects |

---

## Recommended implementation order

### 1. `multi-mod-conflict-resolution.md` — Phase 1 (EditRecord)

Conflict detection foundation. Purely additive to `GamePatch` — low risk, no API changes.
Everything else benefits from having this in place before multiple mods start sharing files.

### 2. `csharp-emevd-lua-api.md` — Phases 1 + 2 (named instructions + EventBuilder)

EMEVD builder before the randomizer-as-mod work. `DS1Randomizer.dll` will need to emit
EMEVD patches; build the good API first rather than retrofitting raw instructions later.

### 3. `randomizer-as-mod.md` — Phases 1–3 (config bridge, framework additions, DS1Randomizer.dll)

Depends on the Modding API being solid. The framework additions (`ModConfig<T>`,
`PatchOrderAttribute`, `IModContext.GameDir`) are small and can land just before or
alongside Phase 1–2 of the C# API work.

### 4. `csharp-emevd-lua-api.md` — Phase 3 (Lua AI builder)

Independent of the randomizer migration but higher effort. Do it once the randomizer
patcher is stable so the new Lua API gets a real workout immediately.

### 5. `new-emevd-lua-loading.md` — Approach 4 (in-process EMEVD hook)

The hardest research item — requires AOB scan + native hook work. Only relevant once a
concrete mod needs fully standalone event scripts with no vanilla file touched. Defer until
then.

### 6. `randomizer-as-mod.md` — Phases 4–5 (UI mod manager evolution)

Last because it touches the most user-facing code and depends on all the framework
plumbing being stable.

### 7. `multi-mod-conflict-resolution.md` — Phases 2–4 (manifests, ModIdSpace, registry)

Polish layer once there are actually multiple mods coexisting and ID collisions become a
real problem rather than a theoretical one.
