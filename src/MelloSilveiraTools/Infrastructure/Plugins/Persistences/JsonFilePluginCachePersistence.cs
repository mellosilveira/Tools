using MelloSilveiraTools.Infrastructure.Plugins;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Text.Json;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Persists plugin cache to a JSON file in the plugins directory.
/// </summary>
public class JsonFilePluginCachePersistence(PluginSettings settings) : IPluginCachePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private string FilePath => Path.Combine(settings.Directory, "plugin-cache.json");

    /// <inheritdoc/>
    public async Task SaveAsync(IReadOnlyDictionary<string, PluginDescriptor> descriptors, IReadOnlyDictionary<string, PluginTypeInfo> states)
    {
        string directory = settings.Directory;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(new { descriptors, states }, JsonOptions);

        string tempPath = FilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
        File.Move(tempPath, FilePath, overwrite: true);
    }

    /// <inheritdoc/>
    public async Task<(Dictionary<string, PluginDescriptor> Descriptors, Dictionary<string, PluginTypeInfo> States)> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return ([], []);

        string json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        var descriptors = JsonSerializer.Deserialize<Dictionary<string, PluginDescriptor>>(doc.RootElement.GetProperty("descriptors").GetRawText());
        var states = JsonSerializer.Deserialize<Dictionary<string, PluginTypeInfo>>(doc.RootElement.GetProperty("states").GetRawText());
        return (descriptors ?? [], states ?? []);
    }
}
