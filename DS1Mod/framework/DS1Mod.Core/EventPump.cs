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
        bool wasInGame  = false;
        long cycle      = 0;
        int  diagLogged = 0;
        const int maxDiag = 20;   // ~1 min of "waiting" diagnostics, then quiet

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Don't touch game memory at the main menu, on loading screens,
                // or mid quit-out. The manager pointer chains are null or being
                // torn down in those states, so polling would read garbage or
                // fault. Skip the whole cycle until the world is actually loaded.
                bool inGame = GameState.IsInGame();

                // Diagnostics: surface the pointer chain so we can see whether
                // IsInGame() is gated correctly (offsets/reads vs. genuine menu).
                if (inGame && !wasInGame)
                    Console.WriteLine($"[EventPump] In-game — polling started. {GameState.Describe()}");
                else if (!inGame && diagLogged < maxDiag && cycle % 6 == 0)
                {
                    Console.WriteLine($"[EventPump] Not in-game (polling paused). {GameState.Describe()}");
                    diagLogged++;
                }
                wasInGame = inGame;

                if (inGame)
                {
                    _hooks.PollAll();
                    foreach (var mod in _mods)
                    {
                        try { mod.OnTick(); }
                        catch { /* isolate per-mod exceptions */ }
                    }
                }
            }
            catch { /* guard against hook exceptions */ }

            cycle++;
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
