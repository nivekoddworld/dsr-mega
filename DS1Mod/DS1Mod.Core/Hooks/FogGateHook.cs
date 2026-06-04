using DS1Mod.Core.Data;

namespace DS1Mod.Core.Hooks;

internal sealed class FogGateHook
{
    private readonly HashSet<int> _opened = new();

    public event Action<FogGate>? FogGateEntered;

    public void Poll()
    {
        foreach (var gate in FogGateData.All)
        {
            if (_opened.Contains(gate.FlagId)) continue;
            if (!EventFlags.Get(gate.FlagId)) continue;
            _opened.Add(gate.FlagId);
            FogGateEntered?.Invoke(gate);
        }
    }
}
