using System.Reflection;
using System.Runtime.Loader;

namespace DS1Mod.Host;

/// <summary>
/// Per-mod isolated load context. DS1Mod.Core and DS1Mod.SDK are shared with
/// the host — every mod must see the *same* type instances so the singleton
/// hooks, events, and memory helpers line up.
///
/// We hand these back from the *host's* load context, not AssemblyLoadContext
/// .Default. hostfxr loads DS1Mod.Host (and its dependency DS1Mod.Core) into an
/// isolated component context, leaving Default empty — so deferring shared
/// assemblies to Default finds nothing, and any copy loaded into Default can't
/// resolve its own dependencies (which live in the host context). Resolving
/// through the host context keeps one consistent set of types.
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

    // The context that loaded the host (and DS1Mod.Core). Under hostfxr this is
    // an isolated component context, NOT Default; shared assemblies must come
    // from here so the mod links against the same types the host uses.
    private static readonly AssemblyLoadContext HostContext =
        GetLoadContext(typeof(ModAssemblyLoadContext).Assembly) ?? Default;

    // Absolute path to the host's own directory, where the shared assemblies are
    // deployed. Derived from this assembly's location rather than
    // AppContext.BaseDirectory, which is empty under hostfxr component activation
    // (passing a relative path to LoadFromAssemblyPath throws ArgumentException).
    private static readonly string HostDir =
        Path.GetDirectoryName(typeof(ModAssemblyLoadContext).Assembly.Location) ?? "";

    /// <summary>
    /// Returns the host's copy of a shared assembly so the mod links against the
    /// exact same types the host uses.
    /// </summary>
    private static Assembly? LoadShared(AssemblyName assemblyName)
    {
        string? name = assemblyName.Name;

        // 1. Already loaded in the host's context? Reuse that instance. This is
        //    the normal path for DS1Mod.Core, which the host actively uses.
        foreach (Assembly asm in HostContext.Assemblies)
            if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                return asm;

        // 2. Not loaded yet (DS1Mod.SDK — the host references it but never uses a
        //    type from it). Resolve it through the host's context so it, and its
        //    dependencies, load alongside the host with one shared identity.
        try
        {
            Assembly? viaName = HostContext.LoadFromAssemblyName(assemblyName);
            if (viaName is not null) return viaName;
        }
        catch (FileNotFoundException) { /* fall through to direct path load */ }

        // 3. Last resort: load by absolute path from the host directory into the
        //    host's context.
        if (name is not null && HostDir.Length > 0)
        {
            string hostDll = Path.Combine(HostDir, name + ".dll");
            if (File.Exists(hostDll) && Path.IsPathRooted(hostDll))
                return HostContext.LoadFromAssemblyPath(hostDll);
        }

        return null;
    }
}
