// imgui_capi.cpp
// C-ABI exports of ImGui functions from dinput8.dll.
// Managed code P/Invokes these via DS1ImGui.cs (targeting "dinput8").

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "imgui/imgui.h"
#include <cstring>

extern "C"
{
    // Context
    __declspec(dllexport) void* igGetCurrentContext() { return ImGui::GetCurrentContext(); }

    // Windows
    __declspec(dllexport) bool igBegin(const char* name, bool* p_open, int flags)
        { return ImGui::Begin(name, p_open, (ImGuiWindowFlags)flags); }
    __declspec(dllexport) void igEnd() { ImGui::End(); }

    // Layout
    __declspec(dllexport) void igSetNextWindowPos(float x, float y, int cond, float pivot_x, float pivot_y)
        { ImGui::SetNextWindowPos({x,y}, (ImGuiCond)cond, {pivot_x,pivot_y}); }
    __declspec(dllexport) void igSetNextWindowSize(float x, float y, int cond)
        { ImGui::SetNextWindowSize({x,y}, (ImGuiCond)cond); }
    __declspec(dllexport) void igSetNextWindowBgAlpha(float alpha)
        { ImGui::SetNextWindowBgAlpha(alpha); }
    __declspec(dllexport) void igSpacing() { ImGui::Spacing(); }
    __declspec(dllexport) void igSeparator() { ImGui::Separator(); }

    // Widgets
    __declspec(dllexport) void igText(const char* fmt) { ImGui::TextUnformatted(fmt); }
    __declspec(dllexport) bool igCheckbox(const char* label, bool* v) { return ImGui::Checkbox(label, v); }
    __declspec(dllexport) void igProgressBar(float fraction, float size_x, float size_y, const char* overlay)
        { ImGui::ProgressBar(fraction, {size_x,size_y}, overlay); }

    // Style
    __declspec(dllexport) void igPushStyleColor(int idx, float r, float g, float b, float a)
        { ImGui::PushStyleColor((ImGuiCol)idx, {r,g,b,a}); }
    __declspec(dllexport) void igPopStyleColor(int count) { ImGui::PopStyleColor(count); }

    // IO
    __declspec(dllexport) float igGetFramerate() { return ImGui::GetIO().Framerate; }
    __declspec(dllexport) const char* igGetVersion() { return ImGui::GetVersion(); }
}
