namespace DS1Mod.Core;

/// <summary>
/// Ties together a live game process with all the trackers.
/// Call <see cref="StartPolling"/> to begin background polling,
/// then subscribe to events on <see cref="Bosses"/> / <see cref="FogGates"/>.
///
/// Usage:
///   var session = GameSession.TryCreate(seed: "abc123");
///   if (session is null) { /* game not running */ }
///   session.Bosses.BossKilled    += kill => ...;
///   session.FogGates.GateOpened  += gate => ...;
///   session.StartPolling();
/// </summary>
public sealed class GameSession : IDisposable
{
    public GameProcess       Process   { get; }
    public EventFlags        Flags     { get; }
    public BossTracker       Bosses    { get; }
    public FogGateTracker    FogGates  { get; }
    public PlayerStateReader Player    { get; }
    public PlayerStatsReader Stats     { get; }
    public ProgressLog       Log       { get; }

    private readonly List<IGameMode> _modes = new();
    private CancellationTokenSource? _cts;

    private GameSession(GameProcess proc, string seed)
    {
        Process  = proc;
        Flags    = new EventFlags(proc);
        Bosses   = new BossTracker(Flags);
        FogGates = new FogGateTracker(Flags);
        Player   = new PlayerStateReader(proc);
        Stats    = new PlayerStatsReader(proc);
        Log      = new ProgressLog(ProgressLog.DefaultPath(seed));

        Bosses.BossKilled    += Log.RecordBossKill;
        FogGates.GateOpened  += Log.RecordGateOpened;
    }

    public static GameSession? TryCreate(string seed = "")
    {
        var proc = GameProcess.TryAttach();
        return proc is null ? null : new GameSession(proc, seed);
    }

    public void AddMode(IGameMode mode)
    {
        _modes.Add(mode);
        mode.OnAttached(this);
        Bosses.BossKilled    += mode.OnBossKilled;
        FogGates.GateOpened  += mode.OnFogGateOpened;
    }

    public void StartPolling(TimeSpan? interval = null)
    {
        _cts = new CancellationTokenSource();
        var ms = (int)(interval ?? TimeSpan.FromMilliseconds(500)).TotalMilliseconds;
        var token = _cts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && Process.IsAlive)
            {
                Bosses.Poll();
                FogGates.Poll();

                var state = Player.Read();
                Log.UpdatePlayer(state);
                if (state is not null)
                    foreach (var mode in _modes)
                        mode.OnPlayerMoved(state.X, state.Y, state.Z, state.MapId);

                Log.Flush();
                await Task.Delay(ms, token).ConfigureAwait(false);
            }
        }, token);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        foreach (var mode in _modes) mode.OnDetached();
        Log.Flush();
        Process.Dispose();
    }
}
