using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace MelloSilveiraTools.Plugins.Infrastructure.Providers;

/// <summary>
/// Thread-safe implementation of <see cref="IDynamicServiceProvider"/> using
/// <see cref="Lazy{T}"/> for deferred instantiation and <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// for thread-safe storage.
/// </summary>
public class DynamicServiceProvider(IServiceProvider serviceProvider) : IDynamicServiceProvider
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ConcurrentDictionary<Type, Lazy<object>> _serviceInstances = [];
    private readonly ConcurrentDictionary<string, object> _keyedInstances = [];

    /// <inheritdoc/>
    public void Add(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        _serviceInstances[serviceType] = new Lazy<object>(() => ActivatorUtilities.CreateInstance(_serviceProvider, implementationType));
    }

    /// <inheritdoc/>
    public void AddKeyed(string key, object instance)
    {
        _keyedInstances[key] = instance;
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        return _serviceInstances.TryGetValue(serviceType, out Lazy<object>? lazy) ? lazy.Value : null;
    }

    /// <inheritdoc/>
    public T? GetKeyed<T>(string key) where T : class
    {
        return _keyedInstances.TryGetValue(key, out object? instance) ? (T)instance : null;
    }
}
