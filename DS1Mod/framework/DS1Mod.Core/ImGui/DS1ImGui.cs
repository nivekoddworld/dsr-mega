using System.Runtime.InteropServices;

namespace DS1Mod.Core.ImGui;

/// <summary>
/// P/Invoke wrappers for ImGui functions exported from dinput8.dll.
/// Use these instead of ImGui.NET — no separate cimgui.dll required.
/// </summary>
public static partial class DS1ImGui
{
    private const string Dll = "dinput8";

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBegin(string name, ref byte p_open, ImGuiWindowFlags flags);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBeginNoClose(string name, nint p_open_null, ImGuiWindowFlags flags);

    public static bool Begin(string name, ImGuiWindowFlags flags = 0)
        => igBeginNoClose(name, nint.Zero, flags) != 0;

    public static bool Begin(string name, ref bool p_open, ImGuiWindowFlags flags = 0)
    {
        byte b = p_open ? (byte)1 : (byte)0;
        bool visible = igBegin(name, ref b, flags) != 0;
        p_open = b != 0;
        return visible;
    }

    [LibraryImport(Dll)] public static partial void igEnd();
    public static void End() => igEnd();

    [LibraryImport(Dll)] public static partial void igSetNextWindowPos(float x, float y, ImGuiCond cond, float pivot_x, float pivot_y);
    public static void SetNextWindowPos(float x, float y, ImGuiCond cond = ImGuiCond.None) => igSetNextWindowPos(x, y, cond, 0, 0);

    [LibraryImport(Dll)] public static partial void igSetNextWindowSize(float x, float y, ImGuiCond cond);
    public static void SetNextWindowSize(float x, float y, ImGuiCond cond = ImGuiCond.None) => igSetNextWindowSize(x, y, cond);

    [LibraryImport(Dll)] public static partial void igSetNextWindowBgAlpha(float alpha);
    public static void SetNextWindowBgAlpha(float alpha) => igSetNextWindowBgAlpha(alpha);

    [LibraryImport(Dll)] public static partial void igSpacing();
    public static void Spacing() => igSpacing();

    [LibraryImport(Dll)] public static partial void igSeparator();
    public static void Separator() => igSeparator();

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)] public static partial void igText(string text);
    public static void Text(string text) => igText(text);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igCheckbox(string label, ref byte v);
    public static bool Checkbox(string label, ref bool v)
    {
        byte b = v ? (byte)1 : (byte)0;
        bool changed = igCheckbox(label, ref b) != 0;
        v = b != 0;
        return changed;
    }

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void igProgressBar(float fraction, float size_x, float size_y, string? overlay);
    public static void ProgressBar(float fraction, float sizeX = -1f, float sizeY = 0f, string? overlay = null)
        => igProgressBar(fraction, sizeX, sizeY, overlay);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igButton(string label, float size_x, float size_y);
    public static bool Button(string label, float width = 0f, float height = 0f)
        => igButton(label, width, height) != 0;

    [LibraryImport(Dll)] public static partial void igPushStyleColor(ImGuiCol idx, float r, float g, float b, float a);
    public static void PushStyleColor(ImGuiCol idx, float r, float g, float b, float a = 1f) => igPushStyleColor(idx, r, g, b, a);

    [LibraryImport(Dll)] public static partial void igPopStyleColor(int count);
    public static void PopStyleColor(int count = 1) => igPopStyleColor(count);

    // Trees / collapsing headers
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igCollapsingHeader(string label, ImGuiTreeNodeFlags flags);
    public static bool CollapsingHeader(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => igCollapsingHeader(label, flags) != 0;

    // Layout
    [LibraryImport(Dll)] public static partial void igSameLine(float offsetFromStartX, float spacing);
    public static void SameLine(float offset = 0f, float spacing = -1f) => igSameLine(offset, spacing);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void igTextDisabled(string text);
    public static void TextDisabled(string text) => igTextDisabled(text);

    // Tabs
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBeginTabBar(string strId, ImGuiTabBarFlags flags);
    public static bool BeginTabBar(string id, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        => igBeginTabBar(id, flags) != 0;

    [LibraryImport(Dll)] public static partial void igEndTabBar();
    public static void EndTabBar() => igEndTabBar();

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBeginTabItem(string label, nint pOpenNull, ImGuiTabItemFlags flags);
    public static bool BeginTabItem(string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => igBeginTabItem(label, nint.Zero, flags) != 0;

    [LibraryImport(Dll)] public static partial void igEndTabItem();
    public static void EndTabItem() => igEndTabItem();

    // Sliders
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igSliderInt(string label, ref int v, int vMin, int vMax, string fmt);
    public static bool SliderInt(string label, ref int v, int min, int max, string fmt = "%d")
        => igSliderInt(label, ref v, min, max, fmt) != 0;

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igSliderFloat(string label, ref float v, float vMin, float vMax, string fmt);
    public static bool SliderFloat(string label, ref float v, float min, float max, string fmt = "%.2f")
        => igSliderFloat(label, ref v, min, max, fmt) != 0;

    [LibraryImport(Dll)] public static partial float igGetFramerate();
    public static float GetFramerate() => igGetFramerate();

    [LibraryImport(Dll)]
    private static partial nint igGetVersion();
    public static string GetVersion() => Marshal.PtrToStringUTF8(igGetVersion()) ?? "";

    // Input widgets
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igInputInt(string label, ref int v, int step, int stepFast, int flags);
    public static bool InputInt(string label, ref int v, int step = 1, int stepFast = 100)
        => igInputInt(label, ref v, step, stepFast, 0) != 0;

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igInputFloat(string label, ref float v, float step, float stepFast, string fmt, int flags);
    public static bool InputFloat(string label, ref float v, float step = 0f, float stepFast = 0f, string fmt = "%.3f")
        => igInputFloat(label, ref v, step, stepFast, fmt, 0) != 0;

    [LibraryImport(Dll)] public static partial void igSetNextItemWidth(float itemWidth);
    public static void SetNextItemWidth(float width) => igSetNextItemWidth(width);

    // InputText — requires unsafe byte* buffer; call site must pin or use fixed()
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static unsafe partial byte igInputText(string label, byte* buf, nuint bufSize, int flags);
    public static unsafe bool InputText(string label, byte[] buf)
    {
        fixed (byte* p = buf)
            return igInputText(label, p, (nuint)buf.Length, 0) != 0;
    }

    // Child windows (scrollable regions)
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBeginChild(string strId, float w, float h, byte border, ImGuiWindowFlags flags);
    public static bool BeginChild(string id, float w = 0f, float h = 0f, bool border = false, ImGuiWindowFlags flags = 0)
        => igBeginChild(id, w, h, border ? (byte)1 : (byte)0, flags) != 0;

    [LibraryImport(Dll)] public static partial void igEndChild();
    public static void EndChild() => igEndChild();

    // Combo box (BeginCombo / EndCombo pattern)
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igBeginCombo(string label, string previewValue, ImGuiComboFlags flags);
    public static bool BeginCombo(string label, string preview, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => igBeginCombo(label, preview, flags) != 0;

    [LibraryImport(Dll)] public static partial void igEndCombo();
    public static void EndCombo() => igEndCombo();

    // Selectable
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igSelectable(string label, byte selected, ImGuiSelectableFlags flags, float w, float h);
    public static bool Selectable(string label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
        => igSelectable(label, selected ? (byte)1 : (byte)0, flags, 0f, 0f) != 0;

    [LibraryImport(Dll)] public static partial void igSetItemDefaultFocus();
    public static void SetItemDefaultFocus() => igSetItemDefaultFocus();
}
