using MelloSilveiraTools.Plugins.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// File-system level operations on plugin DLLs: scanning the plugins directory,
/// moving DLLs between the main and loaded subfolders, and parsing filenames into
/// <see cref="DiscoveredPlugin"/> instances cached in <see cref="PluginCache"/>.
/// </summary>
/// <param name="logger"></param>
/// <param name="cache">Plugin cache used to memoize the parsed <see cref="DiscoveredPlugin"/> instances by name and version.</param>
/// <param name="settings">Plugin settings providing the root plugins directory used to locate the main and <c>loaded/</c> subfolders.</param>
public class PluginFileProcessor(
    ILogger<PluginFileProcessor> logger,
    PluginCache cache,
    PluginSettings settings)
{
    private static readonly Regex PluginFileRegex = new(@"^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$", RegexOptions.Compiled);
    private readonly string _pluginsDirectory = settings.Directory;
    private readonly string _loadedDirectory = Path.Combine(settings.Directory, "loaded");

    /// <summary>
    /// Scans the main plugins directory for DLLs matching the given <paramref name="pluginName"/> and <paramref name="version"/> filters.
    /// </summary>
    /// <param name="pluginName">Optional plugin name prefix used to filter the matching files.</param>
    /// <param name="version">Optional plugin version used to filter the matching files.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when the configured plugins directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to read the plugins directory.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while enumerating files in the plugins directory.</exception>
    /// <exception cref="InvalidOperationException">Thrown by the iterator when a discovered file does not match the expected plugin filename pattern.</exception>
    public IEnumerable<DiscoveredPlugin> Scan(string? pluginName = null, PluginVersion? version = null)
        => Scan(Directory.GetFiles(_pluginsDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"));

    /// <summary>
    /// Scans the <c>loaded/</c> subfolder for DLLs matching the given <paramref name="pluginName"/> and <paramref name="version"/> filters.
    /// </summary>
    /// <param name="pluginName">Optional plugin name prefix used to filter the matching files.</param>
    /// <param name="version">Optional plugin version used to filter the matching files.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when the <c>loaded/</c> subfolder does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to read the <c>loaded/</c> subfolder.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while enumerating files in the <c>loaded/</c> subfolder.</exception>
    /// <exception cref="InvalidOperationException">Thrown by the iterator when a discovered file does not match the expected plugin filename pattern.</exception>
    public IEnumerable<DiscoveredPlugin> ScanLoaded(string? pluginName = null, PluginVersion? version = null)
        => Scan(Directory.GetFiles(_loadedDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"));

    /// <summary>Moves a plugin DLL back from the loaded/ subfolder to the main plugins directory.</summary>
    /// <param name="plugin">The plugin whose DLL should be moved.</param>
    /// <exception cref="IOException">Thrown when the destination file already exists or another I/O error occurs during the move.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to move the file.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the source path or destination directory cannot be found.</exception>
    public void MoveToMainFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        File.Move(plugin.FullPath, destination, overwrite: false);
    }

    /// <summary>Moves a plugin DLL from the plugins directory to the loaded/ subfolder.</summary>
    /// <param name="plugin">The plugin whose DLL should be moved.</param>
    /// <exception cref="IOException">Thrown when the destination file already exists or another I/O error occurs during the move.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to move the file.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the source path or destination directory cannot be found.</exception>
    public void MoveToLoadedFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        File.Move(plugin.FullPath, destination, overwrite: false);
    }

    /// <summary>Replaces an existing DLL in the loaded/ subfolder with one from the plugins directory.</summary>
    /// <param name="plugin">The plugin whose DLL should be moved into the <c>loaded/</c> subfolder, replacing any existing file with the same name.</param>
    /// <exception cref="IOException">Thrown when an I/O error occurs while deleting the existing destination file or moving the new one.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the caller does not have permission to delete or move the file.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the source path or destination directory cannot be found.</exception>
    public void ReplaceInLoadedFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(plugin.FullPath, destination);
    }

    /// <summary>Checks whether a DLL with the same name and version exists in the loaded/ subfolder.</summary>
    /// <param name="plugin">The plugin whose presence in the <c>loaded/</c> subfolder is being checked.</param>
    public bool ExistsInLoadedFolder(DiscoveredPlugin plugin)
    {
        string loadedPath = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        return File.Exists(loadedPath);
    }

    private IEnumerable<DiscoveredPlugin> Scan(IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            DiscoveredPlugin? plugin = Parse(file);
            if (plugin != null)
                yield return plugin;
        }
    }

    /// <summary>
    /// Parses a DLL file path into a <see cref="DiscoveredPlugin"/>.
    /// Expected pattern: {name}.v{major}.{minor}.{patch}.dll.
    /// </summary>
    /// <param name="dllFilePath">Full path to the DLL file whose filename should be parsed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the filename does not match the expected plugin filename pattern.</exception>
    private DiscoveredPlugin? Parse(string dllFilePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(dllFilePath);
        Match match = PluginFileRegex.Match(fileName);

        if (match.Success)
        {
            string name = match.Groups["name"].Value;
            PluginVersion version = new(
                int.Parse(match.Groups["major"].Value),
                int.Parse(match.Groups["minor"].Value),
                int.Parse(match.Groups["patch"].Value));

            return cache.GetOrAdd(name, version, () => new(name, version, Path.GetFullPath(dllFilePath), DateTimeOffset.UtcNow));
        }
        
        logger.LogWarning("File does not match the plugin filename pattern: {FilePath}", dllFilePath);
        return null;
    }
}
