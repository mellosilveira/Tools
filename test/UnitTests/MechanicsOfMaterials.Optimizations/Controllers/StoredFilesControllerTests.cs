using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Unit tests for <see cref="StorageEntryMetadataController"/> verifying file streaming downloads
/// and 404 responses when database metadata, storage providers, or physical files are missing.
/// </summary>
public sealed class StoredFilesControllerTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly StorageEntryMetadataController _controller = new(NullLogger<StorageEntryMetadataController>.Instance);

    /// <summary>
    /// Verifies that DownloadByIdAsync returns NotFound when the metadata entity does not exist.
    /// </summary>
    [Fact]
    public async Task DownloadByIdAsync_WhenEntityNotFound_ReturnsNotFound()
    {
        ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(999L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity?)null);

        IActionResult result = await _controller.DownloadByIdAsync(serviceProvider, _repositoryMock.Object, 999L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that DownloadByIdAsync returns NotFound when the keyed storage provider is not registered.
    /// </summary>
    [Fact]
    public async Task DownloadByIdAsync_WhenStorageProviderNotConfigured_ReturnsNotFound()
    {
        StorageEntryMetadataEntity entity = new()
        {
            Id = 1L,
            ExternalIdentifier = "sweep-run-123",
            FileName = "E_SensitivityAnalysis.csv",
            Location = "storage/SensitivityAnalysis/sweep-run-123/E_SensitivityAnalysis.csv",
            ContentType = "text/csv",
            StorageProvider = StorageProvider.AwsS3
        };

        ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        IActionResult result = await _controller.DownloadByIdAsync(serviceProvider, _repositoryMock.Object, 1L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that DownloadByIdAsync returns NotFound when the physical file is missing in storage.
    /// </summary>
    [Fact]
    public async Task DownloadByIdAsync_WhenPhysicalFileMissing_ReturnsNotFound()
    {
        StorageEntryMetadataEntity entity = new()
        {
            Id = 42L,
            ExternalIdentifier = "run-abc",
            FileName = "simulation.csv",
            Location = "storage/ExperimentalData/run-abc/simulation.csv",
            ContentType = "text/csv",
            StorageProvider = StorageProvider.Local
        };

        ServiceCollection services = new();
        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.ExistsAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IActionResult result = await _controller.DownloadByIdAsync(serviceProvider, _repositoryMock.Object, 42L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that DownloadByIdAsync returns a FileStreamResult when entity and physical file exist.
    /// </summary>
    [Fact]
    public async Task DownloadByIdAsync_WhenFileExists_ReturnsFileStreamResult()
    {
        StorageEntryMetadataEntity entity = new()
        {
            Id = 42L,
            ExternalIdentifier = "run-abc",
            FileName = "simulation.csv",
            Location = "storage/ExperimentalData/run-abc/simulation.csv",
            ContentType = "text/csv",
            StorageProvider = StorageProvider.Local
        };

        await using MemoryStream memoryStream = new([1, 2, 3]);

        ServiceCollection services = new();
        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.ExistsAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(f => f.OpenReadAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        IActionResult result = await _controller.DownloadByIdAsync(serviceProvider, _repositoryMock.Object, 42L);

        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("simulation.csv", fileResult.FileDownloadName);
        Assert.Same(memoryStream, fileResult.FileStream);
    }
}
