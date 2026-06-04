using System.Reflection;
using System.Runtime.Loader;

namespace DS1Mod.Host;

/// <summary>
/// Per-mod isolated load context. DS1Mod.Core and DS1Mod.SDK are shared
/// with the host (returned null → falls through to default context) so all
/// mods see the same singleton types, events, and memory helpers.
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
        if (assemblyName.Name is not null &&
            SharedAssemblies.Contains(assemblyName.Name))
            return null; // resolve from default (host) context

        // Preferred: resolve via the mod's .deps.json (handles NuGet deps).
        string? resolved = _resolver.ResolveAssemblyToPath(assemblyName);
        if (resolved is not null)
            return LoadFromAssemblyPath(resolved);

        // Fallback: probe the mod's own folder for a sibling DLL. Lets a mod
        // ship its dependencies next to it even when no .deps.json was deployed.
        if (assemblyName.Name is not null)
        {
            string sibling = Path.Combine(_modDir, assemblyName.Name + ".dll");
            if (File.Exists(sibling))
                return LoadFromAssemblyPath(sibling);
        }

        return null; // fall through to the default context
    }
}
