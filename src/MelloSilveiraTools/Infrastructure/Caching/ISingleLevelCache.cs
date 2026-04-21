namespace MelloSilveiraTools.Infrastructure.Caching;

/// <summary>
/// Generic single-level cache keyed by a <paramref name="key"/>.
/// Implementations may use in-memory storage or a distributed backend (e.g. Redis).
/// </summary>
public interface ISingleLevelCache
{
    bool TryGet<T>(string key, out T? value);

    T GetOrAdd<T>(string key, Func<T> factory);

    void Set<T>(string key, T value);

    void Remove(string key);

    IEnumerable<string> GetKeys();

    void Clear();
}
