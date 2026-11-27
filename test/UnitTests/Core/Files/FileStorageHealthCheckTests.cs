using MelloSilveiraTools.Core.Files.HealthChecks;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Files.Providers.Dfs;
using MelloSilveiraTools.Core.Files.Providers.Local;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Core.Files;

public sealed class FileStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenNoStorageProvidersRegistered_ReturnsUnhealthy()
    {
        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();

        await using ServiceProvider provider = services.BuildServiceProvider();
        ServiceLocator locator = provider.GetRequiredService<ServiceLocator>();
        FileStorageHealthCheck healthCheck = new(NullLogger<FileStorageHealthCheck>.Instance, locator);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(FileStorageHealthCheck.DefaultName, healthCheck, HealthStatus.Unhealthy, null)
        };

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("No IFileStorageService instances", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WithDuplicateProviderKindsAndMultipleExceptions_DisambiguatesKeysAndAggregatesExceptions()
    {
        Mock<IFileStorageService> first = new();
        first.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        first.Setup(s => s.IsHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Mock<IFileStorageService> second = new();
        second.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        second.Setup(s => s.IsHealthAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<bool>(new IOException("Disk #2 full")));

        Mock<IFileStorageService> third = new();
        third.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        third.Setup(s => s.IsHealthAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<bool>(new UnauthorizedAccessException("Disk #3 denied")));

        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(first.Object);
        services.AddSingleton(second.Object);
        services.AddSingleton(third.Object);

        await using ServiceProvider provider = services.BuildServiceProvider();
        ServiceLocator locator = provider.GetRequiredService<ServiceLocator>();
        FileStorageHealthCheck healthCheck = new(NullLogger<FileStorageHealthCheck>.Instance, locator);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(FileStorageHealthCheck.DefaultName, healthCheck, HealthStatus.Unhealthy, null)
        };

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        AggregateException aggregate = Assert.IsType<AggregateException>(result.Exception);
        Assert.Equal(2, aggregate.InnerExceptions.Count);
        Assert.Equal("Healthy", result.Data["Local"]);
        Assert.Equal("Unhealthy", result.Data["Local#2"]);
        Assert.Equal("Unhealthy", result.Data["Local#3"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelledByCaller_PropagatesOperationCanceledException()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Mock<IFileStorageService> providerMock = new();
        providerMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        providerMock.Setup(s => s.IsHealthAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct =>
            {
                ct.ThrowIfCancellationRequested();
                return ValueTask.FromResult(true);
            });

        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(providerMock.Object);

        await using ServiceProvider provider = services.BuildServiceProvider();
        ServiceLocator locator = provider.GetRequiredService<ServiceLocator>();
        FileStorageHealthCheck healthCheck = new(NullLogger<FileStorageHealthCheck>.Instance, locator);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(FileStorageHealthCheck.DefaultName, healthCheck, HealthStatus.Unhealthy, null)
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => healthCheck.CheckHealthAsync(context, cts.Token));
    }

    [Fact]
    public async Task LocalAndDfsIsHealthAsync_WhenCancelled_ThrowsOperationCanceledExceptionAndLeavesNoProbeFiles()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"mst-health-cancel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            LocalFileStorageService localService = new(
                NullLoggerFactory.Instance,
                TimeProvider.System,
                new LocalFileStorageSettings { BaseDirectory = tempDir });
            DfsFileStorageService dfsService = new(
                NullLoggerFactory.Instance,
                TimeProvider.System,
                new DfsStorageSettings { BaseDirectory = tempDir });

            using CancellationTokenSource cts = new();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => localService.IsHealthAsync(cts.Token).AsTask());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dfsService.IsHealthAsync(cts.Token).AsTask());
            Assert.Empty(Directory.GetFiles(tempDir, ".health-probe-*.tmp"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
