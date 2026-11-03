using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AcceptanceTests.Plugins.Helpers;

/// <summary>
/// Custom test processor tracking processable types found within loaded plugin assemblies.
/// </summary>
public sealed class TestPluginTypeProcessor : IPluginTypeProcessor
{
    /// <inheritdoc/>
    public Type ProcessableType => typeof(object);

    /// <summary>
    /// Gets the list of types successfully processed by this instance.
    /// </summary>
    public List<Type> ProcessedTypes { get; } = [];

    /// <inheritdoc/>
    public void Process(Type type, PluginRegistrationContext context)
    {
        if (type.Name == nameof(UploadPlugin))
        {
            ProcessedTypes.Add(type);

            if (context.IsStartup && context.Services is not null)
            {
                context.Services.AddKeyedTransient("TestPlugin", (IServiceProvider _, object _) => type.Name);
            }
            else if (context.DynamicProvider is not null)
            {
                context.DynamicProvider.AddKeyed(typeof(UploadPlugin).FullName!, type.Name);
            }
        }
    }
}

/// <summary>
/// Test fixture managing an isolated dependency injection container, mocked file storage instances,
/// and configured plugin settings for acceptance test executions.
/// </summary>
public sealed class PluginTestFixture : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// In-memory storage mock targeting <see cref="StorageProvider.Local"/>.
    /// </summary>
    public PluginTestStorageMock LocalStorage { get; }

    /// <summary>
    /// In-memory storage mock targeting <see cref="StorageProvider.AwsS3"/>.
    /// </summary>
    public PluginTestStorageMock AwsS3Storage { get; }

    /// <summary>
    /// Custom type processor observing discovered types.
    /// </summary>
    public TestPluginTypeProcessor TypeProcessor { get; }

    /// <summary>
    /// Configured plugin settings for the container.
    /// </summary>
    public PluginSettings Settings { get; }

    /// <summary>
    /// The service collection defining all application and plugin service registrations.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// The active service provider instance.
    /// </summary>
    public ServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginTestFixture"/> class.
    /// </summary>
    /// <param name="storageProvider">The storage provider technology to configure in settings.</param>
    /// <param name="directory">Root directory or context name for plugins.</param>
    /// <param name="pollInterval">Poll interval for the orchestrator background service.</param>
    /// <param name="retention">Retention period for previous plugin versions.</param>
    public PluginTestFixture(
        StorageProvider storageProvider = StorageProvider.Local,
        string directory = "plugins",
        TimeSpan? pollInterval = null,
        TimeSpan? retention = null)
    {
        LocalStorage = new PluginTestStorageMock(StorageProvider.Local);
        AwsS3Storage = new PluginTestStorageMock(StorageProvider.AwsS3);
        TypeProcessor = new TestPluginTypeProcessor();

        Settings = new PluginSettings
        {
            StorageProvider = storageProvider,
            Directory = directory,
            DefaultCacheTarget = PluginCacheTargets.File,
            PollInterval = pollInterval ?? TimeSpan.FromMilliseconds(50),
            PreviousVersionRetention = retention ?? TimeSpan.FromHours(24)
        };

        Services = new ServiceCollection();
        Services.AddLogging();
        Services.AddSingleton<ITwoLevelCache, InMemoryTwoLevelCache>();
        Services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        Services.AddSingleton<IPluginTypeProcessor>(TypeProcessor);

        Services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, LocalStorage.Mock.Object);
        Services.AddKeyedSingleton<IFileStorageService>(StorageProvider.AwsS3, AwsS3Storage.Mock.Object);
        Services.AddScoped<IFileStorageService>((IServiceProvider sp) => sp.GetRequiredKeyedService<IFileStorageService>(Settings.StorageProvider));

        Services.AddSingleton(Mock.Of<IRepository>());

        Services.AddPluginServices(Settings);

        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a new scoped lifetime container.
    /// </summary>
    /// <returns>A newly created <see cref="IServiceScope"/>.</returns>
    public IServiceScope CreateScope()
    {
        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Rebuilds the root service provider after registering additional services into <see cref="Services"/>.
    /// </summary>
    public void RebuildServiceProvider()
    {
        ServiceProvider.Dispose();
        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets a byte buffer representing a valid managed .NET assembly read from the current runtime.
    /// </summary>
    /// <returns>The raw assembly bytes.</returns>
    public static byte[] GetValidPluginBytes()
    {
        string location = typeof(UploadPlugin).Assembly.Location;
        return File.ReadAllBytes(location);
    }

    /// <summary>
    /// Gets a byte buffer representing an unmanaged native PE binary without CLR metadata.
    /// </summary>
    /// <returns>The raw byte array.</returns>
    public static byte[] GetNonManagedPluginBytes()
    {
        return [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];
    }

    /// <summary>
    /// Gets a byte buffer representing arbitrary corrupted, non-PE binary bytes.
    /// </summary>
    /// <returns>The raw byte array.</returns>
    public static byte[] GetCorruptedPluginBytes()
    {
        return [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ServiceProvider.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await ServiceProvider.DisposeAsync().ConfigureAwait(false);
    }
}
