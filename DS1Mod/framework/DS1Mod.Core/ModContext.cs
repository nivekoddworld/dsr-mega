namespace DS1Mod.Core;

public sealed class ModContext : IModContext
{
    public IGameHooks  Hooks   { get; }
    public IGameReader Reader  { get; }
    public IGameWriter Writer  { get; }
    public string      ModsDir { get; }

    public ModContext(GameHooks hooks, GameReader reader, GameWriter writer, string modsDir)
    {
        Hooks   = hooks;
        Reader  = reader;
        Writer  = writer;
        ModsDir = modsDir;
    }
}
