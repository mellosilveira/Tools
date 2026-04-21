namespace MelloSilveiraTools.Infrastructure.Caching;

/// <summary>
/// Generic two-level cache keyed by a <paramref name="group"/> (level 1) and a <paramref name="key"/> (level 2).
/// Implementations may use in-memory storage or a distributed backend (e.g. Redis).
/// </summary>
public interface ITwoLevelCache
{
    bool TryGet<T>(string group, string key, out T? value);

    T GetOrAdd<T>(string group, string key, Func<T> factory);

    void Set<T>(string group, string key, T value);

    void Remove(string group, string key);

    void Remove(string group);

    IEnumerable<(string Group, string Key)> GetKeys();

    IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(CancellationToken cancellationToken = default);

    void Clear();
}
