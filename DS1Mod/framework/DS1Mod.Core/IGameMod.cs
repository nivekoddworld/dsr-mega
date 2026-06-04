namespace DS1Mod.Core;

public interface IGameMod
{
    string Name    { get; }
    string Version { get; }
    string Author  { get; }

    void OnLoad  (IModContext ctx);
    void OnUnload();
    void OnTick  ();
}
