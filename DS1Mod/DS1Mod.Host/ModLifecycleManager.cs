using System.Reflection;
using DS1Mod.Core;
using DS1Mod.Core.Memory;

namespace DS1Mod.Host;

internal sealed class ModLifecycleManager : IDisposable
{
    private sealed record LoadedMod(
        IGameMod Mod,
        ModAssemblyLoadContext Alc);

    private readonly List<LoadedMod> _mods   = new();
    private readonly GameHooks       _hooks  = new();
    private readonly GameReader      _reader = new();
    private readonly GameWriter      _writer = new();
    private          EventPump?      _pump;

    public void LoadMods(string gameDir, string modsDir)
    {
        if (!Directory.Exists(modsDir)) return;

        // ── Phase 1: load assemblies, instantiate mods ────────────────
        var modCtx   = new ModContext(_hooks, _reader, _writer, modsDir);
        var patchCtx = new PatchContext(gameDir, modsDir);

        foreach (string dll in Directory.EnumerateFiles(modsDir, "*.dll"))
        {
            try   { InstantiateMod(dll); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host] Failed to instantiate {Path.GetFileName(dll)}: {ex.Message}");
            }
        }

        // ── Phase 2: run patchers (before any map file is loaded) ─────
        foreach (var (mod, _) in _mods)
        {
            if (mod is not IGamePatcher patcher) continue;
            try
            {
                Console.WriteLine($"[DS1Mod.Host] Patching: {mod.Name}");
                patcher.Patch(patchCtx);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host] Patch failed for {mod.Name}: {ex.Message}");
            }
        }

        // ── Phase 3: OnLoad ───────────────────────────────────────────
        foreach (var (mod, _) in _mods)
        {
            try
            {
                mod.OnLoad(modCtx);
                Console.WriteLine($"[DS1Mod.Host] Loaded: {mod.Name} v{mod.Version} by {mod.Author}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host] OnLoad failed for {mod.Name}: {ex.Message}");
            }
        }

        _pump = new EventPump(_hooks, _mods.Select(m => m.Mod).ToList());
    }

    private void InstantiateMod(string dllPath)
    {
        var alc = new ModAssemblyLoadContext(dllPath);
        var asm = alc.LoadFromAssemblyPath(dllPath);

        IGameMod? mod = null;
        foreach (var type in asm.GetExportedTypes())
        {
            if (!type.IsAbstract && typeof(IGameMod).IsAssignableFrom(type))
            {
                mod = (IGameMod)Activator.CreateInstance(type)!;
                break;
            }
        }

        if (mod is null) { alc.Unload(); return; }
        _mods.Add(new LoadedMod(mod, alc));
    }

    public void Dispose()
    {
        _pump?.Dispose();

        foreach (var (mod, alc) in _mods)
        {
            try { mod.OnUnload(); } catch { }
            alc.Unload();
        }

        _mods.Clear();
    }
}
