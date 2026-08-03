using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Caching;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ITwoLevelCache"/>.
/// Suitable for single-instance deployments. For distributed scenarios, replace
/// with a Redis-backed implementation registered in the DI container.
/// </summary>
public class InMemoryTwoLevelCache : ITwoLevelCache
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _cache = new();

    /// <inheritdoc/>
    public bool TryGet<T>(string group, string key, out T? value)
    {
        if (_cache.TryGetValue(group, out var byKey)
            && byKey.TryGetValue(key, out var obj)
            && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public T GetOrAdd<T>(string group, string key, Func<T> factory) => (T)_cache.GetOrAdd(group, static _ => new()).GetOrAdd(key, _ => factory()!);

    /// <inheritdoc/>
    public void Set<T>(string group, string key, T value) => _cache.GetOrAdd(group, static _ => new())[key] = value!;

    /// <inheritdoc/>
    public void Remove(string group, string key)
    {
        if (_cache.TryGetValue(group, out var byKey))
        {
            byKey.Remove(key, out _);
            if (byKey.IsEmpty)
                _cache.Remove(group, out _);
        }
    }

    /// <inheritdoc/>
    public void Remove(string group) => _cache.Remove(group, out _);

    /// <inheritdoc/>
    public IEnumerable<(string Group, string Key)> GetKeys() => _cache.SelectMany(kvp => kvp.Value.Keys.Select(k => (kvp.Key, k)));

    /// <inheritdoc/>
    public IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(CancellationToken cancellationToken = default) => StreamAll<T>(null, null, cancellationToken);
    
    /// <inheritdoc/>
    public IAsyncEnumerable<(string Group, string Key, object Value)> StreamAll(CancellationToken cancellationToken = default) => StreamAll<object>(null, null, cancellationToken);

    /// <inheritdoc/>
    public async IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(
        string? group,
        string? key,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var (g, byKey) in _cache)
        {
            if (!string.IsNullOrWhiteSpace(group) && g != group) 
                continue;

            foreach (var (k, obj) in byKey)
            {
                if (!string.IsNullOrWhiteSpace(key) && k != key)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();

                if (obj is T typed)
                    yield return (g, k, typed);
            }
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Clear() => _cache.Clear();
}
