namespace DS1Mod.Core;

public interface IModContext
{
    IGameHooks  Hooks     { get; }
    IGameReader Reader    { get; }
    IGameWriter Writer    { get; }
    string      ModsDir   { get; }
}
