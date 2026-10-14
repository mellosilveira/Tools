using MelloSilveiraTools.Core.Providers.Dynamics;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Core.Providers;

/// <summary>
/// Generic service locator that resolves dependencies by combining a dynamic plugin-aware
/// service provider with the application's static <see cref="IServiceProvider"/>.
/// </summary>
public class ServiceLocator(
    IDynamicServiceProvider dynamicServiceProvider,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Resolves a required service by its type parameter. Looks up the dynamic provider first; falls back to the static
    /// provider when no dynamic registration is found. Throws if neither has the type.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved instance of <typeparamref name="T"/>.</returns>
    public T GetRequiredService<T>() where T : class => dynamicServiceProvider.GetService<T>() ?? serviceProvider.GetRequiredService<T>();

    /// <summary>
    /// Resolves a required service. Looks up the dynamic provider first; falls back to the static
    /// provider when no dynamic registration is found. Throws if neither has the type.
    /// </summary>
    public object GetRequiredService(Type type) => dynamicServiceProvider.GetService(type) ?? serviceProvider.GetRequiredService(type);

    /// <summary>
    /// Resolves an optional service by its type parameter. Looks up the dynamic provider first; falls back to the static
    /// provider. Returns <see langword="null"/> when neither has the type.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved instance cast to <typeparamref name="T"/>, or <see langword="null"/> when neither has the type.</returns>
    public T? GetService<T>() where T : class => dynamicServiceProvider.GetService<T>() ?? serviceProvider.GetService<T>();

    /// <summary>
    /// Resolves an optional service. Looks up the dynamic provider first; falls back to the static
    /// provider. Returns <see langword="null"/> when neither has the type.
    /// </summary>
    public object? GetService(Type type) => dynamicServiceProvider.GetService(type) ?? serviceProvider.GetService(type);

    /// <summary>
    /// Resolves a required service. Looks up the dynamic provider first; falls back to the static
    /// provider when no dynamic registration is found. Throws if neither has the type.
    /// </summary>
    public T GetRequiredKeyedService<T>(object key) where T : class => dynamicServiceProvider.GetKeyedService<T>(key) ?? serviceProvider.GetRequiredKeyedService<T>(key);

    /// <summary>
    /// Resolves an optional service. Looks up the dynamic provider first; falls back to the static
    /// provider. Returns <see langword="null"/> when neither has the type.
    /// </summary>
    public T? GetKeyedService<T>(object key) where T : class => dynamicServiceProvider.GetKeyedService<T>(key) ?? serviceProvider.GetKeyedService<T>(key);
}
