using MelloSilveiraTools.Domain.Repositories;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Text.Json;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Persists plugin cache to a database table (<see cref="PluginCacheEntity"/>)
/// via <see cref="IRepository"/>.
/// </summary>
public class DatabasePluginCachePersistence(IRepository repository) : IPluginCachePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <inheritdoc/>
    public async Task SaveAsync(IAsyncEnumerable<PluginCacheEntry> entries, CancellationToken cancellationToken = default)
    {
        await foreach (PluginCacheEntry entry in entries.WithCancellation(cancellationToken))
            await repository.UpsertAsync(ToEntity(entry), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<PluginCacheEntry> LoadAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default) 
        => repository
            .GetAsync<PluginCacheEntity, PluginCacheFilter>(new() { PluginName = name, PluginVersion = version?.Name }, cancellationToken: cancellationToken)
            .Select(ToEntry);

    private static PluginCacheEntity ToEntity(PluginCacheEntry entry) => new()
    {
        PluginName = entry.Name,
        PluginVersion = entry.Version,
        StateType = entry.State.GetType().Name,
        StateJson = JsonSerializer.Serialize(entry.State, entry.State.GetType(), JsonOptions)
    };

    private static PluginCacheEntry ToEntry(PluginCacheEntity entity) => new(entity.PluginName, entity.PluginVersion, entity.StateType switch
    {
        nameof(RegisteredPlugin) => JsonSerializer.Deserialize<RegisteredPlugin>(entity.StateJson, JsonOptions)!,
        nameof(LoadedPlugin) => JsonSerializer.Deserialize<LoadedPlugin>(entity.StateJson, JsonOptions)!,
        nameof(DiscoveredPlugin) => JsonSerializer.Deserialize<DiscoveredPlugin>(entity.StateJson, JsonOptions)!,
        _ => throw new NotSupportedException($"Invalid state type: {entity.StateType}.")
    });
}
