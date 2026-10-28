using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Storage-level operations on plugin DLL assemblies: scanning storage partitions,
/// validating managed .NET metadata and host architecture compatibility, shadow copying,
/// moving assemblies across lifecycle partitions (incoming, loaded, archive, quarantine),
/// and caching parsed <see cref="DiscoveredPlugin"/> instances.
/// </summary>
/// <param name="logger">The logger instance for diagnostics and tracing.</param>
/// <param name="fileStorageService">Storage abstraction service used for file discovery, streaming, and lifecycle movements.</param>
/// <param name="cache">Plugin cache used to memoize the parsed <see cref="DiscoveredPlugin"/> instances by name and version.</param>
/// <param name="settings">Plugin settings providing the directory used to locate storage partitions.</param>
public class PluginFileProcessor(
    ILogger<PluginFileProcessor> logger,
    IFileStorageService fileStorageService,
    PluginCache cache,
    PluginSettings settings)
{
    private static readonly Regex PluginFileRegex = new(@"^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Enumeration options configured for case-insensitive file matching across platforms (such as Linux ext4).
    /// </summary>
    protected static readonly EnumerationOptions CaseInsensitiveOptions = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = false,
        IgnoreInaccessible = true
    };

    /// <summary>
    /// Asynchronously scans the incoming storage partition for DLLs matching the given <paramref name="pluginName"/> and <paramref name="version"/> filters,
    /// verifying that each candidate is a valid managed .NET assembly compatible with the host process architecture, and isolating non-conforming or incompatible binaries to quarantine.
    /// </summary>
    /// <param name="pluginName">Optional plugin name prefix used to filter matching files.</param>
    /// <param name="version">Optional plugin version used to filter matching files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of discovered valid plugin instances.</returns>
    public async IAsyncEnumerable<DiscoveredPlugin> ScanAsync(
        string? pluginName = null,
        PluginVersion? version = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (StorageEntryMetadata entry in fileStorageService.ListFilesAsync(settings.Directory, PluginStoragePartitions.Incoming, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(entry.FileName) || !entry.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.FileName);
            Match match = PluginFileRegex.Match(fileNameWithoutExtension);
            if (!match.Success)
            {
                logger.LogWarning("Plugin candidate file '{FileName}' does not match expected pattern '{{name}}.v{{major}}.{{minor}}.{{patch}}.dll'. Moving to quarantine.", entry.FileName);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            bool isManaged;
            bool isCompatible = false;
            string failureReason = string.Empty;
            try
            {
                await using Stream stream = await fileStorageService.OpenReadAsync(entry.Location, cancellationToken).ConfigureAwait(false);
                using MemoryStream? buffer = stream.CanSeek ? null : new();
                if (buffer is not null)
                {
                    await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                    buffer.Position = 0;
                }

                using PEReader peReader = new(buffer ?? stream, PEStreamOptions.LeaveOpen);
                isManaged = peReader.HasMetadata;
                if (isManaged)
                    isCompatible = IsArchitectureCompatible(peReader, out failureReason);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to inspect PE headers for plugin file '{FileName}'. Moving to quarantine.", entry.FileName);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (!isManaged)
            {
                logger.LogWarning("Plugin candidate '{FileName}' is not a valid managed .NET assembly. Moving to quarantine.", entry.FileName);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (!isCompatible)
            {
                logger.LogWarning(
                    "Plugin candidate '{FileName}' is incompatible with the host process architecture '{Architecture}': {Reason}. Moving to quarantine.",
                    entry.FileName,
                    RuntimeInformation.ProcessArchitecture,
                    failureReason);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            string name = match.Groups["name"].Value;
            PluginVersion pluginVersion = new(
                int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture));

            if (pluginName is not null && !name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (version is not null && pluginVersion != version.Value)
                continue;

            DiscoveredPlugin discovered = cache.GetOrAdd(name, pluginVersion, () => new(name, pluginVersion, entry.Location, entry.CreatedAt));
            yield return discovered;
        }
    }

    /// <summary>
    /// Asynchronously scans the loaded storage partition for DLLs matching the given <paramref name="pluginName"/> and <paramref name="version"/> filters.
    /// </summary>
    /// <param name="pluginName">Optional plugin name prefix used to filter matching files.</param>
    /// <param name="version">Optional plugin version used to filter matching files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of discovered valid plugin instances residing in the loaded partition.</returns>
    public async IAsyncEnumerable<DiscoveredPlugin> ScanLoadedAsync(
        string? pluginName = null,
        PluginVersion? version = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (StorageEntryMetadata entry in fileStorageService.ListFilesAsync(settings.Directory, PluginStoragePartitions.Loaded, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(entry.FileName) || !entry.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(entry.FileName);
            Match match = PluginFileRegex.Match(fileNameWithoutExtension);
            if (!match.Success)
                continue;

            string name = match.Groups["name"].Value;
            PluginVersion pluginVersion = new(
                int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture));

            if (pluginName is not null && !name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (version is not null && pluginVersion != version.Value)
                continue;

            DiscoveredPlugin discovered = cache.GetOrAdd(name, pluginVersion, () => new(name, pluginVersion, entry.Location, entry.CreatedAt));
            yield return discovered;
        }
    }

    /// <summary>
    /// Creates a temporary shadow copy of the plugin assembly (and optional companion <c>.deps.json</c> file) in a dedicated temporary
    /// shadow directory on the local disk. This prevents persistent OS file locks on the primary file in storage partitions.
    /// </summary>
    /// <param name="plugin">The discovered plugin to shadow copy.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the absolute local path to the shadow copy assembly and the shadow directory path.</returns>
    public async Task<(string ShadowAssemblyPath, string ShadowDirectory)> CreateShadowCopyAsync(
        DiscoveredPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string shadowDir = Path.Combine(Path.GetTempPath(), "MelloSilveiraTools", "Plugins", "ShadowCopy", $"{plugin.Name}_{plugin.Version}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(shadowDir);
        string shadowPath = Path.Combine(shadowDir, fileName);

        await using (Stream sourceStream = await fileStorageService.OpenReadAsync(plugin.FullPath, cancellationToken).ConfigureAwait(false))
        {
            await using (FileStream destinationStream = new(shadowPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
            }
        }

        string depsFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.deps.json";
        string depsSourceLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Loaded, depsFileName);
        if (await fileStorageService.ExistsAsync(depsSourceLocation, cancellationToken).ConfigureAwait(false))
        {
            string shadowDepsPath = Path.Combine(shadowDir, depsFileName);
            await using Stream depsSource = await fileStorageService.OpenReadAsync(depsSourceLocation, cancellationToken).ConfigureAwait(false);
            await using FileStream depsDest = new(shadowDepsPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await depsSource.CopyToAsync(depsDest, cancellationToken).ConfigureAwait(false);
        }

        return (shadowPath, shadowDir);
    }

    /// <summary>
    /// Purges all temporary shadow copy directories and files generated during plugin loading across all sessions.
    /// </summary>
    public void CleanShadowCopies()
    {
        string rootShadowDir = Path.Combine(Path.GetTempPath(), "MelloSilveiraTools", "Plugins", "ShadowCopy");
        if (!Directory.Exists(rootShadowDir))
            return;

        try
        {
            foreach (string dir in Directory.GetDirectories(rootShadowDir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not delete shadow copy directory '{Directory}'. It may still be in use.", dir);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to purge shadow copy directory '{Directory}'.", rootShadowDir);
        }
    }

    /// <summary>
    /// Asynchronously moves a plugin DLL from the incoming partition to the loaded partition.
    /// If an assembly with the same name already exists in the loaded partition, backs it up with an <c>.old</c> suffix.
    /// </summary>
    /// <param name="plugin">The plugin whose DLL should be moved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task MoveToLoadedFolderAsync(DiscoveredPlugin plugin, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string destinationLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Loaded, fileName);

        if (await fileStorageService.ExistsAsync(destinationLocation, cancellationToken).ConfigureAwait(false))
        {
            string backupFileName = $"{fileName}.old";
            logger.LogWarning("Destination file already exists: {DestinationFileName}. Backing up existing file to: {BackupFileName}", fileName, backupFileName);
            string backupLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Loaded, backupFileName);
            await MoveFileAsync(destinationLocation, backupLocation, cancellationToken).ConfigureAwait(false);
        }

        await MoveFileAsync(plugin.FullPath, destinationLocation, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously moves a plugin DLL back from the loaded partition to the incoming partition.
    /// </summary>
    /// <param name="plugin">The plugin whose DLL should be moved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new <see cref="DiscoveredPlugin"/> pointing to the relocated assembly in the incoming partition.</returns>
    public async Task<DiscoveredPlugin> MoveToMainFolderAsync(DiscoveredPlugin plugin, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string destinationLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Incoming, fileName);
        await MoveFileAsync(plugin.FullPath, destinationLocation, cancellationToken).ConfigureAwait(false);
        return new DiscoveredPlugin(plugin.Name, plugin.Version, destinationLocation, plugin.DiscoveredAt);
    }

    /// <summary>
    /// Asynchronously replaces an existing DLL in the loaded partition with one from the incoming partition.
    /// </summary>
    /// <param name="plugin">The plugin whose DLL should be moved into the loaded partition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReplaceInLoadedFolderAsync(DiscoveredPlugin plugin, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string destinationLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Loaded, fileName);

        if (await fileStorageService.ExistsAsync(destinationLocation, cancellationToken).ConfigureAwait(false))
            await fileStorageService.DeleteAsync(destinationLocation, cancellationToken).ConfigureAwait(false);

        await MoveFileAsync(plugin.FullPath, destinationLocation, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously checks whether a DLL with the same name and version exists in the loaded partition.
    /// </summary>
    /// <param name="plugin">The plugin whose presence in the loaded partition is being checked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A value indicating whether the plugin file exists in the loaded partition.</returns>
    public async ValueTask<bool> ExistsInLoadedFolderAsync(DiscoveredPlugin plugin, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string location = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Loaded, fileName);
        return await fileStorageService.ExistsAsync(location, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously moves an obsolete plugin DLL from the loaded partition to the archive partition.
    /// </summary>
    /// <param name="plugin">The plugin whose DLL should be archived.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task MoveToArchiveFolderAsync(DiscoveredPlugin plugin, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(plugin.FullPath);
        string destinationLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Archive, fileName);
        await MoveFileAsync(plugin.FullPath, destinationLocation, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the portable executable (PE) reader represents a valid managed .NET assembly
    /// compatible with the host operating system and process architecture.
    /// </summary>
    /// <param name="peReader">The portable executable reader inspecting the candidate assembly.</param>
    /// <param name="failureReason">When incompatible, outputs a description of why the assembly was rejected.</param>
    /// <returns><c>true</c> if the assembly is compatible with the host architecture; otherwise, <c>false</c>.</returns>
    public static bool IsArchitectureCompatible(PEReader peReader, out string failureReason)
    {
        if (!peReader.HasMetadata)
        {
            failureReason = "Binary does not contain managed .NET metadata.";
            return false;
        }

        PEHeaders headers = peReader.PEHeaders;
        CorHeader? corHeader = headers.CorHeader;
        if (corHeader is null)
        {
            failureReason = "Binary does not contain a CLI CorHeader.";
            return false;
        }

        Architecture hostArch = RuntimeInformation.ProcessArchitecture;
        Machine machine = headers.CoffHeader.Machine;
        CorFlags flags = corHeader.Flags;

        // AnyCPU assembly: ILOnly is set and Requires32Bit is not set.
        if (flags.HasFlag(CorFlags.ILOnly) && !flags.HasFlag(CorFlags.Requires32Bit) && machine == Machine.I386)
        {
            failureReason = string.Empty;
            return true;
        }

        // If assembly requires 32-bit execution:
        if (flags.HasFlag(CorFlags.Requires32Bit))
        {
            if (hostArch is Architecture.X86 or Architecture.Arm)
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = $"Assembly requires 32-bit execution, but host process is running on 64-bit {hostArch}.";
            return false;
        }

        // Platform-specific machine targets:
        bool matches = hostArch switch
        {
            Architecture.X64 => machine == Machine.Amd64,
            Architecture.Arm64 => machine == Machine.Arm64,
            Architecture.X86 => machine == Machine.I386,
            Architecture.Arm => machine == Machine.Arm,
            _ => false
        };

        if (!matches)
        {
            failureReason = $"Assembly machine type '{machine}' is incompatible with the host process architecture '{hostArch}'.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private async Task MoveFileAsync(string sourceLocation, string destinationLocation, CancellationToken cancellationToken = default)
    {
        if (string.Equals(sourceLocation, destinationLocation, StringComparison.OrdinalIgnoreCase))
            return;

        await using (Stream sourceStream = await fileStorageService.OpenReadAsync(sourceLocation, cancellationToken).ConfigureAwait(false))
        {
            await using (Stream destinationStream = await fileStorageService.OpenWriteAsync(destinationLocation, cancellationToken).ConfigureAwait(false))
            {
                await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                await destinationStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await fileStorageService.DeleteAsync(sourceLocation, cancellationToken).ConfigureAwait(false);
    }

    private async Task MoveToQuarantineAsync(string sourceLocation, string fileName, CancellationToken cancellationToken = default)
    {
        string destinationLocation = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Quarantine, fileName);
        await MoveFileAsync(sourceLocation, destinationLocation, cancellationToken).ConfigureAwait(false);
    }
}
