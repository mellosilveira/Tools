namespace MelloSilveiraTools.Core.Caching;

/// <summary>
/// Generic two-level cache keyed by a <c>group</c> (level 1) and a <c>key</c> (level 2).
/// Implementations may use in-memory storage or a distributed backend (e.g. Redis).
/// </summary>
public interface ITwoLevelCache
{
    /// <summary>
    /// Attempts to retrieve a cached value of type <typeparamref name="T"/> for the given <paramref name="group"/> and <paramref name="key"/>.
    /// </summary>
    /// <returns><see langword="true"/> when an entry exists and is assignable to <typeparamref name="T"/>; otherwise <see langword="false"/>.</returns>
    bool TryGet<T>(string group, string key, out T? value);

    /// <summary>
    /// Returns the cached value for (<paramref name="group"/>, <paramref name="key"/>) or adds a new one produced by <paramref name="factory"/>.
    /// </summary>
    T GetOrAdd<T>(string group, string key, Func<T> factory);

    /// <summary>
    /// Stores <paramref name="value"/> under the given <paramref name="group"/> and <paramref name="key"/>, replacing any existing entry.
    /// </summary>
    void Set<T>(string group, string key, T value);

    /// <summary>
    /// Removes the entry associated with the given <paramref name="group"/> and <paramref name="key"/>, if any.
    /// </summary>
    void Remove(string group, string key);

    /// <summary>
    /// Removes every entry under the given <paramref name="group"/>.
    /// </summary>
    void Remove(string group);

    /// <summary>
    /// Enumerates all (group, key) pairs currently held in the cache.
    /// </summary>
    IEnumerable<(string Group, string Key)> GetKeys();

    /// <summary>
    /// Streams every entry currently held in the cache.
    /// </summary>
    IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams entries filtered by <paramref name="group"/> and/or <paramref name="key"/>.
    /// A <see langword="null"/> argument means "match all" for that level.
    /// </summary>
    IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(string? group, string? key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every entry from the cache.
    /// </summary>
    void Clear();
}
