using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.SDK;

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
        DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.Always);
        DS1ImGui.SetNextWindowSize(220, 0, ImGuiCond.Always);
        DS1ImGui.SetNextWindowBgAlpha(0.65f);

        var flags = ImGuiWindowFlags.NoTitleBar
                  | ImGuiWindowFlags.NoResize
                  | ImGuiWindowFlags.NoMove
                  | ImGuiWindowFlags.NoScrollbar
                  | ImGuiWindowFlags.AlwaysAutoResize;

        if (!DS1ImGui.Begin("##StatsPanel", flags))
        {
            DS1ImGui.End();
            return;
        }

        // HP bar
        float hpFrac = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
        DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0.75f, 0.1f, 0.1f, 1f);
        DS1ImGui.ProgressBar(hpFrac, -1, 0, $"HP  {_hp} / {_maxHp}");
        DS1ImGui.PopStyleColor();

        DS1ImGui.Spacing();
        DS1ImGui.Text($"SL {_level}   |   {_souls:N0} souls");
        DS1ImGui.Text($"Map: {(_mapId.Length > 0 ? _mapId : "—")}");
        DS1ImGui.Text($"Pos: ({_x:F1}, {_y:F1}, {_z:F1})");

        DS1ImGui.End();
    }

    private void DrawDebugWindow()
    {
        if (!_showDebug) return;

        DS1ImGui.SetNextWindowPos(10, 130, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(240, 140, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.75f);

        if (!DS1ImGui.Begin("DS1Mod Debug", ref _showDebug))
        {
            DS1ImGui.End();
            return;
        }

        DS1ImGui.Checkbox("Stats panel", ref _showStats);
        DS1ImGui.Separator();

        DS1ImGui.Text($"dinput8 ImGui {DS1ImGui.GetVersion()}");
        DS1ImGui.Text($"Frame rate: {DS1ImGui.GetFramerate():F1} fps");

        DS1ImGui.End();
    }
}
