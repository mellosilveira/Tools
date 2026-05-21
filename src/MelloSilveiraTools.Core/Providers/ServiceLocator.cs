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
    /// Resolves a required service. Looks up the dynamic provider first; falls back to the static
    /// provider when no dynamic registration is found. Throws if neither has the type.
    /// </summary>
    public object GetRequiredService(Type type) => dynamicServiceProvider.GetService(type) ?? serviceProvider.GetRequiredService(type);

    /// <summary>
    /// Resolves an optional service. Looks up the dynamic provider first; falls back to the static
    /// provider. Returns <see langword="null"/> when neither has the type.
    /// </summary>
    public object? GetService(Type type) => dynamicServiceProvider.GetService(type) ?? serviceProvider.GetService(type);

    /// <summary>
    /// Resolves a required service. Looks up the dynamic provider first; falls back to the static
    /// provider when no dynamic registration is found. Throws if neither has the type.
    /// </summary>
    public object GetRequiredKeyedService<T>(string key) where T : class => dynamicServiceProvider.GetKeyedService<T>(key) ?? serviceProvider.GetRequiredKeyedService<T>(key);

    /// <summary>
    /// Resolves an optional service. Looks up the dynamic provider first; falls back to the static
    /// provider. Returns <see langword="null"/> when neither has the type.
    /// </summary>
    public object? GetKeyedService<T>(string key) where T : class => dynamicServiceProvider.GetKeyedService<T>(key) ?? serviceProvider.GetKeyedService<T>(key);
}
