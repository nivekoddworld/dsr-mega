# ImGui overlay mods

The DS1Mod framework hooks D3D11's `Present` call and exposes a managed callback
per frame. Implement `IGuiMod` alongside your `ModBase` to render an ImGui
overlay directly into the game window — no separate process, no window, no
external renderer.

The reference implementation is `DS1Mod.ImGuiDemo` in
`DS1Mod/mods/DS1Mod.ImGuiDemo/`.

---

## How it works

```
DarkSoulsRemastered.exe
└── dinput8.dll  (DS1Mod.Injector)
    └── D3D11 Present hook  (d3d_hook.cpp)
        └── ImGui frame begin/end + DS1Mod_SetOnGuiCallback
            └── ImGuiRenderer (DS1Mod.Rendering)
                └── IGuiMod.OnGui() — called once per frame, per mod
```

The injector vendors Dear ImGui directly into `dinput8.dll`. The C++ hook calls
a single managed function pointer (set by `ImGuiRenderer`) which dispatches to
all loaded `IGuiMod` mods in order.

`DS1ImGui` (in `DS1Mod.Core.ImGui`) exposes the ImGui functions via P/Invoke
directly into `dinput8` — no separate `cimgui.dll` needed.

---

## 1. Project setup

Your mod needs `DS1Mod.SDK` and `DS1Mod.Core`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>MyOverlayMod</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\framework\DS1Mod.SDK\DS1Mod.SDK.csproj" />
    <ProjectReference Include="..\..\framework\DS1Mod.Core\DS1Mod.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## 2. Implementing IGuiMod

```csharp
using DS1Mod.SDK;
using DS1Mod.Core;
using DS1Mod.Core.ImGui;

public sealed class MyOverlayMod : ModBase, IGuiMod
{
    public override string Name    => "My Overlay";
    public override string Version => "1.0.0";
    public override string Author  => "YourName";

    public void OnGui()
    {
        if (DS1ImGui.Begin("My Window"))
            DS1ImGui.Text("Hello from DSR!");
        DS1ImGui.End();
    }
}
```

That's the minimal version. `DS1Mod.Host` detects that your mod implements
`IGuiMod` and passes it to `ImGuiRenderer` automatically — no registration needed.

---

## 3. Thread safety — the critical rule

`OnGui()` runs on the **render thread** (D3D11 Present).  
`OnTick()` runs on the **game thread** (DS1Mod event pump).

These are different threads. Do **not** read `GameMemory` or `IGameReader` inside
`OnGui()` — those accesses are not thread-safe from the render thread.

The correct pattern: cache values from `OnTick()` into `volatile` primitive
fields and read those fields in `OnGui()`.

```csharp
// Updated on the game thread (OnTick)
private volatile int   _hp    = 0;
private volatile int   _maxHp = 0;
private volatile int   _souls = 0;

public override void OnTick()
{
    var stats = _ctx!.Reader.GetPlayerStats();
    if (stats is not null) { _hp = stats.CurrentHp; _maxHp = stats.MaxHp; }
    _souls = _ctx.Reader.GetSouls();
}

// Read on the render thread (OnGui) — safe because fields are volatile primitives
public void OnGui()
{
    if (DS1ImGui.Begin("Stats"))
    {
        float frac = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        DS1ImGui.ProgressBar(frac, -1, 0, $"HP {_hp} / {_maxHp}");
        DS1ImGui.Text($"{_souls:N0} souls");
    }
    DS1ImGui.End();
}
```

`volatile` is sufficient for individual `int`/`float` reads. For composite state
(e.g. a struct with multiple fields that must be consistent), copy to a local
record/struct and use `Interlocked.Exchange` or a lock.

---

## 4. DS1ImGui API reference

`DS1ImGui` is a thin P/Invoke wrapper around the ImGui functions exported from
`dinput8.dll`. It covers the most common calls; use `Instr.Raw` for anything
missing (or file a PR to add it to `DS1ImGui`).

### Windows

```csharp
DS1ImGui.Begin("Title")                          // returns bool (window visible?)
DS1ImGui.Begin("Title", ref bool open)           // closeable window
DS1ImGui.Begin("##id", ImGuiWindowFlags flags)   // no title bar, etc.
DS1ImGui.End()                                   // always call, even if Begin returned false
```

### Positioning and sizing

```csharp
DS1ImGui.SetNextWindowPos(float x, float y, ImGuiCond cond = None)
DS1ImGui.SetNextWindowSize(float x, float y, ImGuiCond cond = None)
DS1ImGui.SetNextWindowBgAlpha(float alpha)       // 0 = transparent, 1 = opaque
```

`ImGuiCond.Always` forces position every frame (good for HUD pins).  
`ImGuiCond.FirstUseEver` sets position only on the first frame (lets the user drag it).

### Content

```csharp
DS1ImGui.Text("some string")
DS1ImGui.Separator()
DS1ImGui.Spacing()
DS1ImGui.Checkbox("label", ref bool value)       // returns true if changed
DS1ImGui.ProgressBar(fraction, sizeX, sizeY, overlay)
DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, r, g, b, a)
DS1ImGui.PopStyleColor()
```

### Diagnostics

```csharp
DS1ImGui.GetFramerate()    // game FPS from ImGui IO
DS1ImGui.GetVersion()      // Dear ImGui version string baked into dinput8
```

---

## 5. Common window patterns

### Pinned HUD (no title bar, no interaction)

```csharp
DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.Always);
DS1ImGui.SetNextWindowBgAlpha(0.65f);
var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize
          | ImGuiWindowFlags.NoMove     | ImGuiWindowFlags.AlwaysAutoResize;
if (DS1ImGui.Begin("##hud", flags))
    DS1ImGui.Text("...");
DS1ImGui.End();
```

### Closeable debug window

```csharp
private bool _show = true;

public void OnGui()
{
    if (!_show) return;
    DS1ImGui.SetNextWindowPos(10, 130, ImGuiCond.FirstUseEver);
    if (DS1ImGui.Begin("Debug", ref _show))
        DS1ImGui.Text($"FPS: {DS1ImGui.GetFramerate():F1}");
    DS1ImGui.End();
}
```

### HP bar with colour

```csharp
float frac = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0.75f, 0.1f, 0.1f);  // dark red
DS1ImGui.ProgressBar(frac, -1, 0, $"HP  {_hp} / {_maxHp}");
DS1ImGui.PopStyleColor();
```

---

## 6. Diagnosing a missing overlay

If the overlay never appears:

1. Check the console / `ds1mod.log` for `[D3DHook]` lines — the hook logs its
   DXGI swapchain acquisition. If they're absent, `dinput8.dll` didn't load or
   the D3D hook failed.

2. `ImGuiRenderer` logs a warning after 5 seconds if no frames have fired:
   ```
   [DS1Mod.Rendering] WARNING: Present hook has not fired after 5 s
   ```
   If you see this, the hook acquired a swapchain but Present isn't being called
   through it — possible alt-tab or windowed vs. fullscreen issue.

3. If frames are firing but nothing draws, your mod's `OnGui()` is throwing.
   `ImGuiRenderer` catches exceptions per mod and logs them:
   ```
   [DS1Mod.Rendering] OnGui exception in MyMod: ...
   ```

4. Confirm your mod class implements **both** `ModBase` (or `IGameMod`) **and**
   `IGuiMod` — `DS1Mod.Host` only passes mods that implement `IGuiMod` to the
   renderer.

---

## 7. Full example

See `DS1Mod/mods/DS1Mod.ImGuiDemo/ImGuiDemoMod.cs` for a complete implementation
with a stats panel (HP bar, soul level, position, map ID), a collapsible debug
window, checkboxes to toggle each panel, and FPS display.
