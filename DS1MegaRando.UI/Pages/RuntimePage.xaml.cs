using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DS1Mod.Core;
using DS1MegaRando.UI.ViewModels;

namespace DS1MegaRando.UI.Pages;

public partial class RuntimePage : UserControl
{
    private GameSession?    _session;
    private DispatcherTimer? _liveTimer;

    private readonly ObservableCollection<BossViewModel>    _bosses = new();
    private readonly ObservableCollection<FogGateViewModel> _gates  = new();

    private static readonly SolidColorBrush GoldBrush = new(Color.FromRgb(0xC8, 0xA4, 0x5A));
    private static readonly SolidColorBrush DimBrush  = new(Color.FromRgb(0x4A, 0x3C, 0x20));

    public RuntimePage()
    {
        InitializeComponent();

        // Populate boss list in canonical order (same order as BossKillFlags.All)
        foreach (var (flagId, name) in BossTracker.KnownBosses)
            _bosses.Add(new BossViewModel(name, flagId));

        BossList.ItemsSource = _bosses;
        GateList.ItemsSource = _gates;
    }

    // ── Attach / Detach ───────────────────────────────────────────────────

    private void AttachDetach_Click(object sender, RoutedEventArgs e)
    {
        if (_session is not null) { Detach(); return; }
        Attach();
    }

    private void Attach()
    {
        string seed = DataContext is MainViewModel vm
            ? vm.MegaSettings.Global.Seed.ToString("X8")
            : "";

        _session = GameSession.TryCreate(seed);
        if (_session is null)
        {
            StatusText.Text = "OFFLINE — Dark Souls Remastered not found";
            return;
        }

        // Subscribe to events (fired from background thread → dispatch to UI)
        _session.Bosses.BossKilled   += OnBossKilled;
        _session.FogGates.GateOpened += OnGateOpened;
        _session.StartPolling();

        // Sync any bosses that were already killed before we attached
        foreach (var boss in _bosses)
        {
            if (_session.Flags.Get(boss.FlagId))
                boss.MarkKilled(DateTime.UtcNow);
        }
        RefreshBossTitle();

        // Live update timer for HP / position (reads directly, not via events)
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _liveTimer.Tick += LiveTick;
        _liveTimer.Start();

        LogPathText.Text = _session.Log.OutputPath;
        SetConnectedState(true);
    }

    private void Detach()
    {
        _liveTimer?.Stop();
        _liveTimer = null;

        _session?.Dispose();
        _session = null;

        SetConnectedState(false);
        ClearLiveReadings();
    }

    // ── Live tick (HP + position) ─────────────────────────────────────────

    private void LiveTick(object? sender, EventArgs e)
    {
        if (_session is null || !_session.Process.IsAlive) { Detach(); return; }

        var stats = _session.Stats.Read();
        if (stats is not null)
        {
            HpBar.Maximum    = stats.MaxHp;
            HpBar.Value      = stats.CurrentHp;
            HpText.Text      = $"{stats.CurrentHp} / {stats.MaxHp}";

            StaminaBar.Maximum = stats.MaxStamina;
            StaminaBar.Value   = stats.CurrentStamina;
            StaminaText.Text   = $"{stats.CurrentStamina:F0}";
        }

        var pos = _session.Player.Read();
        if (pos is not null)
        {
            AreaText.Text = $"Area: {pos.MapId}";
            PosText.Text  = $"X {pos.X,8:F1}   Y {pos.Y,8:F1}   Z {pos.Z,8:F1}";
        }
    }

    // ── Event handlers (called from background thread) ────────────────────

    private void OnBossKilled(BossKill kill) =>
        Dispatcher.Invoke(() =>
        {
            var vm = _bosses.FirstOrDefault(b => b.FlagId == kill.FlagId);
            vm?.MarkKilled(kill.KilledAt);
            RefreshBossTitle();
        });

    private void OnGateOpened(FogGate gate) =>
        Dispatcher.Invoke(() =>
        {
            _gates.Add(new FogGateViewModel(gate));
            GateEmptyHint.Visibility = Visibility.Collapsed;
            int n = _gates.Count;
            GateTitle.Text = $"FOG GATES — {n} opened";
        });

    // ── UI state helpers ──────────────────────────────────────────────────

    private void SetConnectedState(bool connected)
    {
        StatusDot.Fill  = connected
            ? new SolidColorBrush(Color.FromRgb(0x60, 0xA0, 0x60))
            : new SolidColorBrush(Color.FromRgb(0xC8, 0x50, 0x50));
        StatusText.Text = connected
            ? "CONNECTED — Dark Souls Remastered"
            : "OFFLINE — Dark Souls Remastered not found";
        AttachBtn.Content   = connected ? "DETACH" : "ATTACH";
        PlayerPanel.Opacity = connected ? 1.0 : 0.35;

        if (!connected) LogPathText.Text = "—";
    }

    private void ClearLiveReadings()
    {
        HpBar.Value      = 0;
        HpText.Text      = "— / —";
        StaminaBar.Value = 0;
        StaminaText.Text = "—";
        AreaText.Text    = "Area: —";
        PosText.Text     = "Position: —";
    }

    private void RefreshBossTitle()
    {
        int killed = _bosses.Count(b => b.IsKilled);
        BossTitle.Text = $"BOSS TRACKER — {killed} / {_bosses.Count} killed";
    }

    // ── Toolbar ───────────────────────────────────────────────────────────

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        string folder = _session is not null
            ? Path.GetDirectoryName(_session.Log.OutputPath) ?? ""
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DS1Mod");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }
}

// ── View models ───────────────────────────────────────────────────────────

public sealed class BossViewModel : INotifyPropertyChanged
{
    public string Name   { get; }
    public int    FlagId { get; }

    private bool      _isKilled;
    private DateTime? _killedAt;

    public bool IsKilled => _isKilled;

    public string Indicator     => _isKilled ? "✓" : "○";
    public double NameOpacity   => _isKilled ? 1.0 : 0.45;
    public string KilledAtText  => _killedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—";

    public Brush IndicatorBrush => _isKilled
        ? new SolidColorBrush(Color.FromRgb(0xC8, 0xA4, 0x5A))   // gold
        : new SolidColorBrush(Color.FromRgb(0x4A, 0x3C, 0x20));  // dim

    public event PropertyChangedEventHandler? PropertyChanged;

    public BossViewModel(string name, int flagId)
    {
        Name   = name;
        FlagId = flagId;
    }

    public void MarkKilled(DateTime killedAt)
    {
        _isKilled = true;
        _killedAt = killedAt;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsKilled)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Indicator)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameOpacity)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KilledAtText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IndicatorBrush)));
    }
}

public sealed class FogGateViewModel
{
    public string Name  { get; }
    public string MapId { get; }

    public FogGateViewModel(FogGate gate)
    {
        Name  = gate.Name;
        MapId = gate.MapId;
    }
}
