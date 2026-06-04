# DS1Mod — Dark Souls Remastered Mod Framework

`dinput8.dll` sideloads into DSR at startup. It applies the heap fix, then bootstraps
the .NET 8 runtime in-process via `hostfxr` and loads every `*.dll` found in
`<game dir>/mods/`.

## Architecture

```
DarkSoulsRemastered.exe
└── dinput8.dll  (C++, framework/DS1Mod.Injector)
    ├── ApplyHeapFix()      — prevents crash on large heaps
    ├── InitModLoader()     — loads hostfxr → .NET runtime
    │   └── DS1Mod.Host.dll (framework/DS1Mod.Host)
    │       └── ModLifecycleManager
    │           ├── scans mods/*.dll
    │           ├── loads each into its own AssemblyLoadContext
    │           ├── calls IGamePatcher.Patch(ctx) on patchers
    │           ├── calls IGameMod.OnLoad(IModContext)
    │           └── EventPump (500 ms tick)
    │               ├── polls event flags (boss kills, fog gates, deaths, levels)
    │               └── calls IGameMod.OnTick() on each mod
    └── D3D11 Present hook  — ImGui frame pump
        └── DS1Mod.Rendering (ImGuiRenderer)
            └── calls IGuiMod.OnGui() on each mod that implements it
```

## Solutions

| Solution | Purpose |
|---|---|
| `DS1Mod.Framework.slnx` | Framework only — Core, SDK, Host, Modding, Rendering. Build this to compile the runtime you ship with the randomizer. |
| `DS1Mod.Mods.slnx` | All bundled mods + their framework dependencies. Build this to develop or update a mod. |

## Writing a Mod

Reference `DS1Mod.SDK` and implement `ModBase`:

```csharp
using DS1Mod.SDK;

public class DeathCounter : ModBase
{
    public override string Name    => "Death Counter";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    private int _deaths;

    public override void OnLoad(IModContext ctx)
    {
        ctx.Hooks.PlayerDied += () =>
            Console.WriteLine($"Deaths: {++_deaths}");
    }
}
```

Build as a class library targeting `net8.0-windows`. Drop the DLL into
`<game dir>/mods/`. Launch via **▾ → Launch with Mod Framework**.

## Writing an ImGui Overlay Mod

Also implement `IGuiMod` — `OnGui()` is called every frame on the render thread:

```csharp
using DS1Mod.SDK;
using DS1Mod.Core;
using DS1Mod.Core.ImGui;

public class HudMod : ModBase, IGuiMod
{
    public override string Name    => "My HUD";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    public void OnGui()
    {
        DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.Always);
        DS1ImGui.SetNextWindowBgAlpha(0.65f);
        if (DS1ImGui.Begin("##hud", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize))
            DS1ImGui.Text("Hello from DSR!");
        DS1ImGui.End();
    }
}
```

See `mods/DS1Mod.ImGuiDemo` for a full example with a stats panel, HP bar, and
debug window. See [`docs/imgui-overlay.md`](../docs/imgui-overlay.md) for the full guide.

## Writing a Game-Data Patcher

Implement `IGamePatcher` (optionally alongside `IGameMod`) and use
`DS1Mod.Modding.GamePatch` for the heavy lifting:

```csharp
using DS1Mod.SDK;
using DS1Mod.Modding;

public class MyPatcher : ModBase, IGamePatcher
{
    public override string Name    => "My Patcher";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx.GameDir, ctx.BackupFile, Console.WriteLine);

        g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
            Texts.Set(bnd, Texts.EventText, 6900000, "Hello world!"));
    }
}
```

See `mods/DS1Mod.GoofyDemon` for a full example that patches Lua AI, EMEVD,
FMG text, and PARAM rows. See [`docs/writing-a-patcher-mod.md`](../docs/writing-a-patcher-mod.md) for the guide.

## Key Types

| Type | Role |
|---|---|
| `IGameMod` | Mod entry point — `OnLoad`, `OnUnload`, `OnTick` |
| `IGamePatcher` | Patch interface — `Patch(IPatchContext)` runs at title screen before any map loads |
| `IGuiMod` | Optional overlay interface — `OnGui()` called every frame on the render thread |
| `ModBase` | Abstract base class; all methods are virtual no-ops |
| `IModContext` | Passed to `OnLoad`; provides hooks, reader, writer, mods dir |
| `IGameHooks` | Events: `BossKilled`, `FogGateEntered`, `PlayerDied`, `PlayerLeveledUp` |
| `IGameReader` | Read player state, stats, souls, soul level, event flags |
| `IGameWriter` | Write event flags |
| `DS1ImGui` | P/Invoke wrappers for ImGui functions exported from `dinput8.dll` |
| `EventFlags` | Direct in-process bit-array access (flag get/set) |
| `GameMemory` | Unsafe in-process pointer reads/writes |
| `GamePatch` | `DS1Mod.Modding` — wraps DCX round-trip + backup for PARAM/FMG/EMEVD/Lua edits |

## Memory Model

Mods run **inside** the DSR process. `GameMemory.Read<T>` is a direct pointer
dereference — no `ReadProcessMemory`, no inter-process overhead.

`GameMemory.Initialize()` is called once by `DS1Mod.Host` at startup before any
mod's `OnLoad` is invoked, so mods can call `GameMemory.Read` safely from `OnLoad`.

`IGuiMod.OnGui()` runs on the **render thread** (D3D11 Present). Do not call
game-memory reads there — cache values from `OnTick()` into `volatile` fields and
read those in `OnGui()`.

## Installing via the Randomizer

The DS1 Mega Randomizer UI has a **MODS** tab. Click **Install Mod…** to copy a
DLL into the mods folder, or **Remove** to uninstall it.

---

## Mods in this Repo

| Mod | Type | What it does |
|---|---|---|
| **DS1Mod.DemoMod** | `IGameMod` + `IGamePatcher` | Exercises every SDK surface — all hooks, reader/writer, tick, unload. Good starting template. |
| **DS1Mod.FogLogger** | `IGameMod` | Logs every fog wall crossed (animation-based, catches all fogs not just boss fogs). Writes to console + `FogLogger.log`. |
| **DS1Mod.HpLogger** | `IGameMod` | Polls player HP on each tick; logs changes with delta and session low. Pattern reference for `OnTick` polling. |
| **DS1Mod.DiscordRPC** | `IGameMod` | Shows current activity, deaths, last boss killed, and session time in Discord Rich Presence. Requires a Discord application with `ds1_bonfire` / `ds1_skull` art assets registered. |
| **DS1Mod.AsylumSlam** | `IGamePatcher` | Locks Asylum Demon to slam attacks only. Patches `m18_01_00_00.luabnd.dcx` at launch with precompiled Lua 5.0 bytecode. |
| **DS1Mod.GoofyDemon** | `IGamePatcher` + `IGameMod` | Asylum Demon gets 10 random moods, an on-screen HUD showing the current mood, a `*farts*` message on landing, and a "Demon's Dignity (lost)" trinket on death. Patches luabnd + EMEVD + FMG + PARAM at launch via `DS1Mod.Modding`. v1.3. |
| **DS1Mod.ImGuiDemo** | `IGameMod` + `IGuiMod` | Minimal ImGui overlay — HP/souls stats panel and a collapsible debug window. Template for overlay mods. |

> `DS1Mod.AsylumSlam` and `DS1Mod.GoofyDemon` both patch the same Asylum Demon
> AI — do not load both at the same time. GoofyDemon supersedes AsylumSlam.
