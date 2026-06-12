---
name: ds1-lifecycle
description: Authoritative reference for building C# mods that run inside Dark Souls Remastered using the DS1Mod framework.
---

# DS1Mod SDK — Mod Lifecycle, Hooks, Reader/Writer & Overlays

Authoritative reference for building C# mods that run inside Dark Souls Remastered using the DS1Mod framework. Every mod lives in its own DLL in `<game>/mods/` and is hot-loaded by the framework at game startup.

---

## 1. Quick Start — Minimal Mod

```csharp
using DS1Mod.Core;
using DS1Mod.SDK;

namespace MyFirstMod;

public sealed class MyMod : ModBase
{
    public override string Name    => "My Cool Mod";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    public override void OnLoad(IModContext ctx)
    {
        Console.WriteLine("[MyMod] Loaded! Subscribing to hooks...");
        ctx.Hooks.PlayerDied += () => Console.WriteLine("[MyMod] You died!");
    }

    public override void OnTick()
    {
        // Called every ~500ms while in-game
    }

    public override void OnUnload()
    {
        Console.WriteLine("[MyMod] Unloaded!");
    }
}
```

---

## 2. Project Setup

### .csproj Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>DS1Mod.MyMod</AssemblyName>
    <RootNamespace>DS1Mod.MyMod</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\framework\DS1Mod.SDK\DS1Mod.SDK.csproj" />
  </ItemGroup>
</Project>
```

**Reference requirements:**
- `DS1Mod.SDK` — always required (provides `ModBase`, pulls in `DS1Mod.Core` transitively)
- `DS1Mod.Core` — needed when using `DS1ImGui` or `GameMemory` directly
- `DS1Mod.Modding` — needed for file patching (`GamePatch`, `EsdEditor`, `AiBuilder`, etc.)
- `SoulsFormats` — needed for direct `BND3`/`MSB1`/`ESD` manipulation

### Output Location

Build output goes to `<game>/mods/DS1Mod.MyMod.dll`. The framework scans `<game>/mods/` for `DS1Mod.*.dll` assemblies at startup and loads each into its own `AssemblyLoadContext`.

---

## 3. Core Interfaces & Inheritance

### ModBase (Recommended)

Abstract base class. All methods are virtual no-ops — override only what you need.

```csharp
public abstract class ModBase : IGameMod
{
    public abstract string Name    { get; }
    public abstract string Version { get; }
    public abstract string Author  { get; }

    public virtual void OnLoad  (IModContext ctx) { }
    public virtual void OnUnload()                { }
    public virtual void OnTick  ()                { }
}
```

### IGameMod (Direct Implementation)

Only needed when you cannot inherit `ModBase`. Same contract.

### IGamePatcher (Optional)

Implement alongside `ModBase` to modify game files on disk before any map loads. `Patch()` is called once at startup (while on the title screen), before `OnLoad()`.

```csharp
public class MyMod : ModBase, IGamePatcher
{
    public void Patch(IPatchContext ctx)
    {
        // ctx.GameDir — path to UXM-extracted DSR directory
        // ctx.ModsDir — path to <GameDir>/mods/
        // ctx.BackupFile(path) — creates .bak if not already present
        // ctx.Log(msg) — write to patch log
        // ctx.AllocateId(space) / AllocateIds(space, count) — persistent ID allocation
    }
}
```

### IGuiMod (Optional)

Implement alongside `ModBase` to render an ImGui overlay. `OnGui()` is called every frame on the render thread.

```csharp
public class MyMod : ModBase, IGuiMod
{
    private bool _showWindow = true;

    public void OnGui()
    {
        if (DS1ImGui.Begin("My Window", ref _showWindow))
        {
            DS1ImGui.Text("Hello from DSR!");
        }
        DS1ImGui.End();
    }
}
```

> **Important:** All `DS1ImGui.*` calls must happen inside `OnGui()`. Do not call them from `OnTick()` or hook handlers — those run on the game thread, not the render thread.

---

## 4. IModContext — The Central Interface

Received in `OnLoad(IModContext ctx)`:

| Property | Type | Purpose |
|----------|------|---------|
| `Hooks` | `IGameHooks` | Subscribe to game events |
| `Reader` | `IGameReader` | Read game state (flags, HP, position, etc.) |
| `Writer` | `IGameWriter` | Write event flags |
| `ModsDir` | `string` | Path to `<game>/mods/` |

---

## 5. IGameHooks — Event Subscriptions

All hooks fire from a 500ms poll loop on a background thread. Subscribe in `OnLoad()`, unsubscribe is not needed (mod lifecycle ends them).

### Event Reference

| Event | Signature | Trigger Conditions |
|-------|-----------|-------------------|
| `BossKilled` | `Action<BossKill>` | A boss flag was just set. `BossKill` has `BossName`, `FlagId`, `KilledAt`. |
| `FogGateEntered` | `Action<FogGate>` | Player passed through a boss fog gate. `FogGate` has `Name`, `MapId`, `FlagId`. |
| `PlayerDied` | `Action` | Player HP dropped to 0 |
| `PlayerLeveledUp` | `Action<int>` | Player gained a soul level. Arg = new SL. |
| `ItemUsed` | `Action<int>` | A registered item was used. Arg = goodsId. |
| `EnemyDamaged` | `Action<EnemyDamage>` | Any loaded enemy lost HP. Fires from a dedicated 50 ms diff thread (NOT the 500 ms pump) — near-instant. `EnemyDamage` has `Character` (nint), `Damage`, `CurrentHp`, `MaxHp`, `DistanceToPlayer` (meters, -1 if unknown). Lazy: no subscribers → no scan threads. Lock any state shared with `OnTick`. |

### Registering Custom Item Detection

```csharp
public override void OnLoad(IModContext ctx)
{
    // Step 1: Register a (goodsId → triggerFlagId) pair.
    // triggerFlagId must be wired via DefineItemTrigger in your patcher.
    ctx.Hooks.RegisterItemUsed(myGoodsId, myTriggerFlagId);

    // Step 2: Subscribe to ItemUsed
    ctx.Hooks.ItemUsed += goodsId =>
    {
        if (goodsId == myGoodsId)
            Console.WriteLine("Player used my custom item!");
    };
}
```

### Handler Safety

Hook handlers run on the poll thread (`EnemyDamaged` on its own 50 ms thread). Keep them fast. Use `volatile` fields to share state with the render thread (ImGui), and a `lock` for state touched by both `OnTick` and `EnemyDamaged`. Do NOT call `DS1ImGui.*` from handlers. The pump swallows handler exceptions silently — wrap risky logic in try/catch with your own logging or failures are invisible.

---

## 5b. ModConfig — User-Editable Settings (MODS tab UI)

Override `InitializeConfig` to declare a schema; the randomizer UI renders it on the MODS tab and users edit `<game>/mods/config/<ModName>.json`.

```csharp
public override void InitializeConfig(ModConfig config)
{
    config.AddBool("enabled", true).Tooltip("Master switch");
    config.AddFloat("multiplier", 1.0f).Tooltip("Effect strength");
    config.AddInt("count", 3).Tooltip("How many");
    config.AddString("mode", "normal").Tooltip("normal | hard");
}

public override void OnLoad(IModContext ctx)
{
    string gameDir = Directory.GetParent(ctx.ModsDir)?.FullName ?? ctx.ModsDir;
    LoadConfigAsync(gameDir).GetAwaiter().GetResult();   // populates Config

    bool on = Config?.GetBool("enabled", true) == true;  // Get* anywhere after load
}
```

`Config` is `protected ModConfig?` on `ModBase`. Getters: `GetBool/GetInt/GetFloat/GetString(key, default)`. Always null-guard `Config` — it's null until `LoadConfigAsync` completes.

---

## 6. IGameReader — Reading Game State

All methods poll live game memory. Returns `null` / defaults if not in-game (loading screen, menu).

```csharp
ctx.Reader.GetEventFlag(flagId);       // bool — is flag ON?
ctx.Reader.GetPlayerState();           // PlayerState? — X, Y, Z, MapId
ctx.Reader.GetPlayerStats();           // PlayerStats? — CurrentHp, MaxHp, CurrentStamina, MaxStamina
ctx.Reader.GetSoulLevel();             // int
ctx.Reader.GetSouls();                 // int — souls currently held
ctx.Reader.GetCurrentAnimation();      // int — player animation ID (0 if not loaded)
```

### PlayerState

```csharp
public sealed record PlayerState(float X, float Y, float Z, string MapId);
// MapId is currently always empty (no reliable game pointer found yet).
// X/Y/Z are accurate world coordinates.
```

### PlayerStats

```csharp
public sealed record PlayerStats(int CurrentHp, int MaxHp, float CurrentStamina, float MaxStamina)
{
    public float HpFraction => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;
    public float StaminaFraction => MaxStamina > 0 ? CurrentStamina / MaxStamina : 0f;
}
```

---

## 7. IGameWriter — Writing Game State

```csharp
ctx.Writer.SetEventFlag(flagId, value);   // Set a flag ON or OFF
```

---

## 8. IGamePatcher + IPatchContext — File Patching

### Patch Phase Lifecycle

```
Game boots → Game starts → Patch() called (title screen) → OnLoad() → Game loop (tick, hooks, OnGui)
```

### IPatchContext

| Member | Purpose |
|--------|---------|
| `ctx.GameDir` | Path to UXM-extracted DSR directory |
| `ctx.ModsDir` | Path to `<GameDir>/mods/` |
| `ctx.BackupFile(path)` | Creates `<path>.bak` on first call (idempotent) |
| `ctx.Log(msg)` | Write to the host's patch log |
| `ctx.AllocateId(space)` | Get one unique ID from a named space |
| `ctx.AllocateIds(space, count)` | Get a contiguous block of unique IDs |
| `ctx.AllocateBonfireSlot()` | Get next available bonfire menu slot index |
| `ctx.GetBonfireEsd()` | Shared bonfire ESD editor (returns `EsdEditor?`) |
| `ctx.RecordEdit(file, selector)` | Record an edit for cross-mod conflict detection |

### ID Allocation — CRITICAL for Multi-Mod Compatibility

Always use `AllocateId` / `AllocateIds` instead of hardcoding IDs. The allocator is persistent — the same mod always gets the same IDs across runs (save-game compatibility).

Available ID spaces (use `IdSpaces` constants):

```csharp
IdSpaces.EquipParamGoods           // "EquipParamGoods"
"EquipParamAccessory"              // rings (base 900) — string key, no constant yet
IdSpaces.ItemLotParam              // "ItemLotParam"
IdSpaces.SpEffectParam             // "SpEffectParam"
IdSpaces.EventText                 // "EventText" (FMG)
IdSpaces.StatusText                // "StatusText" (FMG)
IdSpaces.ItemName                  // "ItemName" (FMG)
IdSpaces.ItemDescription           // "ItemDescription" (FMG)
IdSpaces.ItemLongDesc              // "ItemLongDesc" (FMG)
IdSpaces.ItemObtainedFlags         // "ItemObtainedFlags" (50000000+)
IdSpaces.EventFlags(mapId)         // "EventFlags_m18_01_00_00" etc.
IdSpaces.EmevdEvents(mapId)        // "EmevdEvents_m18_01_00_00"
IdSpaces.MsbEntities(mapId)        // "MsbEntities_m18_01_00_00"
```

---

## 9. DS1ImGui — Render Overlays

The framework exposes ImGui functions directly from `dinput8.dll` via P/Invoke. No separate `cimgui.dll` required.

### Getting Started

```csharp
using DS1Mod.Core.ImGui;

public void OnGui()
{
    // Position and size (first use only)
    DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.FirstUseEver);
    DS1ImGui.SetNextWindowSize(300, 200, ImGuiCond.FirstUseEver);
    DS1ImGui.SetNextWindowBgAlpha(0.85f);

    if (DS1ImGui.Begin("My Window", ref _showWindow))
    {
        DS1ImGui.Text("Hello from DSR!");
        DS1ImGui.Separator();
        DS1ImGui.Text($"HP: {_hp}/{_maxHp}");

        if (DS1ImGui.Button("Click me"))
            DoSomething();

        DS1ImGui.Checkbox("Show debug", ref _showDebug);
    }
    DS1ImGui.End();
}
```

### Available Widgets

| Function | Purpose |
|----------|---------|
| `Begin(name)` / `Begin(name, ref open)` | Start a window |
| `End()` | End a window |
| `SetNextWindowPos(x, y, cond)` | Window position |
| `SetNextWindowSize(x, y, cond)` | Window size |
| `SetNextWindowBgAlpha(alpha)` | Background opacity |
| `Text(str)` | Static text |
| `Button(label)` | Clickable button |
| `Checkbox(label, ref bool)` | Toggle checkbox |
| `ProgressBar(fraction, w, h, overlay)` | Progress bar |
| `Separator()` | Horizontal line |
| `Spacing()` | Vertical space |
| `SameLine()` | Place next widget on same line |
| `PushStyleColor(col, r, g, b, a)` | Push color |
| `PopStyleColor(count)` | Pop color |
| `CollapsingHeader(label)` | Collapsible section |
| `SliderInt(label, ref int, min, max)` | Integer slider |
| `SliderFloat(label, ref float, min, max)` | Float slider |
| `InputInt(label, ref int)` | Integer input |
| `InputFloat(label, ref float)` | Float input |
| `BeginTabBar(id)` / `EndTabBar()` | Tab bar |
| `BeginTabItem(label)` / `EndTabItem()` | Tab item |
| `BeginChild(id)` / `EndChild()` | Scrollable child region |
| `BeginCombo(label, preview)` / `EndCombo()` | Combo box |
| `Selectable(label, selected)` | Selectable item |
| `TextDisabled(str)` | Dimmed text |
| `GetFramerate()` | Current FPS |

### Window Flags

```csharp
// Common flag combinations:
var statsFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
               | ImGuiWindowFlags.NoMove     | ImGuiWindowFlags.AlwaysAutoResize;

var debugFlags = ImGuiWindowFlags.None; // Default — resizable, dockable, etc.
```

### Thread Safety

`OnGui()` runs on the render thread. `OnTick()` and hook handlers run on a background thread. Share state with `volatile` fields — safe for primitive reads:

```csharp
private volatile int _hp;
private volatile int _maxHp;

public override void OnTick()
{
    var stats = ctx.Reader.GetPlayerStats();
    if (stats is not null)
    {
        _hp = stats.CurrentHp;
        _maxHp = stats.MaxHp;
    }
}

public void OnGui()
{
    DS1ImGui.Text($"HP: {_hp}/{_maxHp}");
}
```

---

## 10. BossKill & FogGate Records

```csharp
// Fired when a boss kill event flag is detected
public sealed record BossKill(string BossName, int FlagId, DateTime KilledAt);

// Fired when player passes through a fog gate (animation-based detection)
public sealed record FogGate(string Name, string MapId, int FlagId);
```

### Boss Names

Boss name → flag ID mapping is driven by `BossIds.All` from `DS1Mod.Core`. You can look up the flag ID for a boss:

```csharp
var boss = BossIds.ByEntityId(1810800);
// boss.Name = "Asylum Demon"
// boss.FlagId is derived from the event flag
```

### Fog Gate Detection

Fog gate detection is animation-based (not flag-based), so it catches ALL fog walls — not just boss fogs.

---

## 11. GameState — In-Game Detection

```csharp
bool inGame = GameState.IsInGame();  // True only when world + player + flags are live
string desc = GameState.Describe();   // Human-readable pointer dump for debugging
```

The EventPump already uses `GameState.IsInGame()` internally — mod `OnTick()` calls are skipped when not in-game.

---

## 12. Example: Full Composite Mod

```csharp
using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.SDK;

namespace DS1Mod.MyFullMod;

public sealed class MyFullMod : ModBase, IGamePatcher, IGuiMod
{
    public override string Name    => "My Full Mod";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    private IModContext? _ctx;
    private int _deaths;
    private int _bossKills;
    private bool _showWindow = true;

    // Volatile fields for ImGui thread safety
    private volatile int _hp;
    private volatile int _maxHp;
    private volatile int _soulLevel;

    // ── Patching ──────────────────────────────────────────────────────────────
    public void Patch(IPatchContext ctx)
    {
        int myFlag = ctx.AllocateId(IdSpaces.EventFlags("m18_01_00_00"));
        ctx.Log($"My mod allocated flag {myFlag}");

        // Use GamePatch for file edits (see ds1mod-modding skill)
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        ctx.Hooks.PlayerDied      += () => { _deaths++; Console.WriteLine($"Death #{_deaths}"); };
        ctx.Hooks.BossKilled      += kill => { _bossKills++; Console.WriteLine($"Killed {kill.BossName}"); };
        ctx.Hooks.PlayerLeveledUp += sl => Console.WriteLine($"Reached SL {sl}");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;
        var stats = _ctx.Reader.GetPlayerStats();
        if (stats is not null) { _hp = stats.CurrentHp; _maxHp = stats.MaxHp; }
        _soulLevel = _ctx.Reader.GetSoulLevel();
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[Mod] Unloaded. Killed {_bossKills} bosses, died {_deaths} times.");
    }

    // ── ImGui ────────────────────────────────────────────────────────────────
    public void OnGui()
    {
        if (!_showWindow) return;
        DS1ImGui.SetNextWindowBgAlpha(0.8f);
        if (DS1ImGui.Begin("My Mod", ref _showWindow))
        {
            DS1ImGui.Text($"HP: {_hp}/{_maxHp}");
            DS1ImGui.Text($"SL: {_soulLevel}");
            DS1ImGui.Text($"Deaths: {_deaths}");
            DS1ImGui.Text($"Boss kills: {_bossKills}");
        }
        DS1ImGui.End();
    }
}
```

---

## 13. Framework Architecture Notes

| Assembly | Purpose |
|----------|---------|
| `DS1Mod.SDK` | Public API — `ModBase` base class. Depends on Core. |
| `DS1Mod.Core` | Runtime — hooks, reader/writer, `DS1ImGui`, `GameMemory`, `GameState`, `EventFlags`, `BossIds`, `EnemyIds`, `MapIds`, `IdSpaces` |
| `DS1Mod.Modding` | File patching — `GamePatch`, `EsdEditor`, `ActionEsdEditor`, `AiBuilder`, `ParamRepository`, etc. |
| `DS1Mod.Host` | Mod loader — `ModLifecycleManager`, `ModAssemblyLoadContext`. Not referenced by mods. |
| `DS1Mod.Rendering` | D3D11 Present hook for ImGui. Not referenced by mods. |
| `DS1Mod.Injector` | C++ `dinput8.dll` — disables Arxan, bootstraps .NET runtime. Not referenced by mods. |
| `SoulsFormats` | Official FromSoft file format library — `BND3`, `MSB1`, `ESD`, `EMEVD`, `FMG`, etc. |

Mods reference `DS1Mod.SDK` (which transitively references `DS1Mod.Core`) and optionally `DS1Mod.Modding` and `SoulsFormats`.

---

## 14. Key Constants & Type References

### Map IDs (MapIds / IdSpaces)

```csharp
MapIds.UndeadAsylum       // "m18_01_00_00"
MapIds.UndeadParish       // "m10_01_00_00"
MapIds.TheDepths          // "m10_00_00_00"
MapIds.PaintedWorld       // "m11_00_00_00"
MapIds.DarkrootGarden     // "m12_00_00_01"
MapIds.DemonRuins         // "m14_01_00_00"
MapIds.AnorLondo          // "m15_01_00_00"
MapIds.KilnOfTheFirstFlame // "m18_00_00_00"
// ... see DS1Mod.Core.MapIds for full list
```

### Enemy Model IDs (EnemyIds)

```csharp
EnemyIds.Hollow           // "c2500"
EnemyIds.BlackKnight      // "c2790"
EnemyIds.SilverKnight     // "c2410"
EnemyIds.Smough           // "c2360"
EnemyIds.Ornstein         // "c5270"
EnemyIds.KnightArtorias   // "c4100"
// ... see EnemyIds.All for every enemy definition
```

### Boss Entity IDs (BossIds)

```csharp
BossIds.All — IReadOnlyList<BossDef> with:
  boss.MapId, boss.EntityId, boss.ModelId, boss.Name,
  boss.CanReplace, boss.EmevdPatches

BossIds.ByEntityId(entityId) — look up a boss by MSB entity ID
BossIds.Replaceable — only bosses with CanReplace: true
```

### GameMemory (Low-Level Access)

```csharp
GameMemory.Initialize()            // Call once at startup (done by host)
GameMemory.Read<T>(address)        // Safe dereference with page validation
GameMemory.Write<T>(address, val)  // Safe write with page validation
GameMemory.Resolve(staticOffset, offsets...)  // Resolve pointer chain
GameMemory.Scan(pattern)           // AOB scan ("48 8B 05 ? ? ? ?")
```

> **Warning:** `GameMemory` is for advanced use (hooks, AOB scanners). Most mods only need `IModContext.Reader` / `Writer`.

### PlayerBody (Player HP Read/Write)

```csharp
(int hp, int maxHp) = PlayerBody.ReadHp();  // (0,0) when not loaded
PlayerBody.WriteHp(hp + 50);                 // direct heal/damage
```

Applies the per-version ChrIns offset boost automatically — never read raw `0x3D8` offsets yourself (garbage on 1.03+). For RTTI scans, heap scanning, and native HUD bar control, see `ds1mod-memory.SKILL.md`.
