using MelloSilveiraTools.Core.Logger;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Core;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Core package.
/// </summary>
public static class CoreDependencyInjection
{
    /// <summary>
    /// Registers core services: settings, SMTP resilience pipeline, file logger and encryption service.
    /// </summary>
    /// <param name="services">Service collection that receives the registrations.</param>
    /// <param name="encryptionSettings">Settings used by the encryption service.</param>
    /// <param name="resiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
    /// <param name="loggerSettings">Settings used by logger service.</param>
    /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services,
        EncryptionSettings encryptionSettings, ResiliencePipelineSettings resiliencePipelineSettings,
        LoggerSettings? loggerSettings = null)
        => services
            // Register settings.
            .AddSingleton(encryptionSettings)
            .AddSingleton(resiliencePipelineSettings)
            .AddSingleton(loggerSettings ?? new LoggerSettings())
            // Register resilience pipelines.
            .AddSingleton(provider => new SmtpResiliencePipeline(provider.GetRequiredService<ILogger>(), resiliencePipelineSettings))
            // Register logger.
            .AddSingleton<ILogger, LocalFileLogger>()
            // Register services.
            .AddScoped<IEncryptionService, EncryptionService>();
}
