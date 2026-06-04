namespace DS1Mod.Core;

public sealed class EventPump : IDisposable
{
    private readonly GameHooks              _hooks;
    private readonly IReadOnlyList<IGameMod> _mods;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task                    _task;

    public EventPump(GameHooks hooks, IReadOnlyList<IGameMod> mods, int intervalMs = 500)
    {
        _hooks = hooks;
        _mods  = mods;
        _task  = Task.Run(() => RunLoop(intervalMs, _cts.Token));
    }

    private async Task RunLoop(int intervalMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _hooks.PollAll();
                foreach (var mod in _mods)
                {
                    try { mod.OnTick(); }
                    catch { /* isolate per-mod exceptions */ }
                }
            }
            catch { /* guard against hook exceptions */ }

            await Task.Delay(intervalMs, ct).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _task.Wait(2000); } catch { }
        _cts.Dispose();
    }
}
