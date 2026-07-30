using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MelloSilveiraTools.Core;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Core package.
/// </summary>
public static class CoreDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers core services: settings, SMTP resilience pipeline, file logger and encryption service.
        /// </summary>
        /// <param name="encryptionSettings">Settings used by the encryption service.</param>
        /// <param name="smtpResiliencePipelineSettings">Settings that parameterize the resilience pipelines.</param>
        /// <param name="emailSettings">Settings used to send e-mails through an SMTP server.</param>
        /// <param name="loggerSettings">Settings used by logger service.</param>
        /// <param name="useDefaultLogger">Indicates if should use the default logger.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddCoreServices(EncryptionSettings? encryptionSettings = null,
            ResiliencePipelineSettings? smtpResiliencePipelineSettings = null,
            EmailSettings? emailSettings = null,
            LoggerSettings? loggerSettings = null,
            bool useDefaultLogger = true)
        {
            if (encryptionSettings is not null)
            {
                services
                    .AddSingleton(encryptionSettings)
                    .AddScoped<IEncryptionService, EncryptionService>();
            }

            if (smtpResiliencePipelineSettings is not null && emailSettings is not null)
            {
                services
                    .AddSingleton(emailSettings)
                    .AddSingleton(provider => new SmtpResiliencePipeline(provider.GetRequiredService<ILogger<SmtpResiliencePipeline>>(), smtpResiliencePipelineSettings))
                    .AddScoped<IEmailService, SmtpEmailService>();
            }

            if (useDefaultLogger)
            {
                services.AddCoreLogging(LoggerConfigurationExtensions.Create(), loggerSettings ?? new LoggerSettings());
            }

            return services
                .AddSingleton<IFileManager, FileManager>()
                // Register caching pipelines.
                .AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>()
                .AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>()
                // Register providers.
                .AddSingleton<ServiceLocator>()
                .AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        }

        public IServiceCollection AddCoreLogging(LoggerConfiguration loggerConfiguration, LoggerSettings loggerSettings)
        {
            var logger = loggerConfiguration
                .WriteToLocalFile(loggerSettings)
                .CreateLogger();

            return services
                .AddSingleton(loggerSettings)
                .AddLogging(loggingBuilder =>
                {
                    // Remove default console noise.
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(logger, dispose: true);
                });
        }
    }
}
