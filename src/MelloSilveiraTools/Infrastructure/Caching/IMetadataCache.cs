namespace MelloSilveiraTools.Infrastructure.Caching;

/// <summary>
/// Centralized cache for reflection-derived type metadata and other expensive computations.
/// Implementations may use in-memory storage or distributed cache (e.g. Redis).
/// </summary>
public interface IMetadataCache
{
    bool TryGet<T>(string key, out T? value);

    T Get<T>(string key);
    
    T GetOrAdd<T>(string key, Func<T> factory);

    void Add<T>(string key, T value);

    void Update<T>(string key, T value);

    void Remove(string key);

    void Clear();
}
