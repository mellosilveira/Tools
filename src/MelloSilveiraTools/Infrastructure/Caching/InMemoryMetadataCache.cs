using System.Collections.Concurrent;

namespace MelloSilveiraTools.Infrastructure.Caching;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IMetadataCache"/>.
/// Suitable for single-instance deployments. For distributed scenarios, replace
/// with a Redis-backed implementation registered in the DI container.
/// </summary>
public class InMemoryMetadataCache : IMetadataCache
{
    private readonly ConcurrentDictionary<string, object> _cache = [];

    /// <inheritdoc/>
    public bool TryGet<T>(string key, out T? value)
    {
        bool exist = _cache.TryGetValue(key, out object? obj);
        value = (T?)obj;
        return exist;
    }

    /// <inheritdoc/>
    public T Get<T>(string key) => (T)_cache[key];

    /// <inheritdoc/>
    public T GetOrAdd<T>(string key, Func<T> factory) => (T)_cache.GetOrAdd(key, _ => factory());

    /// <inheritdoc/>
    public void Add<T>(string key, T value) => _cache[key] = value;

    /// <inheritdoc/>
    public void Update<T>(string key, T value) => _cache[key] = value;

    /// <inheritdoc/>
    public void Remove(string key) => _cache.Remove(key, out _);

    /// <inheritdoc/>
    public void Clear() => _cache.Clear();
}
