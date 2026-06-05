namespace DS1Mod.Core;

public sealed class GameWriter : IGameWriter
{
    public void SetEventFlag(int flagId, bool value) => EventFlags.Set(flagId, value);
}
