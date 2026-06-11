using DS1Mod.Core;
using DS1Mod.SDK;

namespace DS1Mod.Rando;

public sealed class HpLoggerMod : ModBase, IGamePatcher
{
    public override string Name    => "Enemy Randomizer";
    public override string Version => "1.0.0";
    public override string Author  => "Chrombir";

    private IModContext? _ctx;

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
    }

    public override void OnTick()
    {

    }

    public override void OnUnload()
    {

    }

    private void Write(string message)
    {

    }

    public void Patch(IPatchContext ctx)
    {

    }
}
