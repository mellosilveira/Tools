using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Plugin-specific storage operations on DLL assemblies: scanning plugin lifecycle partitions,
/// validating managed .NET metadata and host architecture compatibility, local shadow copying for
/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, coordinating plugin partition transitions
/// (incoming, loaded, archive, quarantine, pending, rejected), and caching parsed <see cref="DiscoveredPlugin"/> instances.
/// Generic file/directory manipulation delegates to <see cref="IFileStorageService"/>.
/// </summary>
/// <param name="logger">The logger instance for diagnostics and tracing.</param>
/// <param name="fileStorageService">Storage abstraction service used for file discovery, streaming, hashing, and generic file movements.</param>
/// <param name="cache">Plugin cache used to memoize the parsed <see cref="DiscoveredPlugin"/> instances by name and version.</param>
/// <param name="settings">Plugin settings providing the directory and unmanaged assembly policy.</param>
public class PluginFileProcessor(
    ILogger<PluginFileProcessor> logger,
    IFileStorageService fileStorageService,
    PluginCache cache,
    PluginSettings settings)
{
    private static readonly Regex PluginFileRegex = new(@"^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$", RegexOptions.Compiled);
    private readonly string _shadowCopyRoot = Path.Combine(Path.GetTempPath(), "MelloSilveiraTools", "Plugins", "ShadowCopy", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Asynchronously lists raw <see cref="StorageEntryMetadata"/> entries in the specified plugin <paramref name="partition"/>
    /// without mutating or quarantining any files.
    /// </summary>
    /// <param name="partition">Canonical partition name from <see cref="PluginStoragePartitions"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous stream of storage entries residing in <paramref name="partition"/>.</returns>
    public IAsyncEnumerable<StorageEntryMetadata> ListPartitionFilesAsync(string partition, CancellationToken cancellationToken = default)
        => fileStorageService.ListFilesAsync(settings.Directory, partition, cancellationToken);

    /// <summary>
    /// Performs read-only static PE/CLI metadata, IL-only policy, and host architecture validation on the file at
    /// <paramref name="location"/> without executing code or moving the file to quarantine.
    /// </summary>
    /// <param name="location">Canonical storage location of the candidate assembly.</param>
    /// <param name="fileName">Filename used in diagnostic messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="null"/> when valid; otherwise a human-readable validation failure reason.</returns>
    public async Task<string?> ValidateAssemblyReadOnlyAsync(string location, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await using Stream stream = await fileStorageService.OpenReadAsync(location, cancellationToken).ConfigureAwait(false);
            using MemoryStream? buffer = stream.CanSeek ? null : new();
            if (buffer is not null)
            {
                await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                buffer.Position = 0;
            }

            using PEReader peReader = new(buffer ?? stream, PEStreamOptions.LeaveOpen);
            if (!peReader.HasMetadata)
                return $"Plugin candidate '{fileName}' is not a valid managed .NET assembly.";

            return IsArchitectureCompatible(peReader, out string failureReason)
                ? null
                : $"Plugin candidate '{fileName}' is incompatible with the host process architecture '{RuntimeInformation.ProcessArchitecture}': {failureReason}.";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to inspect PE headers for plugin file '{FileName}'.", fileName);
            return $"Failed to inspect PE headers for plugin file '{fileName}'.";
        }
    }

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

            if (!TryParsePluginFileName(entry.FileName, out string name, out PluginVersion pluginVersion))
            {
                logger.LogWarning("Plugin candidate file '{FileName}' does not match expected pattern '{{name}}.v{{major}}.{{minor}}.{{patch}}.dll'. Moving to quarantine.", entry.FileName);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            string? invalidReason = await ValidateAssemblyReadOnlyAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
            if (invalidReason is not null)
            {
                logger.LogWarning("{Reason} Moving '{FileName}' to quarantine.", invalidReason, entry.FileName);
                await MoveToQuarantineAsync(entry.Location, entry.FileName, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (pluginName is not null && !name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (version is not null && pluginVersion != version.Value)
                continue;

            yield return cache.GetOrAdd(name, pluginVersion, () => new(name, pluginVersion, entry.Location, entry.CreatedAt));
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
            if (string.IsNullOrEmpty(entry.FileName) || !TryParsePluginFileName(entry.FileName, out string name, out PluginVersion pluginVersion))
                continue;

            if (pluginName is not null && !name.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (version is not null && pluginVersion != version.Value)
                continue;

            yield return cache.GetOrAdd(name, pluginVersion, () => new(name, pluginVersion, entry.Location, entry.CreatedAt));
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
        string shadowDir = Path.Combine(_shadowCopyRoot, $"{plugin.Name}_{plugin.Version}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(shadowDir);
        string shadowPath = Path.Combine(shadowDir, fileName);

        await using (Stream sourceStream = await fileStorageService.OpenReadAsync(plugin.FullPath, cancellationToken).ConfigureAwait(false))
        {
            await using FileStream destinationStream = new(shadowPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
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
    /// Purges all temporary shadow copy directories and files created by this <see cref="PluginFileProcessor"/> instance
    /// without deleting active shadow copies owned by sibling hosts or concurrent test fixtures.
    /// </summary>
    public void CleanShadowCopies()
    {
        if (!Directory.Exists(_shadowCopyRoot))
            return;

        try
        {
            foreach (string dir in Directory.GetDirectories(_shadowCopyRoot))
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

            if (!Directory.EnumerateFileSystemEntries(_shadowCopyRoot).Any())
                Directory.Delete(_shadowCopyRoot, recursive: false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to purge shadow copy directory '{Directory}'.", _shadowCopyRoot);
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
            await fileStorageService.MoveAsync(destinationLocation, backupLocation, overwrite: true, cancellationToken).ConfigureAwait(false);
        }

        await fileStorageService.MoveAsync(plugin.FullPath, destinationLocation, overwrite: true, cancellationToken).ConfigureAwait(false);
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
        string destinationLocation = await MoveToPartitionAsync(plugin.FullPath, PluginStoragePartitions.Incoming, fileName, cancellationToken).ConfigureAwait(false);
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

        await fileStorageService.MoveAsync(plugin.FullPath, destinationLocation, overwrite: true, cancellationToken).ConfigureAwait(false);
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
        await MoveToPartitionAsync(plugin.FullPath, PluginStoragePartitions.Archive, fileName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to parse a plugin assembly file name conforming to <c>{name}.v{major}.{minor}.{patch}.dll</c>.
    /// </summary>
    /// <param name="fileName">File name to parse (without directory components).</param>
    /// <param name="pluginName">When valid, receives the logical plugin name.</param>
    /// <param name="version">When valid, receives the parsed semantic version.</param>
    /// <returns><see langword="true"/> when <paramref name="fileName"/> matches the required pattern; otherwise <see langword="false"/>.</returns>
    public bool TryParsePluginFileName(string fileName, out string pluginName, out PluginVersion version)
    {
        pluginName = string.Empty;
        version = PluginVersion.Default;

        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return false;

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        Match match = PluginFileRegex.Match(fileNameWithoutExtension);
        if (!match.Success)
            return false;

        pluginName = match.Groups["name"].Value;
        version = new PluginVersion(
            int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>
    /// Checks whether a plugin assembly with <paramref name="fileName"/> already exists in any active
    /// lifecycle partition (<c>pending/</c>, <c>incoming/</c>, or <c>loaded/</c>).
    /// </summary>
    /// <param name="fileName">Sanitized assembly file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if a binary with the same name/version already exists; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ExistsInActivePartitionsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        string pendingLocation = ResolvePartitionLocation(PluginStoragePartitions.Pending, fileName);
        if (await fileStorageService.ExistsAsync(pendingLocation, cancellationToken).ConfigureAwait(false))
            return true;

        string incomingLocation = ResolvePartitionLocation(PluginStoragePartitions.Incoming, fileName);
        if (await fileStorageService.ExistsAsync(incomingLocation, cancellationToken).ConfigureAwait(false))
            return true;

        string loadedLocation = ResolvePartitionLocation(PluginStoragePartitions.Loaded, fileName);
        return await fileStorageService.ExistsAsync(loadedLocation, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Promotes an approved community plugin assembly from <c>pending/</c> into <c>incoming/</c>.
    /// </summary>
    /// <param name="pendingLocation">Current location of the binary in the <c>pending/</c> partition.</param>
    /// <param name="fileName">Sanitized assembly file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved storage location inside <c>incoming/</c>.</returns>
    public Task<string> MovePendingToIncomingAsync(string pendingLocation, string fileName, CancellationToken cancellationToken = default) =>
        MoveToPartitionAsync(pendingLocation, PluginStoragePartitions.Incoming, fileName, cancellationToken);

    /// <summary>
    /// Moves a rejected community plugin assembly from <c>pending/</c> into <c>rejected/</c>.
    /// </summary>
    /// <param name="pendingLocation">Current location of the binary in the <c>pending/</c> partition.</param>
    /// <param name="fileName">Sanitized assembly file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved storage location inside <c>rejected/</c>.</returns>
    public Task<string> MovePendingToRejectedAsync(string pendingLocation, string fileName, CancellationToken cancellationToken = default) =>
        MoveToPartitionAsync(pendingLocation, PluginStoragePartitions.Rejected, fileName, cancellationToken);

    /// <summary>
    /// Validates that the portable executable (PE) reader represents a valid managed .NET assembly
    /// compatible with the host operating system, process architecture, and configured <see cref="PluginSettings.AllowUnmanagedDlls"/> policy.
    /// </summary>
    /// <param name="peReader">The portable executable reader inspecting the candidate assembly.</param>
    /// <param name="failureReason">When incompatible, outputs a description of why the assembly was rejected.</param>
    /// <returns><c>true</c> if the assembly is compatible with the host architecture; otherwise, <c>false</c>.</returns>
    public bool IsArchitectureCompatible(PEReader peReader, out string failureReason)
    {
        if (!peReader.HasMetadata)
        {
            failureReason = "Binary does not contain managed .NET metadata.";
            return false;
        }

        MetadataReader metadataReader = peReader.GetMetadataReader();
        if (!metadataReader.IsAssembly || metadataReader.TypeDefinitions.Count == 0)
        {
            failureReason = "Binary does not contain a valid .NET assembly definition.";
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

        if (!settings.AllowUnmanagedDlls && !flags.HasFlag(CorFlags.ILOnly))
        {
            failureReason = "Assembly contains native/mixed-mode code, which is disallowed when AllowUnmanagedDlls is false.";
            return false;
        }

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

    private string ResolvePartitionLocation(string partition, string fileName) => fileStorageService.ResolveLocation(settings.Directory, partition, fileName);

    private async Task<string> MoveToPartitionAsync(string sourceLocation, string partition, string fileName, CancellationToken cancellationToken)
    {
        string destinationLocation = ResolvePartitionLocation(partition, fileName);
        await fileStorageService.MoveAsync(sourceLocation, destinationLocation, overwrite: true, cancellationToken).ConfigureAwait(false);
        return destinationLocation;
    }

    private Task MoveToQuarantineAsync(string sourceLocation, string fileName, CancellationToken cancellationToken = default) =>
        MoveToPartitionAsync(sourceLocation, PluginStoragePartitions.Quarantine, fileName, cancellationToken);
}
