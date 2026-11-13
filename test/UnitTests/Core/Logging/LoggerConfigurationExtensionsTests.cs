using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace UnitTests.Core.Logging;

/// <summary>
/// Unit tests for <see cref="LoggerConfigurationExtensions"/>, <see cref="FileLoggerSettings"/>, and <see cref="LoggerSettings"/>
/// verifying logger bootstrapping, file and database sink registrations, and DI service container integration.
/// </summary>
public sealed class LoggerConfigurationExtensionsTests : IDisposable
{
    private const string DummyConnectionString = "Host=localhost;Database=testdb;Username=test;Password=test;";
    private const string DummyTableName = "app_logs";
    private readonly string _testLogDirectory;

    public LoggerConfigurationExtensionsTests()
    {
        _testLogDirectory = Path.Combine(Path.GetTempPath(), $"LoggerTests_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch
            {
                // Best effort cleanup in test temp folder
            }
        }
    }

    /// <summary>
    /// Verifies that LoggerConfigurationExtensions.Create returns a non-null LoggerConfiguration.
    /// </summary>
    [Fact]
    public void Create_ReturnsConfiguredLoggerConfiguration()
    {
        // Act
        LoggerConfiguration config = LoggerConfigurationExtensions.Create();

        // Assert
        Assert.NotNull(config);
    }

    /// <summary>
    /// Verifies that WriteToLocalFile creates the target directory and returns the LoggerConfiguration when using FileLoggerSettings directly.
    /// </summary>
    [Fact]
    public void WriteToLocalFile_WithFileLoggerSettings_CreatesDirectoryAndAppliesFileSink()
    {
        // Arrange
        LoggerConfiguration config = LoggerConfigurationExtensions.Create();
        FileLoggerSettings settings = new()
        {
            Directory = _testLogDirectory,
            FileNamePrefix = "test-file-log",
            RollDaily = false,
            MaxFileSizeBytes = 1048576,
            MaxRetainedFiles = 10
        };

        // Act
        LoggerConfiguration updatedConfig = config.WriteToLocalFile(settings);

        // Assert
        Assert.NotNull(updatedConfig);
        Assert.True(Directory.Exists(_testLogDirectory));
    }

    /// <summary>
    /// Verifies that WriteToPostgres successfully configures the PostgreSQL sink using DatabaseLoggerSettings.
    /// </summary>
    [Fact]
    public void WriteToPostgres_WithDatabaseLoggerSettings_ConfiguresSink()
    {
        // Arrange
        LoggerConfiguration config = LoggerConfigurationExtensions.Create();
        DatabaseLoggerSettings settings = new()
        {
            ConnectionString = DummyConnectionString,
            TableName = DummyTableName,
            SchemaName = "logging",
            BatchSizeLimit = 50,
            Period = TimeSpan.FromSeconds(10),
            NeedAutoCreateTable = true
        };

        // Act
        LoggerConfiguration updatedConfig = config.WriteToPostgres(settings);

        // Assert
        Assert.NotNull(updatedConfig);
    }

    /// <summary>
    /// Verifies that AddCoreLogging with null LoggerSettings registers Microsoft default console logging.
    /// </summary>
    [Fact]
    public void AddCoreLogging_WhenLoggerSettingsNull_EnablesDefaultConsoleLogging()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddCoreLogging(null);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<LoggerConfigurationExtensionsTests>>());
    }

    /// <summary>
    /// Verifies that AddCoreLogging with File settings registers Serilog and ILoggerFactory in the DI container.
    /// </summary>
    [Fact]
    public void AddCoreLogging_WithFileSettings_RegistersLoggingServices()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings settings = new()
        {
            File = new FileLoggerSettings
            {
                Directory = _testLogDirectory,
                FileNamePrefix = "app-log",
                RollDaily = true,
                MaxRetainedFiles = 30
            }
        };

        // Act
        services.AddCoreLogging(settings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<LoggerConfigurationExtensionsTests>>());
    }

    /// <summary>
    /// Verifies that AddCoreLogging with Database settings registers Serilog and ILoggerFactory in the DI container.
    /// </summary>
    [Fact]
    public void AddCoreLogging_WithDatabaseSettings_RegistersLoggingServices()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings settings = new()
        {
            Database = new DatabaseLoggerSettings
            {
                ConnectionString = DummyConnectionString,
                TableName = DummyTableName
            }
        };

        // Act
        services.AddCoreLogging(settings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<LoggerConfigurationExtensionsTests>>());
    }

    /// <summary>
    /// Verifies that AddCoreLogging with both File and Database configured enables both sinks and registers ILoggerFactory.
    /// </summary>
    [Fact]
    public void AddCoreLogging_WithBothFileAndDatabase_RegistersBothSinks()
    {
        // Arrange
        ServiceCollection services = new();
        LoggerSettings settings = new()
        {
            File = new FileLoggerSettings
            {
                Directory = _testLogDirectory,
                FileNamePrefix = "both-log",
                RollDaily = true,
                MaxRetainedFiles = 10
            },
            Database = new DatabaseLoggerSettings
            {
                ConnectionString = DummyConnectionString,
                TableName = DummyTableName
            }
        };

        // Act
        services.AddCoreLogging(settings);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ILogger<LoggerConfigurationExtensionsTests>>());
    }
}
