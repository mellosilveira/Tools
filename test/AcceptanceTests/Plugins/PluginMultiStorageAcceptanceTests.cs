using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests verifying that <see cref="MelloSilveiraTools.Plugins"/> is fully storage-agnostic:
/// changing <see cref="PluginSettings.StorageProvider"/> to an external cloud or alternative provider
/// (e.g. <see cref="StorageProvider.AwsS3"/>) binds all operations to that keyed storage implementation
/// without modifying application behavior.
/// </summary>
public sealed class PluginMultiStorageAcceptanceTests
{
    private static Mock<IFormFile> CreateFormFileMock(string fileName, byte[] content)
    {
        Mock<IFormFile> fileMock = new();
        fileMock.SetupGet(f => f.FileName).Returns(fileName);
        fileMock.SetupGet(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return fileMock;
    }

    /// <summary>
    /// Verifies that when configured with <see cref="StorageProvider.AwsS3"/>, upload, startup discovery,
    /// promotion, and cache persistence execute exclusively against the AWS S3 storage mock,
    /// leaving local storage untouched.
    /// </summary>
    [Fact]
    public async Task PluginSystem_WhenConfiguredWithAwsS3_ShouldOperateExclusivelyOnAwsS3Storage()
    {
        // Arrange
        await using PluginTestFixture fixture = new(storageProvider: StorageProvider.AwsS3);
        using IServiceScope scope = fixture.CreateScope();

        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();
        IPluginService pluginService = scope.ServiceProvider.GetRequiredService<IPluginService>();
        PluginCache cache = scope.ServiceProvider.GetRequiredService<PluginCache>();

        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "CloudPlugin.v1.0.0.dll";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, assemblyBytes);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act 1: Upload via command
        Result<StorageEntryMetadata> uploadResult = await uploadCommand.ExecuteAsync(request);

        // Assert 1: Uploaded to AwsS3 incoming partition, Local storage empty
        Assert.True(uploadResult.Success);
        Assert.Equal(StorageProvider.AwsS3, uploadResult.Data!.StorageProvider);

        bool inAwsIncoming = fixture.AwsS3Storage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        bool inLocalIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);

        Assert.True(inAwsIncoming);
        Assert.False(inLocalIncoming);

        // Act 2: Load plugins at startup
        await pluginService.LoadPluginsOnStartupAsync(fixture.Services);

        // Assert 2: Promoted to loaded on AwsS3, Local storage still empty
        bool inAwsLoaded = fixture.AwsS3Storage.FileExists("plugins", PluginStoragePartitions.Loaded, fileName);
        bool inLocalLoaded = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Loaded, fileName);

        Assert.True(inAwsLoaded);
        Assert.False(inLocalLoaded);

        // Act 3: Persist and restore cache on AwsS3
        await pluginService.PersistCacheAsync();

        bool awsCacheExists = fixture.AwsS3Storage.FileExists("plugins", PluginStoragePartitions.Cache, "plugin-cache.json");
        bool localCacheExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Cache, "plugin-cache.json");

        Assert.True(awsCacheExists);
        Assert.False(localCacheExists);

        await pluginService.ClearAsync();
        Assert.False(cache.TryGet<RegisteredPlugin>("CloudPlugin", new PluginVersion(1, 0, 0), out RegisteredPlugin? _));

        await pluginService.RestoreCacheAsync();
        Assert.True(cache.TryGet<RegisteredPlugin>("CloudPlugin", new PluginVersion(1, 0, 0), out RegisteredPlugin? restored));
        Assert.NotNull(restored);
        Assert.Equal("CloudPlugin", restored.Name);
    }
}
