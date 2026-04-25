using System.Collections.Concurrent;

namespace MelloSilveiraTools.Core.Infrastructure.Caching;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ISingleLevelCache"/>.
/// Suitable for single-instance deployments. For distributed scenarios, replace
/// with a Redis-backed implementation registered in the DI container.
/// </summary>
public class InMemorySingleLevelCache : ISingleLevelCache
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    /// <inheritdoc/>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public T GetOrAdd<T>(string key, Func<T> factory)
        => (T)_cache.GetOrAdd(key, _ => factory()!);

    /// <inheritdoc/>
    public void Set<T>(string key, T value) => _cache[key] = value!;

    /// <inheritdoc/>
    public void Remove(string key) => _cache.Remove(key, out _);

    /// <inheritdoc/>
    public IEnumerable<string> GetKeys() => _cache.Keys;

    /// <inheritdoc/>
    public void Clear() => _cache.Clear();
}
