using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Text.RegularExpressions;

namespace MelloSilveiraTools.Infrastructure.Plugins;

public class PluginFileProcessor(
    PluginCache cache,
    PluginSettings settings)
{
    private static readonly Regex PluginFileRegex = new(@"^(?<name>.+)\.v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$", RegexOptions.Compiled);
    private readonly string _pluginsDirectory = settings.Directory;
    private readonly string _loadedDirectory = Path.Combine(settings.Directory, "loaded");

    public IEnumerable<DiscoveredPlugin> Scan(string pluginName = "", PluginVersion? version = null)
    {
        foreach (string file in Directory.GetFiles(_pluginsDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"))
            yield return Parse(file);
    }

    public IEnumerable<DiscoveredPlugin> ScanLoaded(string pluginName = "", PluginVersion? version = null)
    {
        foreach (string file in Directory.GetFiles(_loadedDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"))
            yield return Parse(file);
    }

    public void MoveToMainFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        File.Move(plugin.FullPath, destination, overwrite: false);
    }

    /// <summary>Moves a plugin DLL from the plugins directory to the loaded/ subfolder.</summary>
    public void MoveToLoadedFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        File.Move(plugin.FullPath, destination, overwrite: false);
    }

    /// <summary>Replaces an existing DLL in the loaded/ subfolder with one from the plugins directory.</summary>
    public void ReplaceInLoadedFolder(DiscoveredPlugin plugin)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(plugin.FullPath, destination);
    }

    /// <summary>Checks whether a DLL with the same name and version exists in the loaded/ subfolder.</summary>
    public bool ExistsInLoadedFolder(DiscoveredPlugin plugin)
    {
        string loadedPath = Path.Combine(_loadedDirectory, Path.GetFileName(plugin.FullPath));
        return File.Exists(loadedPath);
    }

    /// <summary>
    /// Parses a DLL file path into a <see cref="DiscoveredPlugin"/>.
    /// Expected pattern: {name}.v{major}.{minor}.{patch}.dll.
    /// </summary>
    private DiscoveredPlugin Parse(string dllFilePath)
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

        throw new InvalidOperationException("File does not match the plugin filename pattern.");
    }
}
