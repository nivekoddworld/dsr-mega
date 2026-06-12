using DS1Mod.Core.Memory;

namespace DS1Mod.Core;

/// <summary>
/// Direct read/write access to the live player character. Unlike
/// <see cref="IGameReader"/> (read-only snapshots), this exposes mutation —
/// gameplay mods (regen, rally mechanics, damage effects) write HP here.
/// All offsets include the per-version ChrIns layout boosts, which is why
/// mods must use this rather than dereferencing raw DSR-Gadget offsets.
/// </summary>
public static class PlayerBody
{
    /// <summary>Current and max HP, or (0, 0) when no player is loaded.</summary>
    public static (int Hp, int MaxHp) ReadHp()
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return (0, 0);
        DsrVersion.Ensure();
        return (GameMemory.Read<int>(chr + Offsets.Chr_HpOff    + DsrVersion.ChrData1Boost2),
                GameMemory.Read<int>(chr + Offsets.Chr_MaxHpOff + DsrVersion.ChrData1Boost2));
    }

    /// <summary>Set the player's current HP. No-op when no player is loaded.</summary>
    public static void WriteHp(int hp)
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return;
        DsrVersion.Ensure();
        GameMemory.Write(chr + Offsets.Chr_HpOff + DsrVersion.ChrData1Boost2, hp);
    }

    /// <summary>Base address of the player ChrIns, or 0. For diagnostics/probes.</summary>
    public static nint PlayerChr => GamePointers.PlayerChr;

    /// <summary>
    /// Dump a window of ChrIns fields around the HP block as both int and
    /// float, for locating undocumented fields (e.g. the HUD's trailing
    /// "recently lost HP" lag value used for the damage bar). Offsets are
    /// relative to the version-adjusted HP offset, so 0 == current HP.
    /// </summary>
    public static string ProbeHpNeighborhood(int loRel = -0x18, int hiRel = 0x28)
    {
        nint chr = GamePointers.PlayerChr;
        if (chr == 0) return "(no player)";
        DsrVersion.Ensure();
        nint hpBase = chr + Offsets.Chr_HpOff + DsrVersion.ChrData1Boost2;

        var sb = new System.Text.StringBuilder();
        for (int off = loRel; off <= hiRel; off += 4)
        {
            nint a = hpBase + off;
            if (!GameMemory.CanRead(a, 4)) continue;
            int iv = GameMemory.Read<int>(a);
            float fv = GameMemory.Read<float>(a);
            sb.Append($"[{off:+0x;-0x}]={iv}");
            if (fv > 0.0001f && fv < 1e9f) sb.Append($"/{fv:0.#}f");
            sb.Append("  ");
        }
        return sb.ToString();
    }
}
