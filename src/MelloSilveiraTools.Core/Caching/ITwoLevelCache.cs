namespace MelloSilveiraTools.Core.Caching;

/// <summary>
/// Hierarchical two-level temporary storage contract (Group and Item Key).
/// Allows segmenting business information by domain context (such as tenant, department, or transaction type),
/// enabling ultra-fast reads as well as complete invalidation of an entire business group without affecting others.
/// </summary>
public interface ITwoLevelCache
{
    /// <summary>
    /// Attempts to retrieve a cached business value using its group and key identifiers.
    /// </summary>
    /// <typeparam name="T">Type of the stored business item.</typeparam>
    /// <param name="group">Business group or context category.</param>
    /// <param name="key">Unique item identifier within the specified group.</param>
    /// <param name="value">Retrieved item ready for consumption, if present.</param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    bool TryGet<T>(string group, string key, out T? value);

    /// <summary>
    /// Returns the cached item or calculates and registers it within the specified group if absent.
    /// </summary>
    /// <typeparam name="T">Type of the business item.</typeparam>
    /// <param name="group">Business group or context category.</param>
    /// <param name="key">Unique item identifier within the group.</param>
    /// <param name="factory">Action to compute or fetch the item when missing.</param>
    /// <returns>The business item ready for consumption.</returns>
    T GetOrAdd<T>(string group, string key, Func<T> factory);

    /// <summary>
    /// Stores or replaces a business item under the designated group and key.
    /// </summary>
    /// <typeparam name="T">Type of the business item.</typeparam>
    /// <param name="group">Business group or context category.</param>
    /// <param name="key">Unique item identifier within the group.</param>
    /// <param name="value">The business item to store.</param>
    void Set<T>(string group, string key, T value);

    /// <summary>
    /// Removes a specific business item from the designated group to force fresh data retrieval on subsequent calls.
    /// </summary>
    /// <param name="group">Business group containing the item.</param>
    /// <param name="key">Unique identifier of the item to remove.</param>
    void Remove(string group, string key);

    /// <summary>
    /// Atomically removes all items belonging to a business group (e.g. after a bulk update or tenant change).
    /// </summary>
    /// <param name="group">The business group to invalidate completely.</param>
    void Remove(string group);

    /// <summary>
    /// Enumerates all active (Group, Key) pairs currently held in memory.
    /// </summary>
    /// <returns>Pairs of group and key identifiers.</returns>
    IEnumerable<(string Group, string Key)> GetKeys();

    /// <summary>
    /// Streams all cached business records of a specific type on demand.
    /// </summary>
    /// <typeparam name="T">Type of the business items to stream.</typeparam>
    /// <param name="cancellationToken">Cancellation token to interrupt streaming.</param>
    /// <returns>An asynchronous stream of (Group, Key, Item) tuples.</returns>
    IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams all cached business items regardless of their specific type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to interrupt streaming.</param>
    /// <returns>An asynchronous stream of all cached objects.</returns>
    IAsyncEnumerable<(string Group, string Key, object Value)> StreamAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams cached business items filtered optionally by group and/or item key.
    /// </summary>
    /// <typeparam name="T">Type of the business items to stream.</typeparam>
    /// <param name="group">Specific group to filter by, or <see langword="null"/> to include all groups.</param>
    /// <param name="key">Specific key to filter by, or <see langword="null"/> to include all keys.</param>
    /// <param name="cancellationToken">Cancellation token to interrupt streaming.</param>
    /// <returns>Filtered asynchronous stream of matching entries.</returns>
    IAsyncEnumerable<(string Group, string Key, T Value)> StreamAll<T>(string? group, string? key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Empties all groups and entries from the two-level cache.
    /// </summary>
    void Clear();
}
