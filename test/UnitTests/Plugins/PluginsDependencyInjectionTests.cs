using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;

namespace UnitTests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginsDependencyInjection.AddPluginServices"/> and <see cref="PluginsDependencyInjection.AddPluginServicesAsync"/>
/// verifying service registrations, keyed storage provider resolution according to <see cref="PluginSettings.StorageProvider"/>, and lifecycle scopes.
/// </summary>
public sealed class PluginsDependencyInjectionTests
{
    private static async IAsyncEnumerable<StorageEntryMetadata> EmptyEntries()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static void SetupBaseDependencies(
        IServiceCollection services,
        Mock<IFileStorageService> localStorageMock,
        Mock<IFileStorageService> awsS3StorageMock)
    {
        localStorageMock
            .Setup(s => s.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyEntries());
        awsS3StorageMock
            .Setup(s => s.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyEntries());

        services.AddLogging();
        services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
        services.AddSingleton<IDynamicServiceProvider>(Mock.Of<IDynamicServiceProvider>());
        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, localStorageMock.Object);
        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.AwsS3, awsS3StorageMock.Object);
    }

    private static object? GetStorageField(object instance)
    {
        Type type = instance.GetType();
        FieldInfo? field = type.GetField("<fileStorageService>P", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetField("_fileStorageService", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => typeof(IFileStorageService).IsAssignableFrom(f.FieldType));
        return field?.GetValue(instance);
    }

    [Fact]
    public void AddPluginServices_WithLocalProvider_ResolvesServicesBoundToLocalStorage()
    {
        // Arrange
        Mock<IFileStorageService> localStorageMock = new();
        Mock<IFileStorageService> awsS3StorageMock = new();

        ServiceCollection services = new();
        SetupBaseDependencies(services, localStorageMock, awsS3StorageMock);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = TimeSpan.FromSeconds(30),
            PreviousVersionRetention = TimeSpan.FromHours(24)
        };

        // Act
        services.AddPluginServices(settings);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();
        PluginAssemblyProcessor assemblyProcessor = scope.ServiceProvider.GetRequiredService<PluginAssemblyProcessor>();
        IPluginCachePersistence filePersistence = scope.ServiceProvider.GetRequiredKeyedService<IPluginCachePersistence>(PluginCacheTargets.File);
        UploadPlugin uploadPlugin = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        // Assert
        Assert.NotNull(fileProcessor);
        Assert.NotNull(assemblyProcessor);
        Assert.NotNull(filePersistence);
        Assert.NotNull(uploadPlugin);

        Assert.Same(localStorageMock.Object, GetStorageField(fileProcessor));
        Assert.Same(localStorageMock.Object, GetStorageField(assemblyProcessor));
        Assert.Same(localStorageMock.Object, GetStorageField(filePersistence));
        Assert.Same(localStorageMock.Object, GetStorageField(uploadPlugin));
    }

    [Fact]
    public void AddPluginServices_WithAwsS3Provider_ResolvesServicesBoundToAwsS3Storage()
    {
        // Arrange
        Mock<IFileStorageService> localStorageMock = new();
        Mock<IFileStorageService> awsS3StorageMock = new();

        ServiceCollection services = new();
        SetupBaseDependencies(services, localStorageMock, awsS3StorageMock);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.AwsS3,
            Directory = "plugins",
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = TimeSpan.FromSeconds(30),
            PreviousVersionRetention = TimeSpan.FromHours(24)
        };

        // Act
        services.AddPluginServices(settings);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        PluginFileProcessor fileProcessor = scope.ServiceProvider.GetRequiredService<PluginFileProcessor>();
        PluginAssemblyProcessor assemblyProcessor = scope.ServiceProvider.GetRequiredService<PluginAssemblyProcessor>();
        IPluginCachePersistence filePersistence = scope.ServiceProvider.GetRequiredKeyedService<IPluginCachePersistence>(PluginCacheTargets.File);
        UploadPlugin uploadPlugin = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        // Assert
        Assert.Same(awsS3StorageMock.Object, GetStorageField(fileProcessor));
        Assert.Same(awsS3StorageMock.Object, GetStorageField(assemblyProcessor));
        Assert.Same(awsS3StorageMock.Object, GetStorageField(filePersistence));
        Assert.Same(awsS3StorageMock.Object, GetStorageField(uploadPlugin));
    }

    [Fact]
    public void AddPluginServices_RegistersSettingsAsSingleton()
    {
        // Arrange
        Mock<IFileStorageService> localStorageMock = new();
        Mock<IFileStorageService> awsS3StorageMock = new();

        ServiceCollection services = new();
        SetupBaseDependencies(services, localStorageMock, awsS3StorageMock);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = TimeSpan.FromSeconds(30),
            PreviousVersionRetention = TimeSpan.FromHours(24)
        };

        // Act
        services.AddPluginServices(settings);
        using ServiceProvider provider = services.BuildServiceProvider();

        PluginSettings registeredSettings = provider.GetRequiredService<PluginSettings>();

        // Assert
        Assert.Same(settings, registeredSettings);
        Assert.Equal(StorageProvider.Local, registeredSettings.StorageProvider);
    }

    [Fact]
    public async Task AddPluginServicesAsync_EagerlyLoadsPluginsAtStartup()
    {
        // Arrange
        Mock<IFileStorageService> localStorageMock = new();
        Mock<IFileStorageService> awsS3StorageMock = new();

        ServiceCollection services = new();
        SetupBaseDependencies(services, localStorageMock, awsS3StorageMock);

        PluginSettings settings = new()
        {
            StorageProvider = StorageProvider.Local,
            Directory = "plugins",
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = TimeSpan.FromSeconds(30),
            PreviousVersionRetention = TimeSpan.FromHours(24)
        };

        // Act
        await services.AddPluginServicesAsync(settings);
        using ServiceProvider provider = services.BuildServiceProvider();

        PluginSettings registeredSettings = provider.GetRequiredService<PluginSettings>();

        // Assert
        Assert.Same(settings, registeredSettings);
        localStorageMock.Verify(s => s.ListFilesAsync(It.IsAny<string>(), PluginStoragePartitions.Incoming, It.IsAny<CancellationToken>()), Times.Once);
    }
}
