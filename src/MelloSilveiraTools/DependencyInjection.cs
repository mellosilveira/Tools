using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools;

/// <summary>
/// Provides extension methods to dependency injection of Tools project.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the services of Tools project.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services)
    {
        return services
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>();
    }
}