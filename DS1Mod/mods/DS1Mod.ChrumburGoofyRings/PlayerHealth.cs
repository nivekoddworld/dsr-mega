using DS1Mod.Core;

namespace DS1Mod.ChrumburGoofyRings;

/// <summary>
/// Thin wrapper over the framework's <see cref="PlayerBody"/> API, which
/// applies the per-version ChrIns offset boosts (reading raw 0x3D8 on a
/// 1.03+ binary returns garbage — the HP block shifted by +0x10).
/// </summary>
internal static class PlayerHealth
{
    public static (int Hp, int MaxHp) Read() => PlayerBody.ReadHp();

    public static void SetHp(int hp) => PlayerBody.WriteHp(hp);
}
