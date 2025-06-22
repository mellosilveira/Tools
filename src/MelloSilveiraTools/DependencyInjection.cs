using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
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
    /// <param name="resiliencePipelineSettings"></param>
    /// <returns></returns>
    public static IServiceCollection AddToolsServices(this IServiceCollection services, ResiliencePipelineSettings resiliencePipelineSettings)
    {
        return services
            // Register settings.
            .AddSingleton(resiliencePipelineSettings)
            // Register resilience pipelines.
            .AddSingleton<DefaultResiliencePipeline>()
            .AddSingleton<PostgresResiliencePipeline>()
            // Register SQL providers.
            .AddSingleton<ISqlProvider, PostgresSqlProvider>();
    }
}