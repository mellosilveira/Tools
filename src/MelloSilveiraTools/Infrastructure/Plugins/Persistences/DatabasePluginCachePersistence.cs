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
    public IAsyncEnumerable<PluginCacheEntry> LoadAsync(
        string name = "",
        PluginVersion? version = null,
        CancellationToken cancellationToken = default)
    {
        PluginCacheFilter filter = new()
        {
            PluginName    = string.IsNullOrEmpty(name) ? null : name,
            PluginVersion = version?.Name
        };

        return repository
            .GetAsync<PluginCacheEntity, PluginCacheFilter>(filter, cancellationToken: cancellationToken)
            .Select(ToEntry)
            .Where(static entry => entry is not null)
            .Select(static entry => entry!);
    }

    private static PluginCacheEntity ToEntity(PluginCacheEntry entry) => new()
    {
        PluginName    = entry.Name,
        PluginVersion = entry.Version,
        StateType     = entry.State.GetType().Name,
        StateJson     = JsonSerializer.Serialize(entry.State, entry.State.GetType(), JsonOptions)
    };

    private static PluginCacheEntry? ToEntry(PluginCacheEntity entity)
    {
        DiscoveredPlugin? state = entity.StateType switch
        {
            nameof(RegisteredPlugin) => JsonSerializer.Deserialize<RegisteredPlugin>(entity.StateJson, JsonOptions),
            nameof(LoadedPlugin)     => JsonSerializer.Deserialize<LoadedPlugin>(entity.StateJson, JsonOptions),
            _                        => JsonSerializer.Deserialize<DiscoveredPlugin>(entity.StateJson, JsonOptions)
        };

        return state is null ? null : new PluginCacheEntry(entity.PluginName, entity.PluginVersion, state);
    }
}
