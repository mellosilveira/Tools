namespace MelloSilveiraTools.Core.Infrastructure.Caching;

/// <summary>
/// Generic single-level cache keyed by a key.
/// Implementations may use in-memory storage or a distributed backend (e.g. Redis).
/// </summary>
public interface ISingleLevelCache
{
    /// <summary>
    /// Attempts to retrieve a cached value of type <typeparamref name="T"/> for the given <paramref name="key"/>.
    /// </summary>
    /// <returns><see langword="true"/> when an entry exists and is assignable to <typeparamref name="T"/>; otherwise <see langword="false"/>.</returns>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Returns the cached value for <paramref name="key"/> or adds a new one produced by <paramref name="factory"/>.
    /// </summary>
    T GetOrAdd<T>(string key, Func<T> factory);

    /// <summary>
    /// Stores <paramref name="value"/> under the given <paramref name="key"/>, replacing any existing entry.
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes the entry associated with <paramref name="key"/>, if any.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Enumerates all keys currently held in the cache.
    /// </summary>
    IEnumerable<string> GetKeys();

    /// <summary>
    /// Removes every entry from the cache.
    /// </summary>
    void Clear();
}
