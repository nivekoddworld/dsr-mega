using System.Reflection;
using DS1Mod.Core;
using DS1Mod.Core.Memory;

namespace DS1Mod.Host;

internal sealed class ModLifecycleManager : IDisposable
{
    private sealed record LoadedMod(
        IGameMod Mod,
        ModAssemblyLoadContext Alc);

    private readonly List<LoadedMod> _mods    = new();
    private readonly GameHooks       _hooks   = new();
    private readonly GameReader      _reader  = new();
    private readonly GameWriter      _writer  = new();
    private          EventPump?      _pump;

    public void LoadMods(string modsDir)
    {
        if (!Directory.Exists(modsDir)) return;

        var ctx = new ModContext(_hooks, _reader, _writer, modsDir);

        foreach (string dll in Directory.EnumerateFiles(modsDir, "*.dll"))
        {
            try
            {
                LoadMod(dll, ctx);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DS1Mod.Host] Failed to load {Path.GetFileName(dll)}: {ex.Message}");
            }
        }

        _pump = new EventPump(_hooks, _mods.Select(m => m.Mod).ToList());
    }

    private void LoadMod(string dllPath, ModContext ctx)
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

        if (mod is null)
        {
            alc.Unload();
            return;
        }

        mod.OnLoad(ctx);
        _mods.Add(new LoadedMod(mod, alc));
        Console.WriteLine($"[DS1Mod.Host] Loaded: {mod.Name} v{mod.Version} by {mod.Author}");
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
