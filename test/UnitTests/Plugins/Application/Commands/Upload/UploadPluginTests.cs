using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Plugins.Application.Commands.Upload;

public sealed class UploadPluginTests
{
    private readonly Mock<IFileStorageService> _storageMock = new();
    private readonly UploadPluginValidator _validator = new();
    private readonly PluginSettings _settings = new()
    {
        StorageProvider = StorageProvider.Local,
        Directory = "plugins",
        DefaultCacheTarget = "file",
        PollInterval = TimeSpan.FromSeconds(30),
        PreviousVersionRetention = TimeSpan.FromHours(24)
    };

    [Fact]
    public async Task ExecuteAsync_WhenValidPluginFile_StreamsToIncomingPartitionAndReturnsMetadata()
    {
        // Arrange
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPlugin).Assembly.Location);
        string fileName = "Contoso.Tools.v2.1.0.dll";
        string targetLocation = "storage/plugins/incoming/Contoso.Tools.v2.1.0.dll";

        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(validAssemblyBytes));

        MemoryStream destinationStream = new();

        _storageMock
            .Setup(s => s.ResolveLocation("plugins", PluginStoragePartitions.Incoming, fileName))
            .Returns(targetLocation);

        _storageMock
            .Setup(s => s.OpenWriteAsync(targetLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationStream);

        StorageEntryMetadata expectedMetadata = new(
            StorageProvider.Local,
            targetLocation,
            "storage/plugins/incoming",
            fileName,
            validAssemblyBytes.Length,
            DateTimeOffset.UtcNow);

        _storageMock
            .Setup(s => s.GetMetadataAsync(targetLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetadata);

        UploadPlugin command = new(NullLogger<UploadPlugin>.Instance, _storageMock.Object, _validator, _settings);
        UploadPluginRequest request = new() { File = fileMock.Object };

        // Act
        Result<StorageEntryMetadata> result = await command.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(fileName, result.Data.FileName);
        Assert.Equal(validAssemblyBytes.Length, result.Data.SizeBytes);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFileValidationFails_ReturnsBadRequestWithoutWritingToStorage()
    {
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(0L);
        fileMock.Setup(f => f.FileName).Returns("InvalidPlugin.txt");

        UploadPlugin command = new(NullLogger<UploadPlugin>.Instance, _storageMock.Object, _validator, _settings);
        UploadPluginRequest request = new() { File = fileMock.Object };

        Result<StorageEntryMetadata> result = await command.ExecuteAsync(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        _storageMock.Verify(s => s.OpenWriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
