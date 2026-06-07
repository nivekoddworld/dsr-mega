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

    // Button takes an ImVec2 by value. We pass it as a 2-float struct — the
    // Win64 ABI puts it in the second argument register (or XMM1) the same
    // way cimgui expects.
    [StructLayout(LayoutKind.Sequential)]
    private struct ImVec2 { public float X, Y; }

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial byte igButton(string label, ImVec2 size);
    public static bool Button(string label, float width = 0f, float height = 0f)
        => igButton(label, new ImVec2 { X = width, Y = height }) != 0;

    [LibraryImport(Dll)] public static partial void igPushStyleColor(ImGuiCol idx, float r, float g, float b, float a);
    public static void PushStyleColor(ImGuiCol idx, float r, float g, float b, float a = 1f) => igPushStyleColor(idx, r, g, b, a);

    [LibraryImport(Dll)] public static partial void igPopStyleColor(int count);
    public static void PopStyleColor(int count = 1) => igPopStyleColor(count);

    [LibraryImport(Dll)] public static partial float igGetFramerate();
    public static float GetFramerate() => igGetFramerate();

    [LibraryImport(Dll)]
    private static partial nint igGetVersion();
    public static string GetVersion() => Marshal.PtrToStringUTF8(igGetVersion()) ?? "";
}
