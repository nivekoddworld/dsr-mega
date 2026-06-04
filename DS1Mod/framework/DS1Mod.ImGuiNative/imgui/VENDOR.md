# Vendoring imgui and cimgui

This directory must contain the imgui and cimgui source trees before you can build `cimgui.dll`.

## Step 1 — imgui (Dear ImGui)

Download the latest release from https://github.com/ocornut/imgui/releases (tested with 1.91.x).

Copy these files into this `imgui/` directory:

```
imgui.h
imgui.cpp
imgui_draw.cpp
imgui_tables.cpp
imgui_widgets.cpp
imgui_internal.h
imconfig.h
imstb_rectpack.h
imstb_textedit.h
imstb_truetype.h
backends/imgui_impl_dx11.h
backends/imgui_impl_dx11.cpp
backends/imgui_impl_win32.h
backends/imgui_impl_win32.cpp
```

## Step 2 — cimgui (C wrapper)

Clone or download https://github.com/cimgui/cimgui (match the imgui version).

Copy these files into this `imgui/` directory alongside the imgui sources:

```
cimgui.h
cimgui.cpp
```

> cimgui.cpp includes imgui.cpp internally via relative path — make sure
> all sources are in the same flat directory.

## Step 3 — Build

Open `DS1Mod.ImGuiNative.vcxproj` in Visual Studio and build Release|x64.
Output: `cimgui.dll`

Deploy `cimgui.dll` to the same directory as `dinput8.dll` in the DSR game folder.
