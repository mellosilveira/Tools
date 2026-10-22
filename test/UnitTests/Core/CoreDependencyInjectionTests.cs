using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Logging;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Managers.File.Providers.Dfs;
using MelloSilveiraTools.Core.Managers.File.Providers.Local;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Services.Email;
using MelloSilveiraTools.Core.Services.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="CoreDependencyInjection.AddCoreServices"/> verifying service registrations,
/// caching singletons, keyed storage providers, encryption, email services, and logging configurations.
/// </summary>
public sealed class CoreDependencyInjectionTests
{
    private static readonly ResiliencePipelineSettings TestResilienceSettings = new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 1,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    [Fact]
    public void AddCoreServices_MinimalSettings_RegistersBaseSingletons()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddCoreServices(defaultResiliencePipelineSettings: TestResilienceSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISingleLevelCache>());
        Assert.NotNull(provider.GetService<ITwoLevelCache>());
        Assert.NotNull(provider.GetService<ServiceLocator>());
        Assert.NotNull(provider.GetService<IDynamicServiceProvider>());
    }

    [Fact]
    public void AddCoreServices_WithEncryptionSettings_RegistersEncryptionService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        EncryptionSettings encryptionSettings = new()
        {
            SaltSize = 16,
            HashSize = 32,
            Iterations = 10000
        };

        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            encryptionSettings: encryptionSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEncryptionService>());
        Assert.Same(encryptionSettings, provider.GetService<EncryptionSettings>());
    }

    [Fact]
    public void AddCoreServices_WithEmailSettings_RegistersEmailServiceAndPipeline()
    {
        ServiceCollection services = new();
        services.AddLogging();

        EmailSettings emailSettings = new()
        {
            ApplicationEmail = "sender@example.com",
            ApplicationPassword = "super-secret-password",
            SmtpHost = "smtp.example.com",
            SmtpPort = 587
        };

        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            emailSettings: emailSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEmailService>());
        Assert.NotNull(provider.GetService<SmtpResiliencePipeline>());
        Assert.Same(emailSettings, provider.GetService<EmailSettings>());
    }

    [Fact]
    public void AddCoreServices_WithLocalStorageSettings_RegistersKeyedStorageService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        FileStorageSettings storageSettings = new()
        {
            Local = new LocalFileStorageSettings
            {
                BaseDirectory = Path.GetTempPath()
            }
        };

        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            fileStorageSettings: storageSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        IFileStorageService? localService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Local);
        Assert.NotNull(localService);
    }

    [Fact]
    public void AddCoreServices_WithDfsStorageSettings_RegistersKeyedDfsStorageService()
    {
        ServiceCollection services = new();
        services.AddLogging();

        FileStorageSettings storageSettings = new()
        {
            Dfs = new DfsStorageSettings
            {
                BaseDirectory = OperatingSystem.IsWindows() ? @"\\server\share\folder" : "/mnt/dfs/folder"
            }
        };

        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            fileStorageSettings: storageSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        IFileStorageService? dfsService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Dfs);
        Assert.NotNull(dfsService);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCoreServices_WithInvalidDfsBaseDirectory_ThrowsArgumentExceptionImmediately(string invalidPath)
    {
        ServiceCollection services = new();
        FileStorageSettings storageSettings = new()
        {
            Dfs = new DfsStorageSettings { BaseDirectory = invalidPath }
        };

        Assert.Throws<ArgumentException>(() =>
        {
            services.AddCoreServices(
                defaultResiliencePipelineSettings: TestResilienceSettings,
                fileStorageSettings: storageSettings);
        });
    }

    [Fact]
    public void AddCoreServices_WhenLoggerSettingsNull_UsesMicrosoftDefaultLogging()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            loggerSettings: null);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<CoreDependencyInjectionTests>>());
    }

    [Fact]
    public void AddCoreServices_WhenLoggerSettingsEmpty_UsesMicrosoftDefaultLogging()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings loggerSettings = new();

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            loggerSettings: loggerSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<CoreDependencyInjectionTests>>());
    }

    [Fact]
    public void AddCoreServices_WhenFileConfiguredOnly_EnablesFileLogging()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings loggerSettings = new()
        {
            File = new FileLoggerSettings
            {
                Directory = Path.GetTempPath(),
                FileNamePrefix = "test-log",
                RollDaily = true,
                MaxRetainedFiles = 5
            }
        };

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            loggerSettings: loggerSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<CoreDependencyInjectionTests>>());
    }

    [Fact]
    public void AddCoreServices_WhenDatabaseConfiguredOnly_EnablesDatabaseLogging()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings loggerSettings = new()
        {
            Database = new DatabaseLoggerSettings
            {
                ConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres",
                TableName = "app_logs"
            }
        };

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            loggerSettings: loggerSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<CoreDependencyInjectionTests>>());
    }

    [Fact]
    public void AddCoreServices_WhenBothConfigured_EnablesBothSinks()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings loggerSettings = new()
        {
            File = new FileLoggerSettings
            {
                Directory = Path.GetTempPath(),
                FileNamePrefix = "test-log-both",
                RollDaily = true,
                MaxRetainedFiles = 5
            },
            Database = new DatabaseLoggerSettings
            {
                ConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres",
                TableName = "app_logs"
            }
        };

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            loggerSettings: loggerSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<CoreDependencyInjectionTests>>());
    }

    [Fact]
    public void AddCoreServices_WithPipelineProgressNotificationSettings_RegistersSingleton()
    {
        // Arrange
        ServiceCollection services = new();
        PipelineProgressNotificationSettings notificationSettings = new()
        {
            ImmediateNotificationItemLimit = 25,
            HighVolumeNotificationInterval = 250
        };

        // Act
        services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            pipelineProgressNotificationSettings: notificationSettings);

        ServiceProvider provider = services.BuildServiceProvider();
        PipelineProgressNotificationSettings? resolvedSettings = provider.GetService<PipelineProgressNotificationSettings>();

        // Assert
        Assert.NotNull(resolvedSettings);
        Assert.Equal(25, resolvedSettings.ImmediateNotificationItemLimit);
        Assert.Equal(250, resolvedSettings.HighVolumeNotificationInterval);
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(-1, 500)]
    [InlineData(50, 0)]
    [InlineData(50, -1)]
    public void AddCoreServices_WithInvalidPipelineProgressNotificationSettings_ThrowsArgumentOutOfRangeException(int immediateLimit, int interval)
    {
        // Arrange
        ServiceCollection services = new();
        PipelineProgressNotificationSettings invalidSettings = new()
        {
            ImmediateNotificationItemLimit = immediateLimit,
            HighVolumeNotificationInterval = interval
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddCoreServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            pipelineProgressNotificationSettings: invalidSettings));
    }
}
