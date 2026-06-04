using DS1Mod.Core;
using DS1Mod.SDK;
using ImGuiNET;
using System.Numerics;

namespace DS1Mod.ImGuiDemo;

/// <summary>
/// Minimal overlay mod that exercises the IGuiMod surface.
/// Shows a stats panel in the top-left and a collapsible debug window.
/// </summary>
public sealed class ImGuiDemoMod : ModBase, IGuiMod
{
    public override string Name    => "ImGui Demo";
    public override string Version => "1.0.0";
    public override string Author  => "DS1MegaRando";

    private IModContext? _ctx;

    // Toggled by ImGui checkbox
    private bool _showDebug = true;
    private bool _showStats = true;

    // Cached stats updated on OnTick (game thread), read on render thread.
    // Using volatile int fields to avoid locks on primitive values.
    private volatile int   _hp      = 0;
    private volatile int   _maxHp   = 0;
    private volatile float _x       = 0f;
    private volatile float _y       = 0f;
    private volatile float _z       = 0f;
    private volatile int   _souls   = 0;
    private volatile int   _level   = 0;
    private          string _mapId  = "";

    // ── IGameMod ──────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        Console.WriteLine("[ImGuiDemo] Overlay mod loaded — window will appear in-game.");
    }

    public override void OnTick()
    {
        if (_ctx is null) return;

        var stats = _ctx.Reader.GetPlayerStats();
        if (stats is not null)
        {
            _hp    = stats.CurrentHp;
            _maxHp = stats.MaxHp;
        }

        var state = _ctx.Reader.GetPlayerState();
        if (state is not null)
        {
            _x     = state.X;
            _y     = state.Y;
            _z     = state.Z;
            _mapId = state.MapId ?? "";
        }

        _souls = _ctx.Reader.GetSouls();
        _level = _ctx.Reader.GetSoulLevel();
    }

    // ── IGuiMod ───────────────────────────────────────────────────────────────

    public void OnGui()
    {
        DrawStatsPanel();
        DrawDebugWindow();
    }

    // ── Overlay windows ───────────────────────────────────────────────────────

    private void DrawStatsPanel()
    {
        if (!_showStats) return;

        // Pin to top-left, no title bar, no resize.
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(220, 0), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.65f);

        var flags = ImGuiWindowFlags.NoTitleBar
                  | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.AlwaysAutoResize;

        if (!ImGui.Begin("##StatsPanel", flags))
        {
            ImGui.End();
            return;
        }

        // HP bar
        float hpFrac = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.75f, 0.1f, 0.1f, 1f));
        ImGui.ProgressBar(hpFrac, new Vector2(-1, 0), $"HP  {_hp} / {_maxHp}");
        ImGui.PopStyleColor();

        ImGui.Spacing();
        ImGui.Text($"SL {_level}   |   {_souls:N0} souls");
        ImGui.Text($"Map: {(_mapId.Length > 0 ? _mapId : "—")}");
        ImGui.Text($"Pos: ({_x:F1}, {_y:F1}, {_z:F1})");

        ImGui.End();
    }

    private void DrawDebugWindow()
    {
        if (!_showDebug) return;

        ImGui.SetNextWindowPos(new Vector2(10, 130), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(240, 140), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowBgAlpha(0.75f);

        if (!ImGui.Begin("DS1Mod Debug", ref _showDebug))
        {
            ImGui.End();
            return;
        }

        ImGui.Checkbox("Stats panel", ref _showStats);
        ImGui.Separator();

        ImGui.Text($"ImGui.NET {ImGui.GetVersion()}");
        ImGui.Text($"Frame rate: {ImGui.GetIO().Framerate:F1} fps");

        ImGui.End();
    }
}
