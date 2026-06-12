using DS1Mod.ChrumburGoofyRings.Rings;
using DS1Mod.Core;
using DS1Mod.Modding;
using DS1Mod.SDK;

namespace DS1Mod.ChrumburGoofyRings;

/// <summary>
/// Chrumbur Goofy Rings — a collection of new rings for Dark Souls Remastered.
///
/// Each ring lives in its own file under Rings/ and implements
/// <see cref="IGoofyRing"/>, owning its config options, game-file patches and
/// runtime behavior. This entrypoint only orchestrates: it forwards config /
/// patch / lifecycle calls to every registered ring and provides the shared
/// equipped-ring check.
///
/// Current collection:
///   - <see cref="HuntersRing"/> — Bloodborne rally health mechanic
/// </summary>
public sealed class ChrumburGoofyRingsMod : ModBase, IGamePatcher
{
    public override string Name    => "Chrumbur Goofy Rings";
    public override string Version => "1.1.0";
    public override string Author  => "Chrumbur";

    // Bump whenever runtime logic changes — printed on load so a stale DLL is
    // immediately obvious in the console.
    private const string BuildTag = "rings-quiet-9";

    /// <summary>The collection. Add new rings here — one line per ring.</summary>
    private readonly IGoofyRing[] _rings =
    {
        new HuntersRing(),
    };

    private readonly RingEquipDetector _equip = new();
    private long _tick;
    private long _nextScanTick;
    private string? _lastError;

    // ═════════════════════════════════════════════════════════════════════
    // Config — shared options + every ring's own section
    // ═════════════════════════════════════════════════════════════════════

    public override void InitializeConfig(ModConfig config)
    {
        config.AddBool("enabled", true)
            .Tooltip("Enable or disable Chrumbur Goofy Rings");

        config.AddBool("requireRingEquipped", true)
            .Tooltip("Ring effects only work while the ring is equipped. Disable for testing (all effects always active)");

        foreach (IGoofyRing ring in _rings)
            ring.InitializeConfig(config);
    }

    // ═════════════════════════════════════════════════════════════════════
    // IGamePatcher — let every ring define itself
    // ═════════════════════════════════════════════════════════════════════

    public void Patch(IPatchContext ctx)
    {
        var g = new GamePatch(ctx);
        byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

        foreach (IGoofyRing ring in _rings)
            ring.Patch(ctx, g, paramdefs);
    }

    // ═════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════

    public override void OnLoad(IModContext ctx)
    {
        string gameDir = Directory.GetParent(ctx.ModsDir)?.FullName ?? ctx.ModsDir;
        LoadConfigAsync(gameDir).GetAwaiter().GetResult();

        foreach (IGoofyRing ring in _rings)
            ring.OnLoad(ctx, Config!);

        Console.WriteLine($"[GoofyRings] BUILD {BuildTag} — {_rings.Length} ring(s): "
            + string.Join(", ", _rings.Select(r => $"{r.Name} (id {r.AccessoryId})"))
            + ". May the good blood guide your way.");
    }

    public override void OnUnload()
    {
        foreach (IGoofyRing ring in _rings)
            ring.OnUnload();
    }

    public override void OnTick()
    {
        // The EventPump swallows mod exceptions silently — surface them.
        try { TickCore(); }
        catch (Exception ex)
        {
            if (_lastError != ex.Message)
            {
                _lastError = ex.Message;
                Console.WriteLine($"[GoofyRings] OnTick failed: {ex}");
            }
        }
    }

    private void TickCore()
    {
        _tick++;
        bool enabled = Config?.GetBool("enabled", true) == true;
        bool requireEquip = Config?.GetBool("requireRingEquipped", true) == true;

        // Shared equip detection via the inventory heap signature (throttled
        // rescan until found). While the signature is unresolved and equip is
        // required, every ring reads as unequipped.
        bool signatureReady = _equip.HasSignature;
        if (requireEquip && !signatureReady && _tick >= _nextScanTick)
        {
            _nextScanTick = _tick + 10;   // ~5 s between scans
            if (_equip.TryFindSignature(Console.WriteLine))
            {
                Console.WriteLine("[GoofyRings] Inventory signature found — ring detection active.");
                signatureReady = true;
            }
        }

        foreach (IGoofyRing ring in _rings)
        {
            bool equipped = enabled
                && (!requireEquip || (signatureReady && _equip.IsRingEquipped((uint)ring.AccessoryId)));
            ring.OnTick(equipped);
        }
    }

    private static byte[] GetEmbeddedResource(string name)
    {
        var asm = typeof(ChrumburGoofyRingsMod).Assembly;
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource not found: {name}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
