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

    public ModAssemblyLoadContext(string dllPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(dllPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null &&
            SharedAssemblies.Contains(assemblyName.Name))
            return null; // resolve from default (host) context

        string? resolved = _resolver.ResolveAssemblyToPath(assemblyName);
        return resolved is not null ? LoadFromAssemblyPath(resolved) : null;
    }
}
