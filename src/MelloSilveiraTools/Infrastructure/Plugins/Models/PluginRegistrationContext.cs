using MelloSilveiraTools.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Context passed to <see cref="IPluginTypeProcessor"/> to indicate whether registration should 
/// target the static DI container (startup) or the dynamic provider (runtime).
/// </summary>
public record PluginRegistrationContext
{
    /// <summary>
    /// Non-null during application startup. Used to register services in the static DI container.
    /// </summary>
    public IServiceCollection Services { get; init; }

    /// <summary>
    /// Non-null during runtime. Used to register services in the dynamic provider.
    /// </summary>
    public IDynamicServiceProvider DynamicProvider { get; init; }

    public bool IsStartup => Services is not null;

    public static PluginRegistrationContext ForStartup(IServiceCollection services) => new() { Services = services };
    public static PluginRegistrationContext ForRuntime(IDynamicServiceProvider dynamicProvider) => new() { DynamicProvider = dynamicProvider };
}
