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
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool igBegin(string name, ref bool p_open, ImGuiWindowFlags flags);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool igBeginNoClose(string name, nint p_open_null, ImGuiWindowFlags flags);

    public static bool Begin(string name, ImGuiWindowFlags flags = 0)
        => igBeginNoClose(name, nint.Zero, flags);

    public static bool Begin(string name, ref bool p_open, ImGuiWindowFlags flags = 0)
        => igBegin(name, ref p_open, flags);

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
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool igCheckbox(string label, [MarshalAs(UnmanagedType.Bool)] ref bool v);
    public static bool Checkbox(string label, ref bool v) => igCheckbox(label, ref v);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void igProgressBar(float fraction, float size_x, float size_y, string? overlay);
    public static void ProgressBar(float fraction, float sizeX = -1f, float sizeY = 0f, string? overlay = null)
        => igProgressBar(fraction, sizeX, sizeY, overlay);

    [LibraryImport(Dll)] public static partial void igPushStyleColor(ImGuiCol idx, float r, float g, float b, float a);
    public static void PushStyleColor(ImGuiCol idx, float r, float g, float b, float a = 1f) => igPushStyleColor(idx, r, g, b, a);

    [LibraryImport(Dll)] public static partial void igPopStyleColor(int count);
    public static void PopStyleColor(int count = 1) => igPopStyleColor(count);

    [LibraryImport(Dll)] public static partial float igGetFramerate();
    public static float GetFramerate() => igGetFramerate();

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf8)]
    public static partial string igGetVersion();
    public static string GetVersion() => igGetVersion();
}
