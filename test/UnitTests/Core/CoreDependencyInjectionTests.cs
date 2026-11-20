using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.AwsS3;
using MelloSilveiraTools.Core.Files.Providers.BlobStorage;
using MelloSilveiraTools.Core.Files.Providers.Dfs;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System.Threading.Channels;

namespace UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="CoreDependencyInjection"/> verifying service registrations,
/// storage provider resolution, logging sinks, and DFS validation.
/// </summary>
public sealed class CoreDependencyInjectionTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"CoreDiTests_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Best effort cleanup of temporary test directory
            }
        }
    }

    private static ResiliencePipelineSettings CreateResilienceSettings() => new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 1,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    /// <summary>
    /// Verifies that AddCoreServices with minimal settings registers base singletons and NullPipelineProgressNotifier.
    /// </summary>
    [Fact]
    public void AddCoreServices_WithMinimalSettings_RegistersBaseSingletonsAndNullNotifier()
    {
        // Arrange
        ServiceCollection services = new();
        ResiliencePipelineSettings resilienceSettings = CreateResilienceSettings();

        // Act
        services.AddCoreServices(resilienceSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.NotNull(provider.GetRequiredService<PipelineFactory>());
        Assert.Same(NullPipelineProgressNotifier.Instance, provider.GetRequiredService<IPipelineProgressNotifier>());
        Assert.IsType<LocalFileStorageService>(provider.GetRequiredService<IFileStorageService>());
        Assert.IsType<InMemorySingleLevelCache>(provider.GetRequiredService<ISingleLevelCache>());
        Assert.IsType<InMemoryTwoLevelCache>(provider.GetRequiredService<ITwoLevelCache>());
        Assert.NotNull(provider.GetRequiredService<ServiceLocator>());
        Assert.IsType<InMemoryDynamicServiceProvider>(provider.GetRequiredService<IDynamicServiceProvider>());
        Assert.NotNull(provider.GetRequiredService<ILogger<CoreDependencyInjectionTests>>());
    }

    /// <summary>
    /// Verifies that supplying PipelineProgressNotifierSettings registers ChannelPipelineProgressNotifier.
    /// </summary>
    [Fact]
    public void AddCoreServices_WithProgressNotifierSettings_RegistersChannelNotifier()
    {
        // Arrange
        ServiceCollection services = new();
        PipelineProgressNotifierSettings notifierSettings = new()
        {
            ChannelCapacity = 64,
            FullMode = BoundedChannelFullMode.DropOldest,
            MaxChannels = 10
        };

        // Act
        services.AddCoreServices(
            CreateResilienceSettings(),
            pipelineProgressNotifierSettings: notifierSettings);
        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        // Assert
        IPipelineProgressNotifier notifier = provider.GetRequiredService<IPipelineProgressNotifier>();
        Assert.IsType<ChannelPipelineProgressNotifier>(notifier);
        Assert.Same(notifierSettings, provider.GetRequiredService<PipelineProgressNotifierSettings>());
    }

    /// <summary>
    /// Verifies that EncryptionSettings registers IEncryptionService as scoped and EncryptionSettings as singleton.
    /// </summary>
    [Fact]
    public void AddCoreServices_WithEncryptionSettings_RegistersEncryptionService()
    {
        // Arrange
        ServiceCollection services = new();
        EncryptionSettings encryptionSettings = new()
        {
            SaltSize = 16,
            HashSize = 32,
            Iterations = 1000
        };

        // Act
        services.AddCoreServices(CreateResilienceSettings(), encryptionSettings: encryptionSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(encryptionSettings, provider.GetRequiredService<EncryptionSettings>());
        Assert.IsType<EncryptionService>(scope.ServiceProvider.GetRequiredService<IEncryptionService>());
    }

    /// <summary>
    /// Verifies that EmailSettings registers SmtpEmailService and SmtpResiliencePipeline.
    /// </summary>
    [Fact]
    public void AddCoreServices_WithEmailSettings_RegistersEmailServiceAndPipeline()
    {
        // Arrange
        ServiceCollection services = new();
        EmailSettings emailSettings = new()
        {
            ApplicationEmail = "sender@example.com",
            ApplicationPassword = "secret",
            SmtpHost = "localhost",
            SmtpPort = 2525,
            ResiliencePipelineSettings = CreateResilienceSettings()
        };

        // Act
        services.AddCoreServices(CreateResilienceSettings(), emailSettings: emailSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        using IServiceScope scope = provider.CreateScope();
        Assert.Same(emailSettings, provider.GetRequiredService<EmailSettings>());
        Assert.NotNull(provider.GetRequiredService<SmtpResiliencePipeline>());
        Assert.IsType<SmtpEmailService>(scope.ServiceProvider.GetRequiredService<IEmailService>());
    }

    /// <summary>
    /// Verifies that FileStorageSettings registers keyed Local, DFS, BlobStorage (via ServiceUrl), and AWS S3 (with credentials and ServiceUrl).
    /// </summary>
    [Fact]
    public void AddCoreServices_WithAllFileStorageProvidersAndCustomEndpoints_RegistersKeyedServices()
    {
        // Arrange
        Directory.CreateDirectory(_tempDirectory);
        ServiceCollection services = new();
        FileStorageSettings fileStorageSettings = new()
        {
            Local = new LocalFileStorageSettings { BaseDirectory = _tempDirectory },
            Dfs = new DfsStorageSettings { BaseDirectory = _tempDirectory },
            BlobStorage = new BlobStorageSettings
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container",
                ServiceUrl = "http://127.0.0.1:10000/devstoreaccount1",
                ResiliencePipelineSettings = CreateResilienceSettings()
            },
            AwsS3 = new AwsS3StorageSettings
            {
                BucketName = "test-bucket",
                Region = "us-east-1",
                AccessKey = "test-key",
                SecretKey = "test-secret",
                ServiceUrl = "http://127.0.0.1:4566",
                ResiliencePipelineSettings = CreateResilienceSettings()
            }
        };

        // Act
        services.AddCoreServices(CreateResilienceSettings(), fileStorageSettings: fileStorageSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<LocalFileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Local));
        Assert.IsType<DfsFileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.Dfs));
        Assert.IsType<BlobStorageFileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.BlobStorage));
        Assert.IsType<AwsS3FileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.AwsS3));
    }

    /// <summary>
    /// Verifies fallback branches for BlobStorage (ConnectionString) and AWS S3 (default credential chain, default resilience settings).
    /// </summary>
    [Fact]
    public void AddCoreServices_WithBlobConnectionStringAndAwsDefaultCredentials_RegistersKeyedServices()
    {
        // Arrange
        ServiceCollection services = new();
        FileStorageSettings fileStorageSettings = new()
        {
            BlobStorage = new BlobStorageSettings
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container"
            },
            AwsS3 = new AwsS3StorageSettings
            {
                BucketName = "test-bucket",
                Region = string.Empty,
                ServiceUrl = "http://127.0.0.1:4566"
            }
        };

        // Act
        services.AddCoreServices(CreateResilienceSettings(), fileStorageSettings: fileStorageSettings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<BlobStorageFileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.BlobStorage));
        Assert.IsType<AwsS3FileStorageService>(provider.GetRequiredKeyedService<IFileStorageService>(StorageProvider.AwsS3));
    }

    /// <summary>
    /// Verifies ValidateNonWindowsDfsPath rejects UNC and Windows drive paths and accepts POSIX paths.
    /// </summary>
    [Fact]
    public void ValidateNonWindowsDfsPath_RejectsUncAndDrivePathsAndAcceptsPosixMountPaths()
    {
        System.Reflection.MethodInfo? method = typeof(CoreDependencyInjection).GetMethod(
            "ValidateNonWindowsDfsPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        System.Reflection.TargetInvocationException uncEx = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method.Invoke(null, [@"\\server\share"]));
        Assert.IsType<PlatformNotSupportedException>(uncEx.InnerException);

        System.Reflection.TargetInvocationException driveEx = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method.Invoke(null, [@"C:\dfs"]));
        Assert.IsType<PlatformNotSupportedException>(driveEx.InnerException);

        method.Invoke(null, ["/mnt/dfs"]);
        method.Invoke(null, ["a"]);
        method.Invoke(null, ["1:path"]);
        method.Invoke(null, ["a/path"]);
    }

    /// <summary>
    /// Verifies that an invalid DFS BaseDirectory triggers eager ArgumentException during AddCoreServices.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCoreServices_WithInvalidDfsBaseDirectory_ThrowsArgumentException(string invalidBaseDirectory)
    {
        // Arrange
        ServiceCollection services = new();
        FileStorageSettings fileStorageSettings = new()
        {
            Dfs = new DfsStorageSettings { BaseDirectory = invalidBaseDirectory }
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddCoreServices(
            CreateResilienceSettings(),
            fileStorageSettings: fileStorageSettings));
    }

    /// <summary>
    /// Verifies that AddCoreLogging configures Serilog sinks when File and Database logger settings are provided.
    /// </summary>
    [Fact]
    public void AddCoreLogging_WithFileAndDatabaseSettings_ConfiguresSerilogAndResolvesLogger()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings loggerSettings = new()
        {
            File = new FileLoggerSettings
            {
                Directory = _tempDirectory,
                FileNamePrefix = "core-di-test",
                RollDaily = false,
                MaxFileSizeBytes = 1048576,
                MaxRetainedFiles = 3
            },
            Database = new DatabaseLoggerSettings
            {
                ConnectionString = "Host=localhost;Database=logs;Username=postgres;Password=postgres;",
                TableName = "app_logs"
            }
        };

        // Act
        services.AddCoreLogging(loggerSettings);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        ILogger<CoreDependencyInjectionTests> logger = provider.GetRequiredService<ILogger<CoreDependencyInjectionTests>>();
        Assert.NotNull(logger);
        Assert.True(Directory.Exists(_tempDirectory));
    }
}
