using System.Diagnostics.CodeAnalysis;

namespace MelloSilveiraTools.Core.Providers.Dynamics;

/// <summary>
/// Provides dynamic service registration and resolution at runtime.
/// Used for services that cannot be registered statically in the DI container,
/// such as plugin-provided implementations discovered after application startup.
/// </summary>
public interface IDynamicServiceProvider
{
    /// <summary>
    /// Registers a service type with its implementation type for lazy instantiation.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="implementationType">The concrete implementation type.</param>
    /// <param name="parameters"></param>
    void Add(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, params object[] parameters);

    /// <summary>
    /// Registers a named instance directly.
    /// </summary>
    /// <param name="key">A unique key identifying the registration.</param>
    /// <param name="instance">The instance to register.</param>
    void AddKeyed(string key, object instance);

    /// <summary>
    /// Resolves a service by its type.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <returns>The resolved instance, or <see langword="null"/> when the service is not registered.</returns>
    object? GetService(Type serviceType);

    /// <summary>
    /// Resolves a named instance by its key.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="key">The registration key.</param>
    /// <returns>The resolved instance cast to <typeparamref name="T"/>, or <see langword="null"/> when the service is not registered.</returns>
    T? GetKeyedService<T>(string key) where T : class;
}
