namespace DS1Mod.Core.Hooks;

/// <summary>
/// Detects fog-wall traversal by watching the player's current animation. DS1
/// doesn't flag most fog crossings, but it always plays a "walking through fog
/// gate" animation (7410-7414), so this fires for every fog wall — not just the
/// handful of boss fogs that set event flags. Fires once per crossing, on the
/// transition into the animation.
/// </summary>
internal sealed class FogGateHook
{
    private bool _inFogAnim = false;

    public event Action<FogGate>? FogGateEntered;

    public void Poll()
    {
        int anim = GamePointers.CurrentAnimation;
        bool inFog = anim >= Offsets.FogAnimLo && anim <= Offsets.FogAnimHi;

        if (inFog && !_inFogAnim)
        {
            // We can't name the specific gate (the animation doesn't say which),
            // so report a generic fog wall and stash the animation id in FlagId.
            FogGateEntered?.Invoke(new FogGate("Fog Wall", "", anim));
        }

        _inFogAnim = inFog;
    }
}
