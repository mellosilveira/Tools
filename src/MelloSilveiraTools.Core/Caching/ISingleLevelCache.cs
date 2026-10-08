namespace MelloSilveiraTools.Core.Caching;

/// <summary>
/// Fast temporary in-memory storage contract for frequently referenced business information.
/// Dramatically reduces customer wait times and cuts repetitive database queries, lowering infrastructure load and cost.
/// </summary>
public interface ISingleLevelCache
{
    /// <summary>
    /// Attempts to retrieve a cached business value using its unique identifier.
    /// </summary>
    /// <typeparam name="T">Type of the stored business item.</typeparam>
    /// <param name="key">Unique identifier of the business item.</param>
    /// <param name="value">The retrieved business item, if present.</param>
    /// <returns><see langword="true"/> if the item was found and ready for immediate consumption; otherwise <see langword="false"/>.</returns>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Retrieves the cached item or computes and saves it if absent, avoiding duplicate calculations.
    /// </summary>
    /// <typeparam name="T">Type of the business item.</typeparam>
    /// <param name="key">Unique identifier of the business item.</param>
    /// <param name="factory">Action that calculates or fetches the item when not present in storage.</param>
    /// <returns>The business item ready for consumption.</returns>
    T GetOrAdd<T>(string key, Func<T> factory);

    /// <summary>
    /// Stores or replaces a business item under the given identifier for future fast lookups.
    /// </summary>
    /// <typeparam name="T">Type of the business item.</typeparam>
    /// <param name="key">Unique identifier of the business item.</param>
    /// <param name="value">The business item to store.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes a specific business item from temporary storage, ensuring subsequent operations fetch fresh data.
    /// </summary>
    /// <param name="key">Unique identifier of the item to remove.</param>
    void Remove(string key);

    /// <summary>
    /// Returns the collection of all item identifiers currently stored in memory.
    /// </summary>
    /// <returns>List of currently active cache keys.</returns>
    IEnumerable<string> GetKeys();

    /// <summary>
    /// Empties all temporary storage entries, useful during system synchronizations or bulk data updates.
    /// </summary>
    void Clear();
}
