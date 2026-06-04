using System.Reflection;
using System.Runtime.Loader;

namespace DS1Mod.Host;

/// <summary>
/// Per-mod isolated load context. DS1Mod.Core and DS1Mod.SDK are shared with
/// the host — every mod must see the *same* type instances so the singleton
/// hooks, events, and memory helpers line up. We hand back the host's copy
/// rather than returning null: the default context only contains an assembly
/// once something has actually loaded it, and the host never references a
/// DS1Mod.SDK type, so SDK would otherwise be missing → FileNotFoundException.
/// </summary>
internal sealed class ModAssemblyLoadContext : AssemblyLoadContext
{
    private static readonly HashSet<string> SharedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "DS1Mod.Core",
        "DS1Mod.SDK",
    };

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _modDir;

    public ModAssemblyLoadContext(string dllPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(dllPath);
        _modDir   = Path.GetDirectoryName(dllPath) ?? AppContext.BaseDirectory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;

        if (name is not null && SharedAssemblies.Contains(name))
            return LoadShared(assemblyName);

        // Preferred: resolve via the mod's .deps.json (handles NuGet deps).
        string? resolved = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolved is not null)
            return LoadFromAssemblyPath(resolved);

        // Fallback: probe the mod's own folder for a sibling DLL. Lets a mod
        // ship its dependencies next to it even when no .deps.json was deployed.
        if (name is not null)
        {
            string sibling = Path.Combine(_modDir, name + ".dll");
            if (File.Exists(sibling))
                return LoadFromAssemblyPath(sibling);
        }

        return null; // fall through to the default context
    }

    /// <summary>
    /// Returns the host's copy of a shared assembly so the mod links against the
    /// exact same types the host uses.
    /// </summary>
    private static Assembly? LoadShared(AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;

        // 1. Already loaded in the default (host) context? Reuse that instance.
        //    This is the normal path for DS1Mod.Core, which the host loads.
        foreach (Assembly asm in Default.Assemblies)
            if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                return asm;

        // 2. Not loaded yet (DS1Mod.SDK — the host references it but never uses a
        //    type from it, so it was never loaded). Load it from the host's own
        //    directory *into the default context* so it's shared with every mod.
        if (name is not null)
        {
            string hostDll = Path.Combine(AppContext.BaseDirectory, name + ".dll");
            if (File.Exists(hostDll))
                return Default.LoadFromAssemblyPath(hostDll);
        }

        // 3. Let the runtime attempt its normal resolution as a last resort.
        return null;
    }
}
