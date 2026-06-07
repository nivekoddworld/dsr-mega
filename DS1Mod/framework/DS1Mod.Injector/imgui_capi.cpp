// imgui_capi.cpp
// C-ABI exports of ImGui functions from dinput8.dll.
// Managed code P/Invokes these via DS1ImGui.cs (targeting "dinput8").

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include "imgui/imgui.h"
#include <cstdint>

extern "C"
{
    // Context
    __declspec(dllexport) void* igGetCurrentContext() { return ImGui::GetCurrentContext(); }

    // Windows
    __declspec(dllexport) uint8_t igBegin(const char* name, uint8_t* p_open, int flags)
    {
        bool open = p_open ? (*p_open != 0) : true;
        bool visible = ImGui::Begin(name, p_open ? &open : nullptr, (ImGuiWindowFlags)flags);
        if (p_open) *p_open = open ? 1 : 0;
        return visible ? 1 : 0;
    }
    __declspec(dllexport) uint8_t igBeginNoClose(const char* name, void* /*unused*/, int flags)
        { return ImGui::Begin(name, nullptr, (ImGuiWindowFlags)flags) ? 1 : 0; }
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
    __declspec(dllexport) uint8_t igCheckbox(const char* label, uint8_t* v)
    {
        bool b = *v != 0;
        bool changed = ImGui::Checkbox(label, &b);
        *v = b ? 1 : 0;
        return changed ? 1 : 0;
    }
    __declspec(dllexport) void igProgressBar(float fraction, float size_x, float size_y, const char* overlay)
        { ImGui::ProgressBar(fraction, {size_x,size_y}, overlay); }
    __declspec(dllexport) uint8_t igButton(const char* label, float size_x, float size_y)
        { return ImGui::Button(label, {size_x,size_y}) ? 1 : 0; }

    // Style
    __declspec(dllexport) void igPushStyleColor(int idx, float r, float g, float b, float a)
        { ImGui::PushStyleColor((ImGuiCol)idx, {r,g,b,a}); }
    __declspec(dllexport) void igPopStyleColor(int count) { ImGui::PopStyleColor(count); }

    // Trees / headers
    __declspec(dllexport) uint8_t igCollapsingHeader(const char* label, int flags)
        { return ImGui::CollapsingHeader(label, (ImGuiTreeNodeFlags)flags) ? 1 : 0; }

    // Layout helpers
    __declspec(dllexport) void igSameLine(float offset_from_start_x, float spacing)
        { ImGui::SameLine(offset_from_start_x, spacing); }
    __declspec(dllexport) void igTextDisabled(const char* text)
        { ImGui::TextDisabled("%s", text); }

    // Tabs
    __declspec(dllexport) uint8_t igBeginTabBar(const char* str_id, int flags)
        { return ImGui::BeginTabBar(str_id, (ImGuiTabBarFlags)flags) ? 1 : 0; }
    __declspec(dllexport) void igEndTabBar()
        { ImGui::EndTabBar(); }
    __declspec(dllexport) uint8_t igBeginTabItem(const char* label, uint8_t* p_open, int flags)
    {
        bool open = p_open ? (*p_open != 0) : true;
        bool visible = ImGui::BeginTabItem(label, p_open ? &open : nullptr, (ImGuiTabItemFlags)flags);
        if (p_open) *p_open = open ? 1 : 0;
        return visible ? 1 : 0;
    }
    __declspec(dllexport) void igEndTabItem()
        { ImGui::EndTabItem(); }

    // Sliders
    __declspec(dllexport) uint8_t igSliderInt(const char* label, int* v, int v_min, int v_max, const char* fmt)
        { return ImGui::SliderInt(label, v, v_min, v_max, fmt) ? 1 : 0; }
    __declspec(dllexport) uint8_t igSliderFloat(const char* label, float* v, float v_min, float v_max, const char* fmt)
        { return ImGui::SliderFloat(label, v, v_min, v_max, fmt) ? 1 : 0; }

    // Input widgets
    __declspec(dllexport) uint8_t igInputInt(const char* label, int* v, int step, int step_fast, int flags)
        { return ImGui::InputInt(label, v, step, step_fast, (ImGuiInputTextFlags)flags) ? 1 : 0; }
    __declspec(dllexport) uint8_t igInputFloat(const char* label, float* v, float step, float step_fast, const char* fmt, int flags)
        { return ImGui::InputFloat(label, v, step, step_fast, fmt, (ImGuiInputTextFlags)flags) ? 1 : 0; }
    __declspec(dllexport) uint8_t igInputText(const char* label, char* buf, size_t buf_size, int flags)
        { return ImGui::InputText(label, buf, buf_size, (ImGuiInputTextFlags)flags) ? 1 : 0; }
    __declspec(dllexport) void igSetNextItemWidth(float item_width)
        { ImGui::SetNextItemWidth(item_width); }

    // Child windows (scrollable regions)
    __declspec(dllexport) uint8_t igBeginChild(const char* str_id, float w, float h, uint8_t border, int flags)
        { return ImGui::BeginChild(str_id, {w,h}, border != 0, (ImGuiWindowFlags)flags) ? 1 : 0; }
    __declspec(dllexport) void igEndChild() { ImGui::EndChild(); }

    // Combo box
    __declspec(dllexport) uint8_t igBeginCombo(const char* label, const char* preview, int flags)
        { return ImGui::BeginCombo(label, preview, (ImGuiComboFlags)flags) ? 1 : 0; }
    __declspec(dllexport) void igEndCombo() { ImGui::EndCombo(); }

    // Selectable
    __declspec(dllexport) uint8_t igSelectable(const char* label, uint8_t selected, int flags, float w, float h)
        { return ImGui::Selectable(label, selected != 0, (ImGuiSelectableFlags)flags, {w,h}) ? 1 : 0; }
    __declspec(dllexport) void igSetItemDefaultFocus() { ImGui::SetItemDefaultFocus(); }

    // IO
    __declspec(dllexport) float igGetFramerate() { return ImGui::GetIO().Framerate; }
    __declspec(dllexport) const char* igGetVersion() { return ImGui::GetVersion(); }
}
