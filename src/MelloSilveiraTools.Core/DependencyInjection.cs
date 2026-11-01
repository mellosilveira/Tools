using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;
using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.AwsS3;
using MelloSilveiraTools.Core.Managers.File.Providers.BlobStorage;
using MelloSilveiraTools.Core.Managers.File.Providers.Dfs;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Channels;

namespace MelloSilveiraTools.Core;

/// <summary>
/// Provides extension methods to register services from the MelloSilveiraTools.Core package.
/// </summary>
public static class CoreDependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers core services: settings, SMTP resilience pipeline, file storage, logging, and encryption service.
        /// When <paramref name="loggerSettings"/> is null or specifies no sinks, Microsoft default console logging is enabled.
        /// When file or database sink settings are provided, Serilog is configured accordingly.
        /// </summary>
        /// <param name="defaultResiliencePipelineSettings">Settings that parameterize the default resilience pipelines.</param>
        /// <param name="encryptionSettings">Settings used by the encryption service.</param>
        /// <param name="emailSettings">Settings used to send e-mails through an SMTP server.</param>
        /// <param name="fileStorageSettings">Settings used to configure storage services.</param>
        /// <param name="loggerSettings">Settings used by the logging service.</param>
        /// <param name="pipelineProgressNotifierSettings">Mandatory configuration settings when real-time pipeline notifications are enabled.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddCoreServices(
            ResiliencePipelineSettings defaultResiliencePipelineSettings,
            EncryptionSettings? encryptionSettings = null,
            EmailSettings? emailSettings = null,
            FileStorageSettings? fileStorageSettings = null,
            LoggerSettings? loggerSettings = null,
            PipelineProgressNotifierSettings? pipelineProgressNotifierSettings = null)
        {
            services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<PipelineFactory>();

            if (pipelineProgressNotifierSettings is not null)
            {
                services
                    .AddSingleton(pipelineProgressNotifierSettings)
                    .AddSingleton<ChannelPipelineProgressNotifier>()
                    .AddSingleton<IPipelineProgressNotifier>(provider => provider.GetRequiredService<ChannelPipelineProgressNotifier>());
            }
            else
            {
                services.AddSingleton<IPipelineProgressNotifier>(NullPipelineProgressNotifier.Instance);
            }

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
                    services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, (provider, _) => new LocalFileStorageService(provider.GetRequiredService<TimeProvider>(), fileStorageSettings.Local));

                if (fileStorageSettings.BlobStorage is not null)
                    services
                        .AddSingleton(provider => new BlobStorageResiliencePipeline(provider.GetRequiredService<ILogger<BlobStorageResiliencePipeline>>(), fileStorageSettings.BlobStorage.ResiliencePipelineSettings ?? defaultResiliencePipelineSettings))
                        .AddKeyedSingleton<IFileStorageService>(
                            StorageProvider.BlobStorage,
                            (provider, _) => new BlobStorageFileStorageService(
                                provider.GetRequiredService<ILogger<BlobStorageFileStorageService>>(),
                                provider.GetRequiredService<IFileStorageService>(),
                                CreateBlobContainerClient(fileStorageSettings.BlobStorage),
                                fileStorageSettings.BlobStorage,
                                provider.GetRequiredService<BlobStorageResiliencePipeline>(),
                                provider.GetRequiredService<TimeProvider>()));

                if (fileStorageSettings.Dfs is not null)
                {
                    ValidateDfsStorageSettings(fileStorageSettings.Dfs);
                    services.AddKeyedSingleton<IFileStorageService>(
                        StorageProvider.Dfs,
                        (provider, _) => new DfsFileStorageService(provider.GetRequiredService<TimeProvider>(), fileStorageSettings.Dfs));
                }

                if (fileStorageSettings.AwsS3 is not null)
                    services
                        .AddSingleton(provider => new AwsS3ResiliencePipeline(provider.GetRequiredService<ILogger<AwsS3ResiliencePipeline>>(), fileStorageSettings.AwsS3.ResiliencePipelineSettings ?? defaultResiliencePipelineSettings))
                        .AddKeyedSingleton<IFileStorageService>(
                            StorageProvider.AwsS3,
                            (provider, _) => new AwsS3FileStorageService(
                                provider.GetRequiredService<ILogger<AwsS3FileStorageService>>(),
                                provider.GetRequiredService<IFileStorageService>(),
                                CreateClient(fileStorageSettings.AwsS3),
                                fileStorageSettings.AwsS3,
                                provider.GetRequiredService<AwsS3ResiliencePipeline>(),
                                provider.GetRequiredService<TimeProvider>()));
            }

            return services
                .AddCoreLogging(loggerSettings)
                .AddSingleton<IFileStorageService>(provider => new LocalFileStorageService(provider.GetRequiredService<TimeProvider>(), new() { BaseDirectory = Path.Combine(Path.GetTempPath(), "storage") }))
                // Register caching pipelines.
                .AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>()
                .AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>()
                // Register providers.
                .AddSingleton<ServiceLocator>()
                .AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        }

        /// <summary>
        /// Registers logging services in the DI container using default base configuration and aggregated logger settings.
        /// </summary>
        /// <param name="loggerSettings">The aggregated logger settings.</param>
        /// <returns>The same <paramref name="services"/> instance to allow call chaining.</returns>
        public IServiceCollection AddCoreLogging(LoggerSettings? loggerSettings)
        {
            if (loggerSettings is null)
                return services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());

            LoggerConfiguration loggerConfiguration = LoggerConfigurationExtensions.Create();

            if (loggerSettings.File is not null)
                loggerConfiguration.WriteToLocalFile(loggerSettings.File);

            if (loggerSettings.Database is not null)
                loggerConfiguration.WriteToPostgres(loggerSettings.Database);

            return services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(loggerConfiguration.CreateLogger(), dispose: true);
            });
        }

        private static IAmazonS3 CreateClient(AwsS3StorageSettings settings)
        {
            AmazonS3Config config = new() { MaxErrorRetry = 0 };

            if (!string.IsNullOrWhiteSpace(settings.Region))
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);

            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                config.ServiceURL = settings.ServiceUrl;
                config.ForcePathStyle = true;
            }

            if (!string.IsNullOrWhiteSpace(settings.AccessKey) && !string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                BasicAWSCredentials credentials = new(settings.AccessKey, settings.SecretKey);
                return new AmazonS3Client(credentials, config);
            }

            return new AmazonS3Client(config);
        }

        private static BlobContainerClient CreateBlobContainerClient(BlobStorageSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                Uri serviceUri = new(settings.ServiceUrl);
                BlobServiceClient serviceClient = new(serviceUri);
                return serviceClient.GetBlobContainerClient(settings.ContainerName);
            }

            return new BlobContainerClient(settings.ConnectionString, settings.ContainerName);
        }

        private static void ValidateDfsStorageSettings(DfsStorageSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentException.ThrowIfNullOrWhiteSpace(settings.BaseDirectory);

            string baseDirectory = settings.BaseDirectory.Trim();

            if (!OperatingSystem.IsWindows())
                ValidateNonWindowsDfsPath(baseDirectory);
        }

        private static void ValidateNonWindowsDfsPath(string path)
        {
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                string platformName = OperatingSystem.IsLinux()
                    ? "Linux"
                    : OperatingSystem.IsMacOS() ? "macOS" : "Unix-like";

                string exampleMount = OperatingSystem.IsMacOS() ? "/Volumes/dfs" : "/mnt/dfs";

                throw new PlatformNotSupportedException(
                    $"Windows UNC network share path '{path}' cannot be accessed directly on {platformName}. " +
                    $"In {platformName} and containerized environments, mount the network share to a local directory (e.g., '{exampleMount}') " +
                    "and configure BaseDirectory with the mount point path.");
            }

            if (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':')
            {
                throw new PlatformNotSupportedException(
                    $"Windows drive path '{path}' is not valid on non-Windows operating systems. " +
                    "Configure BaseDirectory with the local directory or mount point path (e.g., '/mnt/dfs' or './data/dfs').");
            }
        }
    }
}
