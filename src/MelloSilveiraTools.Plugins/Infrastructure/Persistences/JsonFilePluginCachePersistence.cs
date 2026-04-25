using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Persists plugin cache to a JSON file in the plugins directory.
/// Each entry is stored with a type discriminator so the correct
/// <see cref="DiscoveredPlugin"/> subtype is restored.
/// </summary>
/// <param name="settings">Plugin settings providing the directory in which the <c>plugin-cache.json</c> file is created and read.</param>
public class JsonFilePluginCachePersistence(PluginSettings settings) : IPluginCachePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string FilePath => Path.Combine(settings.Directory, "plugin-cache.json");

    /// <inheritdoc/>
    public async Task SaveAsync(IAsyncEnumerable<PluginCacheEntry> entries, CancellationToken cancellationToken = default)
    {
        string directory = settings.Directory;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string tempPath = FilePath + ".tmp";

        await using FileStream stream = File.Create(tempPath);
        await using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartArray();

        await foreach (PluginCacheEntry entry in entries.WithCancellation(cancellationToken))
            JsonSerializer.Serialize(writer, ToDto(entry), JsonOptions);

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        File.Move(tempPath, FilePath, overwrite: true);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<PluginCacheEntry> LoadAsync(
        string? name = null,
        PluginVersion? version = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath)) yield break;

        string json = await File.ReadAllTextAsync(FilePath, cancellationToken).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (JsonElement element in doc.RootElement.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            CacheEntryDto? dto = element.Deserialize<CacheEntryDto>(JsonOptions);
            if (dto is null) continue;
            if (!string.IsNullOrEmpty(name) && dto.Name != name) continue;
            if (version is not null && dto.Version != version.Value.Name) continue;

            PluginCacheEntry? entry = FromDto(dto);
            if (entry is not null)
                yield return entry;
        }
    }

    private static CacheEntryDto ToDto(PluginCacheEntry entry) => new(
        entry.Name,
        entry.Version,
        entry.State.GetType().Name,
        JsonSerializer.SerializeToElement(entry.State, entry.State.GetType(), JsonOptions));

    private static PluginCacheEntry? FromDto(CacheEntryDto dto)
    {
        DiscoveredPlugin? state = dto.StateType switch
        {
            nameof(RegisteredPlugin) => dto.State.Deserialize<RegisteredPlugin>(JsonOptions),
            nameof(LoadedPlugin)     => dto.State.Deserialize<LoadedPlugin>(JsonOptions),
            _                        => dto.State.Deserialize<DiscoveredPlugin>(JsonOptions)
        };

        return state is null ? null : new PluginCacheEntry(dto.Name, dto.Version, state);
    }

    private record CacheEntryDto(string Name, string Version, string StateType, JsonElement State);
}
