using MelloSilveiraTools.Infrastructure.Caching;
using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Collections.Concurrent;

namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Multi-stage, two-level cache for the plugin processing pipeline.
/// Level 1: plugin name (<see langword="string"/>). Level 2: <see cref="PluginVersion"/> struct.
/// All hot-path lookups are O(1).
/// Generic processed-type lists are stored in the underlying <see cref="IMetadataCache"/> because
/// their value type is open — only their keys are tracked here for cleanup.
/// </summary>
// TODO: PENSAR NUMA ESTRUTURA MELHOR DE CACHE COM DOIS NÍVEIS DE CHAVE
public class PluginCache(ILogger logger, IMetadataCache cache)
{
    private readonly ConcurrentDictionary<string, bool> _registeredNames = new();

    public void Add(string name, PluginVersion version, PluginBaseInfo pluginBaseInfo)
    {
        _registeredNames.TryAdd(name, true);
        Add<PluginBaseInfo>(GetDescriptorKey(name), version, pluginBaseInfo);
    }

    public PluginAssemblyInfo GetOrAdd(string name, PluginVersion version, Func<PluginAssemblyInfo> factory)
    {
        _registeredNames.TryAdd(name, true);
        var assemblyInfo = GetOrAdd<PluginAssemblyInfo>(GetAssemblyInfoKey(name), version, factory);

        try
        {
            // Since the assembly info was already saved on cache, we can remove descriptor from cache, improving cache usage.
            Remove(GetDescriptorKey(name), version);
        }
        catch (Exception ex)
        {
            Dictionary<string, object> additionalData = new()
            {
                { "PluginName", name },
                { "PluginVersion", version }
            };
            logger.Warn("Failed to remove plugin descriptor from cache.", ex, additionalData);
        }

        return assemblyInfo;
    }

    public PluginInfo GetOrAdd(string name, PluginVersion version, Func<PluginInfo> factory)
    {
        _registeredNames.TryAdd(name, true);
        var state = GetOrAdd<PluginInfo>(GetStateKey(name), version, factory);

        try
        {
            // Since the state was already saved on cache, we can remove assembly info from cache, improving cache usage.
            Remove(GetAssemblyInfoKey(name), version);
        }
        catch (Exception ex)
        {
            Dictionary<string, object> additionalData = new()
            {
                { "PluginName", name },
                { "PluginVersion", version }
            };
            logger.Warn("Failed to remove plugin descriptor from cache.", ex, additionalData);
        }

        return state;
    }

    public bool TryGetPluginState(string name, PluginVersion version, out PluginInfo state)
        => TryGet(GetStateKey(name), version, out state);

    public void UpdateState(string name, PluginVersion version, PluginInfo state)
    {
        var key = GetStateKey(name);

        var statesPerVersion = cache.Get<ConcurrentDictionary<string, PluginInfo>>(key);
        statesPerVersion[version.Name] = state;

        cache.Update(key, statesPerVersion);
    }

    public void Clear() => cache.Clear();

    /// <summary>
    /// Clears all cache entries for the given plugin name.
    /// When <paramref name="version"/> is <see langword="null"/>, all versions are removed;
    /// otherwise only the specified version is evicted from each cache stage.
    /// </summary>
    public void Clear(string name, PluginVersion? version)
    {
        if (version is null)
        {
            cache.Remove(GetDescriptorKey(name));
            cache.Remove(GetAssemblyInfoKey(name));
            cache.Remove(GetStateKey(name));
            _registeredNames.Remove(name, out _);
            return;
        }

        RemoveVersioned<PluginBaseInfo>(GetDescriptorKey(name), version.Value);
        RemoveVersioned<PluginAssemblyInfo>(GetAssemblyInfoKey(name), version.Value);
        RemoveVersioned<PluginInfo>(GetStateKey(name), version.Value);
    }

    /// <summary>
    /// Returns all <see cref="PluginBaseInfo"/>, <see cref="PluginAssemblyInfo"/> and
    /// <see cref="PluginInfo"/> entries currently held across all cache stages.
    /// </summary>
    public (IReadOnlyList<PluginBaseInfo> Descriptors, IReadOnlyList<PluginAssemblyInfo> AssemblyInfos, IReadOnlyList<PluginInfo> PluginInfos) GetAll()
    {
        var descriptors = new List<PluginBaseInfo>();
        var assemblyInfos = new List<PluginAssemblyInfo>();
        var pluginInfos = new List<PluginInfo>();

        foreach (var name in _registeredNames.Keys)
        {
            if (cache.TryGet(GetDescriptorKey(name), out ConcurrentDictionary<string, PluginBaseInfo>? descriptorsByVersion))
                descriptors.AddRange(descriptorsByVersion!.Values);

            if (cache.TryGet(GetAssemblyInfoKey(name), out ConcurrentDictionary<string, PluginAssemblyInfo>? assemblyInfosByVersion))
                assemblyInfos.AddRange(assemblyInfosByVersion!.Values);

            if (cache.TryGet(GetStateKey(name), out ConcurrentDictionary<string, PluginInfo>? infosByVersion))
                pluginInfos.AddRange(infosByVersion!.Values);
        }

        return (descriptors, assemblyInfos, pluginInfos);
    }

    private void Add<T>(string name, PluginVersion version, T value)
    {
        var pluginValue = new ConcurrentDictionary<string, T>();
        pluginValue[version.Name] = value;

        cache.Add(name, pluginValue);
    }

    private T GetOrAdd<T>(string key, PluginVersion version, Func<T> factory)
        => cache
            .GetOrAdd<ConcurrentDictionary<string, T>>(key, static () => new())
            .GetOrAdd(version.Name, _ => factory());

    private bool TryGet<T>(string name, PluginVersion version, out T? value)
    {
        if (cache.TryGet(GetStateKey(name), out ConcurrentDictionary<string, T>? cachePerVersion))
        {
            return cachePerVersion!.TryGetValue(version.Name, out value);
        }

        value = default;
        return false;
    }

    private void Remove(string key, PluginVersion version)
    {
        if (cache.TryGet(key, out ConcurrentDictionary<string, object>? cachePerVersion))
        {
            cachePerVersion!.Remove(version.Name, out _);

            if (cachePerVersion!.IsEmpty)
                cache.Remove(key);
        }
    }

    private void RemoveVersioned<T>(string key, PluginVersion version)
    {
        if (cache.TryGet(key, out ConcurrentDictionary<string, T>? cachePerVersion))
        {
            cachePerVersion!.Remove(version.Name, out _);

            if (cachePerVersion.IsEmpty)
                cache.Remove(key);
        }
    }

    private static string GetDescriptorKey(string name)
    {
        const string prefix = "Plugin:Descriptor:";
        return $"{prefix}{name}";
    }

    private static string GetAssemblyInfoKey(string name)
    {
        const string prefix = "Plugin:AssemblyInfo:";
        return $"{prefix}{name}";
    }

    private static string GetStateKey(string name)
    {
        const string prefix = "Plugin:State:";
        return $"{prefix}{name}";
    }


    //// Two-level registries — name → version → value.
    //private readonly ConcurrentDictionary<string, ConcurrentDictionary<PluginVersion, PluginDescriptor>> _descriptors = new();
    //private readonly ConcurrentDictionary<string, ConcurrentDictionary<PluginVersion, PluginAssemblyInfo>> _assemblyInfos = new();
    //private readonly ConcurrentDictionary<string, ConcurrentDictionary<PluginVersion, PluginTypeInfo>> _states = new();

    //// Tracks ITypeMetadataCache keys used for generic processed-type lists, for cleanup only.
    //private readonly ConcurrentDictionary<string, ConcurrentDictionary<PluginVersion, bool>> _processedKeys = new();

    //// ── Private helpers ────────────────────────────────────────────────────────

    ///// <summary>Composes the ITypeMetadataCache key for a processed-types entry.</summary>
    //private static string ProcessedCacheKey(string name, PluginVersion version)
    //    => string.Concat("Plugin:Processed:", name, ":", version.ToString());

    ///// <summary>Composes the flat persistence key "{name}.v{version}" used only in GetAll* enumerations.</summary>
    //private static string FlatKey(string name, PluginVersion version)
    //    => string.Concat(name, ".v", version.ToString());

    //// ── Descriptors ────────────────────────────────────────────────────────────

    ///// <summary>O(1) name lookup. Returns the entry for the first registered version.</summary>
    //public bool TryGetDescriptorByName(string name, out PluginDescriptor descriptor)
    //{
    //    descriptor = null;
    //    if (!_descriptors.TryGetValue(name, out var versions) || versions.IsEmpty)
    //        return false;
    //    descriptor = versions.Values.First();
    //    return true;
    //}

    //public IReadOnlyDictionary<string, PluginDescriptor> GetAllDescriptors()
    //{
    //    var result = new Dictionary<string, PluginDescriptor>();
    //    foreach (var (name, versions) in _descriptors)
    //        foreach (var (version, descriptor) in versions)
    //            result[FlatKey(name, version)] = descriptor;
    //    return result;
    //}

    //// ── Assembly infos ─────────────────────────────────────────────────────────

    ///// <summary>O(1) name lookup. Returns the entry for the first registered version.</summary>
    //public bool TryGetAssemblyInfoByName(string name, out PluginAssemblyInfo assemblyInfo)
    //{
    //    assemblyInfo = null;
    //    if (!_assemblyInfos.TryGetValue(name, out var versions) || versions.IsEmpty)
    //        return false;
    //    assemblyInfo = versions.Values.First();
    //    return true;
    //}

    //public IReadOnlyDictionary<string, PluginAssemblyInfo> GetAllAssemblyInfos()
    //{
    //    var result = new Dictionary<string, PluginAssemblyInfo>();
    //    foreach (var (name, versions) in _assemblyInfos)
    //        foreach (var (version, info) in versions)
    //            result[FlatKey(name, version)] = info;
    //    return result;
    //}

    //// ── Processed types ────────────────────────────────────────────────────────

    //public IReadOnlyList<TPlugin> GetOrAddProcessedTypes<TPlugin>(string name, PluginVersion version, Func<IReadOnlyList<TPlugin>> factory)
    //    where TPlugin : IPlugin
    //{
    //    IReadOnlyList<TPlugin> types = cache.GetOrAdd(ProcessedCacheKey(name, version), factory);
    //    _processedKeys
    //        .GetOrAdd(name, static _ => new())
    //        .TryAdd(version, true);

    //    return types;
    //}

    //// ── States ─────────────────────────────────────────────────────────────────

    //public IReadOnlyDictionary<string, PluginTypeInfo> GetAllStates()
    //{
    //    var result = new Dictionary<string, PluginTypeInfo>();
    //    foreach (var (name, versions) in _states)
    //        foreach (var (version, state) in versions)
    //            result[FlatKey(name, version)] = state;
    //    return result;
    //}

    ///// <summary>
    ///// Clears the cache from the specified stage onward (cascading).
    ///// State is never cleared by this method — use <see cref="ClearAll"/> for that.
    ///// </summary>
    //public void Clear(CacheStage stage)
    //{
    //    if (stage <= CacheStage.Discovery)
    //        _descriptors.Clear();

    //    if (stage <= CacheStage.Assembly)
    //        _assemblyInfos.Clear();

    //    if (stage <= CacheStage.Processed)
    //    {
    //        foreach (var (name, versions) in _processedKeys)
    //            foreach (var (version, _) in versions)
    //                cache.Remove(ProcessedCacheKey(name, version));
    //        _processedKeys.Clear();
    //    }
    //}

    ///// <summary>Clears all stages, including state.</summary>
    //public void ClearAll()
    //{
    //    Clear(CacheStage.Discovery);
    //    _states.Clear();
    //}

    ///// <summary>
    ///// Restores previously persisted state. Requires descriptors to resolve name and version
    ///// without re-parsing the flat key.
    ///// </summary>
    //public void RestoreFrom(Dictionary<string, PluginDescriptor> descriptors, Dictionary<string, PluginTypeInfo> states)
    //{
    //    foreach (var (key, persisted) in states)
    //    {
    //        if (!descriptors.TryGetValue(key, out PluginDescriptor descriptor))
    //            continue;

    //        PluginTypeInfo state = GetOrCreateState(descriptor.Name, descriptor.Version);
    //        foreach (var (typeName, loaded) in persisted.TypeLoadedStatus)
    //            state.TypeLoadedStatus[typeName] = loaded;
    //        state.LoadedAt = persisted.LoadedAt;
    //    }
    //}
}
