using AcceptanceTests.Plugins.Helpers;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AcceptanceTests.Plugins;

/// <summary>
/// Acceptance tests for plugin file uploads, verifying that valid managed .NET assemblies
/// are safely streamed to the incoming storage partition, while non-conforming or corrupted files
/// are rejected before persisting to storage.
/// </summary>
public sealed class PluginUploadAcceptanceTests
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
    /// Verifies that uploading a valid managed .NET assembly with conforming naming convention
    /// streams the payload to the incoming partition and returns accurate storage metadata.
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithValidManagedAssembly_ShouldWriteToIncomingPartitionAndReturnMetadata()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        using IServiceScope scope = fixture.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "Contoso.Analytics.v1.0.0.dll";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, assemblyBytes);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await uploadCommand.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(fileName, result.Data.FileName);
        Assert.Equal(assemblyBytes.Length, result.Data.SizeBytes);
        Assert.Equal(StorageProvider.Local, result.Data.StorageProvider);

        bool fileExistsInIncoming = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.True(fileExistsInIncoming);

        byte[]? storedBytes = fixture.LocalStorage.GetFileBytes("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.NotNull(storedBytes);
        Assert.Equal(assemblyBytes.Length, storedBytes.Length);
    }

    /// <summary>
    /// Verifies that uploading a file with a non-.dll extension is rejected with HTTP 400 Bad Request
    /// and never touches storage.
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithNonDllExtension_ShouldReturnBadRequestAndNotStoreFile()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        using IServiceScope scope = fixture.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        byte[] textBytes = [0x54, 0x65, 0x73, 0x74];
        string fileName = "MaliciousScript.v1.0.0.exe";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, textBytes);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await uploadCommand.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);

        bool fileExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.False(fileExists);
    }

    /// <summary>
    /// Verifies that uploading a file without the mandatory .v{major}.{minor}.{patch} version pattern
    /// is rejected with HTTP 400 Bad Request and not persisted to storage.
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithMalformedFileNamePattern_ShouldReturnBadRequestAndNotStoreFile()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        using IServiceScope scope = fixture.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        byte[] assemblyBytes = PluginTestFixture.GetValidPluginBytes();
        string fileName = "Contoso.Analytics.dll";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, assemblyBytes);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await uploadCommand.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);

        bool fileExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.False(fileExists);
    }

    /// <summary>
    /// Verifies that uploading an unmanaged or corrupted binary file named as a DLL is rejected
    /// by the PE validator and not persisted to storage.
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithCorruptedOrNonManagedBinary_ShouldReturnBadRequestAndNotStoreFile()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        using IServiceScope scope = fixture.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        byte[] corruptedBytes = PluginTestFixture.GetCorruptedPluginBytes();
        string fileName = "CorruptedModule.v1.0.0.dll";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, corruptedBytes);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await uploadCommand.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);

        bool fileExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.False(fileExists);
    }

    /// <summary>
    /// Verifies that uploading an empty zero-byte file is rejected before any PE header processing.
    /// </summary>
    [Fact]
    public async Task UploadAsync_WithEmptyFile_ShouldReturnBadRequestAndNotStoreFile()
    {
        // Arrange
        await using PluginTestFixture fixture = new();
        using IServiceScope scope = fixture.CreateScope();
        UploadPlugin uploadCommand = scope.ServiceProvider.GetRequiredService<UploadPlugin>();

        string fileName = "EmptyModule.v1.0.0.dll";
        Mock<IFormFile> fileMock = CreateFormFileMock(fileName, []);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await uploadCommand.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);

        bool fileExists = fixture.LocalStorage.FileExists("plugins", PluginStoragePartitions.Incoming, fileName);
        Assert.False(fileExists);
    }
}
