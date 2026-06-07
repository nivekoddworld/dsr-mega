using System.Reflection;
using DS1Mod.Core;
using DS1Mod.Core.Memory;
using DS1Mod.Modding;
using DS1Mod.Rendering;
using SoulsFormats;

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
    private          ImGuiRenderer?  _renderer;

    // Host/SDK assemblies that may end up beside mods but are never themselves
    // mods — skip them so we don't spin up a load context for each.
    private static readonly HashSet<string> FrameworkDlls = new(StringComparer.OrdinalIgnoreCase)
    {
        "DS1Mod.Core.dll", "DS1Mod.SDK.dll", "DS1Mod.Host.dll", "DS1Mod.Rendering.dll",
    };

    public void LoadMods(string gameDir, string modsDir)
    {
        // Set console to UTF-8 to properly display em dashes and other Unicode characters
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine($"[DS1Mod.Host] Game dir: {gameDir}");
        Console.WriteLine($"[DS1Mod.Host] Mods dir: {modsDir}");

        if (!Directory.Exists(modsDir))
        {
            Console.WriteLine(
                $"[DS1Mod.Host] Mods directory does not exist — nothing to load. " +
                $"Create '{modsDir}' and put mod DLLs in it.");
            return;
        }

        var dllPaths = Directory.EnumerateFiles(modsDir, "*.dll")
            .Where(p => !FrameworkDlls.Contains(Path.GetFileName(p)))
            .ToList();

        Console.WriteLine($"[DS1Mod.Host] Found {dllPaths.Count} candidate DLL(s) in mods dir.");

        // ── Phase 1: load assemblies, instantiate mods ────────────────
        var modCtx = new ModContext(_hooks, _reader, _writer, modsDir);
        var patchCtx = new PatchContext(gameDir, modsDir);

        foreach (string dll in dllPaths)
        {
            try { InstantiateMod(dll); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host] Failed to instantiate {Path.GetFileName(dll)}: {ex}");
            }
        }

        Console.WriteLine($"[DS1Mod.Host] Instantiated {_mods.Count} mod(s).");
        if (_mods.Count == 0)
        {
            Console.WriteLine(
                "[DS1Mod.Host] No mods loaded. Check that your mod class is public, " +
                "non-abstract, has a parameterless constructor, implements IGameMod " +
                "(e.g. via ModBase), and references the deployed DS1Mod.SDK.");
        }

        // ── Phase 2: Load bonfire ESD + run patchers ──────────────────
        LoadSharedBonfireEsd(patchCtx, gameDir);

        foreach (var (mod, _) in _mods)
        {
            if (mod is not IGamePatcher patcher) continue;
            try
            {
                Console.WriteLine($"[DS1Mod.Host] Patching: {mod.Name}");
                patchCtx.SetCurrentMod(mod.Name);
                patcher.Patch(patchCtx);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host] Patch failed for {mod.Name}: {ex.Message}");
            }
        }

        WriteSharedBonfireEsd(patchCtx, gameDir);

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
        Console.WriteLine("[DS1Mod.Host] Event pump started — listening for game events.");

        // ── Phase 4: ImGui overlay ────────────────────────────────────
        var guiMods = _mods
            .Select(m => m.Mod)
            .OfType<IGuiMod>()
            .ToList();

        if (guiMods.Count > 0)
        {
            _renderer = new ImGuiRenderer(guiMods);
            Console.WriteLine($"[DS1Mod.Host] ImGui renderer started ({guiMods.Count} overlay mod(s)).");
        }
    }

    private void InstantiateMod(string dllPath)
    {
        string file = Path.GetFileName(dllPath);
        Console.WriteLine($"[DS1Mod.Host] Inspecting {file}...");

        var alc = new ModAssemblyLoadContext(dllPath);

        Assembly asm;
        try
        {
            asm = alc.LoadFromAssemblyPath(dllPath);
        }
        catch (Exception ex)
        {
            // BadImageFormatException = native / non-.NET DLL; that's fine to skip.
            Console.Error.WriteLine($"[DS1Mod.Host]   {file}: could not load as a .NET assembly: {ex.Message}");
            alc.Unload();
            return;
        }

        Type[] types;
        try
        {
            types = asm.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // A dependency of one of the mod's public types failed to load. The
            // LoaderExceptions are the actual reason — surface them so the author
            // knows what's missing (usually an un-deployed dependency DLL).
            Console.Error.WriteLine($"[DS1Mod.Host]   {file}: some types failed to load:");
            foreach (var le in ex.LoaderExceptions)
                if (le is not null) Console.Error.WriteLine($"[DS1Mod.Host]     - {le.Message}");
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        IGameMod? mod = null;
        foreach (var type in types)
        {
            if (type.IsAbstract || !typeof(IGameMod).IsAssignableFrom(type)) continue;
            try
            {
                mod = (IGameMod)Activator.CreateInstance(type)!;
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DS1Mod.Host]   {file}: '{type.FullName}' implements IGameMod but could not be " +
                    $"constructed (needs a public parameterless constructor): {ex.Message}");
            }
        }

        if (mod is null)
        {
            // Silently skip — this is normal for dependency DLLs (e.g. DiscordRPC.dll)
            // that get copied next to the mod but are not mods themselves.
            alc.Unload();
            return;
        }

        _mods.Add(new LoadedMod(mod, alc));
        Console.WriteLine($"[DS1Mod.Host]   {file}: found mod '{mod.Name}'.");
    }

    private void LoadSharedBonfireEsd(PatchContext patchCtx, string gameDir)
    {
        try
        {
            string talkDir = Path.Combine(gameDir, @"script\talk");
            string[] bndPaths = Directory.GetFiles(talkDir, "*.talkesdbnd.dcx");

            // Detect the bonfire ESD size: all 74 bonfire scripts are byte-for-byte
            // identical, so they share a size that appears far more often than any
            // other entry size. Works on both vanilla (23012) and already-patched files.
            var sizeCounts = new Dictionary<int, int>();
            foreach (string p in bndPaths)
            {
                BND3 b = BND3.Read(DCX.Decompress(p, out _));
                foreach (var f in b.Files)
                    sizeCounts[f.Bytes.Length] = sizeCounts.GetValueOrDefault(f.Bytes.Length) + 1;
            }
            if (sizeCounts.Count == 0) { Console.WriteLine("[DS1Mod.Host] Warning: no talkesdbnd entries found."); return; }

            var (bonfireSize, count) = sizeCounts.OrderByDescending(kv => kv.Value).First();
            if (count < 5) { Console.WriteLine("[DS1Mod.Host] Warning: bonfire ESD not found — bonfire modifications skipped."); return; }

            patchCtx.BonfireEsdSize = bonfireSize;

            // Load one instance, strip any custom slots from a previous run, hand to mods.
            foreach (string p in bndPaths)
            {
                BND3 b = BND3.Read(DCX.Decompress(p, out _));
                var entry = b.Files.FirstOrDefault(f => f.Bytes.Length == bonfireSize);
                if (entry == null) continue;

                var editor = new EsdEditor(ESD.Read(entry.Bytes));
                editor.ResetCustomBonfireSlots();
                patchCtx.BonfireEsd = editor;
                Console.WriteLine($"[DS1Mod.Host] Loaded bonfire ESD template ({bonfireSize} bytes, {count} instances)");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DS1Mod.Host] Error loading bonfire ESD: {ex.Message}");
        }
    }

    private void WriteSharedBonfireEsd(PatchContext patchCtx, string gameDir)
    {
        if (patchCtx.BonfireEsd == null) return;

        try
        {
            string talkDir = Path.Combine(gameDir, @"script\talk");
            byte[] modifiedEsdBytes = patchCtx.BonfireEsd.Esd.Write();
            int bonfireSize = patchCtx.BonfireEsdSize;

            int filesModified = 0;
            foreach (string bndPath in Directory.GetFiles(talkDir, "*.talkesdbnd.dcx"))
            {
                byte[] dec = DCX.Decompress(bndPath, out DCX.Type type);
                BND3 bnd = BND3.Read(dec);

                bool changed = false;
                foreach (BinderFile file in bnd.Files)
                {
                    if (file.Bytes.Length != bonfireSize) continue;
                    file.Bytes = modifiedEsdBytes;
                    changed = true;
                }

                if (changed)
                {
                    File.WriteAllBytes(bndPath, DCX.Compress(bnd.Write(), type));
                    filesModified++;
                }
            }

            if (filesModified > 0)
                Console.WriteLine($"[DS1Mod.Host] Wrote bonfire ESD to {filesModified} file(s)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DS1Mod.Host] Error writing bonfire ESD: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _pump?.Dispose();

        foreach (var (mod, alc) in _mods)
        {
            try { mod.OnUnload(); } catch { }
            alc.Unload();
        }

        _mods.Clear();
    }
}
