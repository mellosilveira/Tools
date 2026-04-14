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

    public IEnumerable<PluginDescriptor> Scan(string pluginName = "", PluginVersion? version = null)
    {
        foreach (string file in Directory.GetFiles(_pluginsDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"))
            yield return ParseDescriptor(file);
    }

    public IEnumerable<PluginDescriptor> ScanLoaded(string pluginName = "", PluginVersion? version = null)
    {
        foreach (string file in Directory.GetFiles(_loadedDirectory, $"{pluginName}{version?.Name ?? string.Empty}*.dll"))
            yield return ParseDescriptor(file);
    }

    public void MoveToMainFolder(PluginDescriptor descriptor)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(descriptor.FullPath));
        File.Move(descriptor.FullPath, destination, overwrite: false);
    }

    /// <summary>
    /// Moves a plugin DLL from the plugins directory to the loaded/ subfolder.
    /// </summary>
    public void MoveToLoadedFolder(PluginDescriptor descriptor)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(descriptor.FullPath));
        File.Move(descriptor.FullPath, destination, overwrite: false);
    }

    /// <summary>
    /// Replaces an existing DLL in the loaded/ subfolder with one from the plugins directory.
    /// </summary>
    public void ReplaceInLoadedFolder(PluginDescriptor descriptor)
    {
        string destination = Path.Combine(_loadedDirectory, Path.GetFileName(descriptor.FullPath));

        if (File.Exists(destination))
            File.Delete(destination);

        File.Move(descriptor.FullPath, destination);
    }

    /// <summary>
    /// Checks whether a DLL with the same name and version exists in the loaded/ subfolder.
    /// </summary>
    public bool ExistsInLoadedFolder(PluginDescriptor descriptor)
    {
        string loadedPath = Path.Combine(_loadedDirectory, Path.GetFileName(descriptor.FullPath));
        return File.Exists(loadedPath);
    }

    /// <summary>
    /// Parses a DLL file path into a <see cref="PluginDescriptor"/>.
    /// Expected pattern: {name}.v{major}.{minor}.{patch}.dll.
    /// Falls back to version 0.0.0 if pattern does not match.
    /// </summary>
    private PluginDescriptor ParseDescriptor(string dllFilePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(dllFilePath);
        Match match = PluginFileRegex.Match(fileName);
        if (match.Success)
        {
            var name = match.Groups["name"].Value;
            var version = new PluginVersion(
                int.Parse(match.Groups["major"].Value),
                int.Parse(match.Groups["minor"].Value),
                int.Parse(match.Groups["patch"].Value));

            PluginDescriptor descriptor = new(name, version, Path.GetFullPath(dllFilePath), DateTimeOffset.UtcNow);
            cache.Add(name, version, descriptor);
            return descriptor;
        }

        throw new InvalidOperationException("File does not match to plugin pattern.");
    }
}
