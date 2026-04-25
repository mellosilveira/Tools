using MelloSilveiraTools.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Context passed to plugin type processors to indicate whether registration should
/// target the static DI container (startup) or the dynamic provider (runtime).
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Services"/> and <see cref="DynamicProvider"/> is expected to be non-null:
/// when <see cref="IsStartup"/> is <see langword="true"/>, <see cref="Services"/> is non-null and
/// <see cref="DynamicProvider"/> is <see langword="null"/>; otherwise <see cref="DynamicProvider"/> is
/// non-null and <see cref="Services"/> is <see langword="null"/>.
/// Use the <see cref="ForStartup"/> and <see cref="ForRuntime"/> factory methods to construct
/// instances that satisfy this invariant.
/// </remarks>
public record PluginRegistrationContext
{
    /// <summary>
    /// The static DI container, populated only during application startup. <see langword="null"/> when
    /// this context targets runtime registration.
    /// </summary>
    public IServiceCollection? Services { get; init; }

    /// <summary>
    /// The dynamic service provider, populated only during runtime plugin registration. <see langword="null"/>
    /// when this context targets startup registration.
    /// </summary>
    public IDynamicServiceProvider? DynamicProvider { get; init; }

    /// <summary>
    /// Returns <see langword="true"/> when this context targets the static DI container (startup registration).
    /// In that case <see cref="Services"/> is guaranteed to be non-null.
    /// </summary>
    public bool IsStartup => Services is not null;

    /// <summary>
    /// Creates a context that targets the static DI container during application startup.
    /// </summary>
    public static PluginRegistrationContext ForStartup(IServiceCollection services) => new() { Services = services };

    /// <summary>
    /// Creates a context that targets the dynamic service provider for runtime plugin registration.
    /// </summary>
    public static PluginRegistrationContext ForRuntime(IDynamicServiceProvider dynamicProvider) => new() { DynamicProvider = dynamicProvider };
}
