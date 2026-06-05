# Vendoring ImGui for DS1Mod.Injector

Copy these files from imgui 1.91.x (https://github.com/ocornut/imgui) into this directory:

  imgui.h
  imgui_internal.h
  imconfig.h
  imstb_rectpack.h
  imstb_textedit.h
  imstb_truetype.h
  imgui.cpp
  imgui_draw.cpp
  imgui_tables.cpp
  imgui_widgets.cpp
  backends/imgui_impl_dx11.h   → imgui_impl_dx11.h
  backends/imgui_impl_dx11.cpp → imgui_impl_dx11.cpp
  backends/imgui_impl_win32.h  → imgui_impl_win32.h
  backends/imgui_impl_win32.cpp → imgui_impl_win32.cpp

The injector (dinput8.dll) compiles imgui directly — no external cimgui.dll needed.
DS1Mod.Core.ImGui.DS1ImGui P/Invokes into dinput8.dll for managed draw calls.
