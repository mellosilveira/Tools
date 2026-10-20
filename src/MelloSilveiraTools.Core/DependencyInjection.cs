using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.AwsS3;
using MelloSilveiraTools.Core.Managers.File.Providers.BlobStorage;
using MelloSilveiraTools.Core.Managers.File.Providers.Dfs;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.Providers;
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
        /// <param name="defaultResiliencePipelineSettings">Settings that parameterize the default resilience pipelines.</param>
        /// <param name="encryptionSettings">Settings used by the encryption service.</param>
        /// <param name="emailSettings">Settings used to send e-mails through an SMTP server.</param>
        /// <param name="fileStorageSettings">Settings used to configure storage services.</param>
        /// <param name="loggerSettings">Settings used by logger service.</param>
        /// <param name="useDefaultLogger">Indicates if should use the default logger.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddCoreServices(
            ResiliencePipelineSettings defaultResiliencePipelineSettings,
            EncryptionSettings? encryptionSettings = null,
            EmailSettings? emailSettings = null,
            FileStorageSettings? fileStorageSettings = null,
            LoggerSettings? loggerSettings = null,
            bool useDefaultLogger = true)
        {
            if (encryptionSettings is not null)
            {
                services
                    .AddSingleton(encryptionSettings)
                    .AddScoped<IEncryptionService, EncryptionService>();
            }

            if (emailSettings is not null)
            {
                services
                    .AddSingleton(emailSettings)
                    .AddSingleton(provider => new SmtpResiliencePipeline(provider.GetRequiredService<ILogger<SmtpResiliencePipeline>>(), emailSettings.ResiliencePipelineSettings ?? defaultResiliencePipelineSettings))
                    .AddScoped<IEmailService, SmtpEmailService>();
            }

            if (fileStorageSettings is not null)
            {
                if (fileStorageSettings.Local is not null)
                    services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, (_, _) => new LocalFileStorageService(fileStorageSettings.Local));

                if (fileStorageSettings.BlobStorage is not null)
                    services.AddKeyedSingleton<IFileStorageService>(StorageProvider.BlobStorage, (_, _) => new BlobStorageFileStorageService(fileStorageSettings.BlobStorage));

                if (fileStorageSettings.Dfs is not null)
                    services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Dfs, (_, _) => new DfsFileStorageService(fileStorageSettings.Dfs));

                if (fileStorageSettings.AwsS3 is not null)
                    services.AddKeyedSingleton<IFileStorageService>(
                        StorageProvider.AwsS3,
                        (provider, _) => new AwsS3FileStorageService(fileStorageSettings.AwsS3, provider.GetRequiredService<IFileStorageService>(), provider.GetRequiredService<ILogger<AwsS3FileStorageService>>()));
            }

            if (useDefaultLogger)
                services.AddCoreLogging(LoggerConfigurationExtensions.Create(), loggerSettings ?? new LoggerSettings());

            return services
                .AddSingleton<IFileStorageService>(_ => new LocalFileStorageService(new() { BaseDirectory = Path.Combine(Path.GetTempPath(), "storage") }))
                // Register caching pipelines.
                .AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>()
                .AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>()
                // Register providers.
                .AddSingleton<ServiceLocator>()
                .AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        }

        public IServiceCollection AddCoreLogging(LoggerConfiguration loggerConfiguration, LoggerSettings loggerSettings) => services
            .AddSingleton(loggerSettings)
            .AddLogging(loggingBuilder =>
            {
                // Remove default console noise.
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(loggerConfiguration.WriteToLocalFile(loggerSettings).CreateLogger(), dispose: true);
            });
    }
}
