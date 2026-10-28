using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// An isolated <see cref="AssemblyLoadContext"/> that uses an <see cref="AssemblyDependencyResolver"/>
/// to resolve assembly dependencies and host-architecture-compatible unmanaged native libraries according to the Runtime Identifier (RID).
/// </summary>
/// <param name="name">The descriptive name of the load context.</param>
/// <param name="pluginPath">The physical file path to the plugin assembly on disk (typically a shadow-copied assembly).</param>
/// <param name="isCollectible">Whether the context can be unloaded from memory dynamically.</param>
public class PluginLoadContext(string name, string pluginPath, bool isCollectible = true) : AssemblyLoadContext(name, isCollectible)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLoadContext"/> class with a generated name derived from the assembly file name.
    /// </summary>
    /// <param name="pluginPath">The physical file path to the plugin assembly on disk.</param>
    /// <param name="isCollectible">Whether the context can be unloaded from memory dynamically.</param>
    public PluginLoadContext(string pluginPath, bool isCollectible = true)
        : this($"PluginContext_{Path.GetFileNameWithoutExtension(pluginPath)}", pluginPath, isCollectible)
    {
    }

    /// <summary>
    /// Resolves managed assembly dependencies using the plugin's dependency specification.
    /// Returns <c>null</c> if the assembly is not part of the plugin dependencies, allowing fallback to the default load context.
    /// </summary>
    /// <param name="assemblyName">The assembly identity to resolve.</param>
    /// <returns>The resolved assembly, or <c>null</c> if fallback to the default context is required.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is not null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    /// <summary>
    /// Resolves unmanaged (native) libraries according to the host process Runtime Identifier (RID) using the plugin dependencies.
    /// </summary>
    /// <param name="unmanagedDllName">The name of the unmanaged native library (e.g. <c>myNativeLib.dll</c> or <c>libmyNativeLib.so</c>).</param>
    /// <returns>A pointer to the loaded native library, or <see cref="IntPtr.Zero"/> if unresolved.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is not null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}
