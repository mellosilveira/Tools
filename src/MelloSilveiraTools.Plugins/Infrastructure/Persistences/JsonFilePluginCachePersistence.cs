using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Persists plugin cache to a JSON file using the configured storage service.
/// Each entry is stored with a type discriminator so the correct
/// <see cref="DiscoveredPlugin"/> subtype is restored.
/// </summary>
/// <param name="fileStorageService">The file storage service used to read and write the cache file.</param>
/// <param name="settings">Plugin settings providing the directory or context in which the <c>plugin-cache.json</c> file is created and read.</param>
public class JsonFilePluginCachePersistence(
    IFileStorageService fileStorageService,
    PluginSettings settings) : IPluginCachePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new TypeJsonConverter() }
    };

    private const string FileName = "plugin-cache.json";

    private string Location => fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Cache, FileName);

    /// <inheritdoc/>
    public async Task SaveAsync(IAsyncEnumerable<PluginCacheEntry> entries, CancellationToken cancellationToken = default)
    {
        await using Stream stream = await fileStorageService.OpenWriteAsync(Location, cancellationToken).ConfigureAwait(false);
        await using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartArray();

            await foreach (PluginCacheEntry entry in entries.WithCancellation(cancellationToken))
            {
                JsonSerializer.Serialize(writer, ToDto(entry), JsonOptions);
            }

            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<PluginCacheEntry> LoadAsync(
        string? name = null,
        PluginVersion? version = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool exists = await fileStorageService.ExistsAsync(Location, cancellationToken).ConfigureAwait(false);
        if (!exists)
            yield break;

        await using Stream stream = await fileStorageService.OpenReadAsync(Location, cancellationToken).ConfigureAwait(false);
        if (stream.CanSeek && stream.Length == 0)
            yield break;

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                yield break;

            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                CacheEntryDto? dto = element.Deserialize<CacheEntryDto>(JsonOptions);
                if (dto is null)
                    continue;

                if (!string.IsNullOrEmpty(name) && dto.Name != name)
                    continue;

                if (version is not null && dto.Version != version.Value.Name)
                    continue;

                PluginCacheEntry? entry = FromDto(dto);
                if (entry is not null)
                    yield return entry;
            }
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
            nameof(LoadedPlugin) => dto.State.Deserialize<LoadedPlugin>(JsonOptions),
            _ => dto.State.Deserialize<DiscoveredPlugin>(JsonOptions)
        };

        return state is null ? null : new PluginCacheEntry(dto.Name, dto.Version, state);
    }

    private record CacheEntryDto(string Name, string Version, string StateType, JsonElement State);
}
