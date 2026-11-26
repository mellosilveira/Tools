using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// The system responsible for dynamically loading external modules ("plugins") into our application.
/// Uses shadow copying, static PE/CLI metadata inspection, and <see cref="PluginLoadContext"/> to isolate dependencies,
/// enforce IL-only managed constraints by default, and prevent operating system file locking.
/// </summary>
/// <param name="typeProcessors">Collection of type processors used to handle each <see cref="IPluginTypeProcessor.ProcessableType"/> discovered inside a plugin assembly.</param>
/// <param name="timeProvider">The time provider used to record plugin load completion timestamps.</param>
/// <param name="fileProcessor">File processor used for storage and shadow copy operations.</param>
/// <param name="cache">Plugin cache used to memoize loaded and registered plugins by name and version.</param>
/// <param name="settings">Plugin settings controlling security options such as native DLL loading.</param>
public class PluginAssemblyProcessor(
    IEnumerable<IPluginTypeProcessor> typeProcessors,
    TimeProvider timeProvider,
    PluginFileProcessor fileProcessor,
    PluginCache cache,
    PluginSettings settings)
{
    private readonly IPluginTypeProcessor[] _typeProcessors = [.. typeProcessors];

    /// <summary>
    /// Asynchronously loads the assembly described by <paramref name="discovered"/>, validating its PE/CLI metadata and architecture headers,
    /// shadow-copying it to eliminate persistent OS file locks, and returning the set of processable types it contains.
    /// </summary>
    /// <param name="discovered">Discovered plugin metadata holding the assembly location.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A loaded plugin representation holding the collectible assembly load context and discovered processable types.</returns>
    /// <exception cref="BadImageFormatException">
    /// Thrown when the binary does not contain valid managed .NET metadata, violates IL-only policy, or has an incompatible architecture.
    /// </exception>
    public Task<LoadedPlugin> LoadAsync(DiscoveredPlugin discovered, CancellationToken cancellationToken = default) => cache.GetOrAddAsync(
        discovered.Name,
        discovered.Version,
        async () =>
        {
            (string shadowAssemblyPath, string shadowDir) = await fileProcessor.CreateShadowCopyAsync(discovered, cancellationToken).ConfigureAwait(false);
            try
            {
                await using (FileStream inspectStream = new(shadowAssemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using PEReader peReader = new(inspectStream, PEStreamOptions.LeaveOpen);
                    ValidateAssemblyArchitecture(peReader, discovered.Name, settings.AllowUnmanagedDlls);
                }

                PluginLoadContext pluginContext = new($"PluginContext_{discovered.Name}", shadowAssemblyPath, isCollectible: true, allowUnmanagedDlls: settings.AllowUnmanagedDlls);
                Assembly assembly = pluginContext.LoadFromAssemblyPath(shadowAssemblyPath);

                Type[] processableTypes = [.. assembly.GetTypes().Where(t =>
                    !t.IsInterface &&
                    !t.IsAbstract &&
                    Array.Exists(_typeProcessors, tp => tp.ProcessableType.IsAssignableFrom(t)))];

                return new LoadedPlugin(discovered, processableTypes, pluginContext, shadowDir);
            }
            catch
            {
                if (Directory.Exists(shadowDir))
                {
                    try
                    {
                        Directory.Delete(shadowDir, recursive: true);
                    }
                    // Ignore cleanup errors during exception propagation.
                    catch { }
                }

                throw;
            }
        });

    /// <summary>
    /// Validates the portable executable (PE) and CLI metadata headers using <see cref="PEReader"/> without executing any code
    /// to ensure the assembly is a valid managed .NET assembly compatible with the host process architecture.
    /// </summary>
    /// <param name="peReader">The portable executable reader inspecting the assembly.</param>
    /// <param name="assemblyName">The name of the assembly for error reporting.</param>
    /// <param name="allowUnmanagedDlls">When <see langword="false"/>, rejects mixed-mode (non-IL-only) assemblies.</param>
    /// <exception cref="BadImageFormatException">Thrown when the assembly is invalid, mixed-mode while disallowed, or architecture-incompatible.</exception>
    public static void ValidateAssemblyArchitecture(PEReader peReader, string assemblyName, bool allowUnmanagedDlls = false)
    {
        if (!peReader.HasMetadata)
            throw new BadImageFormatException($"Assembly '{assemblyName}' does not contain valid managed .NET metadata.");

        MetadataReader metadataReader = peReader.GetMetadataReader();
        if (!metadataReader.IsAssembly || metadataReader.TypeDefinitions.Count == 0)
            throw new BadImageFormatException($"Assembly '{assemblyName}' does not contain a valid .NET assembly definition.");

        PEHeaders headers = peReader.PEHeaders;
        Machine machine = headers.CoffHeader.Machine;

        CorHeader? corHeader = headers.CorHeader ?? throw new BadImageFormatException($"Assembly '{assemblyName}' does not contain a CLI CorHeader.");
        CorFlags flags = corHeader.Flags;

        if (!allowUnmanagedDlls && !flags.HasFlag(CorFlags.ILOnly))
            throw new BadImageFormatException($"Assembly '{assemblyName}' contains native/mixed-mode code, which is disallowed when AllowUnmanagedDlls is false.");

        // AnyCPU assembly: ILOnly is set and Requires32Bit is not set.
        if (flags.HasFlag(CorFlags.ILOnly) && !flags.HasFlag(CorFlags.Requires32Bit) && machine == Machine.I386)
            return;

        Architecture hostArch = RuntimeInformation.ProcessArchitecture;

        // If assembly requires 32-bit execution:
        if (flags.HasFlag(CorFlags.Requires32Bit))
        {
            if (hostArch is Architecture.X86 or Architecture.Arm)
                return;

            throw new BadImageFormatException($"Assembly '{assemblyName}' requires 32-bit execution, but the host process is running on 64-bit {hostArch}.");
        }

        // Platform-specific machine types:
        bool matches = hostArch switch
        {
            Architecture.X64 => machine == Machine.Amd64,
            Architecture.Arm64 => machine == Machine.Arm64,
            Architecture.X86 => machine == Machine.I386,
            Architecture.Arm => machine == Machine.Arm,
            _ => false
        };

        if (!matches)
            throw new BadImageFormatException($"Assembly '{assemblyName}' machine type '{machine}' is incompatible with the host process architecture '{hostArch}'.");
    }

    /// <summary>
    /// Returns a <see cref="RegisteredPlugin"/> instance for <paramref name="loaded"/>, creating a cache entry when missing.
    /// </summary>
    /// <param name="loaded">The loaded plugin instance.</param>
    /// <returns>A registered plugin metadata entry.</returns>
    public RegisteredPlugin GetInfo(LoadedPlugin loaded) => cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

    /// <summary>
    /// Runs each processable type of <paramref name="loaded"/> through its matching <see cref="IPluginTypeProcessor"/>
    /// using the provided <paramref name="context"/> and records the progress in cache.
    /// </summary>
    /// <param name="loaded">The loaded plugin instance.</param>
    /// <param name="context">The registration context for startup or runtime DI.</param>
    /// <returns>The registered plugin metadata entry.</returns>
    public RegisteredPlugin ProcessTypes(LoadedPlugin loaded, PluginRegistrationContext context)
    {
        RegisteredPlugin registered = cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

        foreach (Type type in loaded.ProcessableTypes)
        {
            Array.Find(_typeProcessors, tp => tp.ProcessableType.IsAssignableFrom(type))!.Process(type, context);
            registered.MarkTypeLoaded(type, timeProvider.GetUtcNow());
            cache.Update(loaded.Name, loaded.Version, registered);
        }

        return registered;
    }
}
