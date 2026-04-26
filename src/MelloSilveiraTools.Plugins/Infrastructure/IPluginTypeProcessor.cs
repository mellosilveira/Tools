using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// Domain-specific processor for a plugin type. Handles DI registration logic
/// that differs between startup (static IServiceCollection) and runtime (IDynamicServiceProvider).
/// </summary>
public interface IPluginTypeProcessor
{
    /// <summary>
    /// The base type (interface or class) that this processor knows how to register.
    /// </summary>
    Type ProcessableType { get; }

    /// <summary>
    /// Processes a plugin for service registration based on the given context.
    /// </summary>
    void Process(Type type, PluginRegistrationContext context);
}
