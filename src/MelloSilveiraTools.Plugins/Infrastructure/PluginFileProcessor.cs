using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Storage-level operations on plugin DLL assemblies: scanning storage partitions,
/// validating managed .NET metadata, moving assemblies across lifecycle partitions
/// (incoming, loaded, archive, quarantine), and caching parsed <see cref="DiscoveredPlugin"/> instances.
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
    /// Asynchronously scans the incoming storage partition for DLLs matching the given <paramref name="pluginName"/> and <paramref name="version"/> filters,
    /// verifying that each candidate is a valid managed .NET assembly and isolating non-conforming binaries to quarantine.
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
            try
            {
                await using Stream stream = await fileStorageService.OpenReadAsync(entry.Location, cancellationToken).ConfigureAwait(false);
                isManaged = IsManagedDotNetAssembly(stream);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to inspect PE headers for plugin file '{FileName}'. Moving to quarantine.", entry.FileName);
                isManaged = false;
            }

            if (!isManaged)
            {
                logger.LogWarning("Plugin candidate '{FileName}' is not a valid managed .NET assembly. Moving to quarantine.", entry.FileName);
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

    private static bool IsManagedDotNetAssembly(Stream stream)
    {
        using MemoryStream? buffer = stream.CanSeek ? null : new();
        if (buffer is not null)
        {
            stream.CopyTo(buffer);
            buffer.Position = 0;
        }

        using PEReader peReader = new(buffer ?? stream, PEStreamOptions.LeaveOpen);
        return peReader.HasMetadata;
    }
}
