using DS1Mod.Core.Data;

namespace DS1Mod.Core.Hooks;

internal sealed class BossHook
{
    private readonly HashSet<int> _killed = new();

    public event Action<BossKill>? BossKilled;

    public void Poll()
    {
        foreach (var (flagId, name) in BossData.All)
        {
            if (_killed.Contains(flagId)) continue;
            if (!EventFlags.Get(flagId)) continue;
            _killed.Add(flagId);
            BossKilled?.Invoke(new BossKill(name, flagId, DateTime.UtcNow));
        }
    }
}
