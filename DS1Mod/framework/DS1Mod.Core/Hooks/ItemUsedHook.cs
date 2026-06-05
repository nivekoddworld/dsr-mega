namespace DS1Mod.Core.Hooks;

internal sealed class ItemUsedHook
{
    private readonly record struct Entry(int GoodsId, int FlagId, bool WasOn);
    private readonly List<Entry> _entries = new();

    public event Action<int>? ItemUsed;

    /// <summary>
    /// Register a (goodsId, triggerFlagId) pair. The flag must be wired up via
    /// <c>GamePatch.DefineItemTrigger</c> in the patcher — it pulses ON when
    /// the corresponding item is used.
    /// </summary>
    public void Register(int goodsId, int flagId)
        => _entries.Add(new Entry(goodsId, flagId, WasOn: false));

    public void Poll()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var e    = _entries[i];
            bool isOn = EventFlags.Get(e.FlagId);
            if (isOn && !e.WasOn)
                ItemUsed?.Invoke(e.GoodsId);
            _entries[i] = e with { WasOn = isOn };
        }
    }
}
