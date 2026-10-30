//using MelloSilveiraTools.Core.Managers.File.Models;
//using MelloSilveiraTools.Core.Managers.File.Providers;
//using MelloSilveiraTools.Core.Models;
//using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
//using MelloSilveiraTools.Database.Repositories;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;

//namespace UnitTests.MechanicsOfMaterials.Optimizations.Controllers;

///// <summary>
///// Unit tests for <see cref="StoredFilesController"/> verifying file streaming download responses,
///// 404 handling when database records or physical storage objects are absent, and keyed storage resolution.
///// </summary>
//public sealed class StoredFilesControllerTests
//{
//    private readonly Mock<IRepository> _repositoryMock = new();
//    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
//    private readonly StoredFilesController _controller = new(NullLogger<StoredFilesController>.Instance);

//    /// <summary>
//    /// Verifies that downloading returns a 404 NotFound response when the database entity is absent.
//    /// </summary>
//    [Fact]
//    public async Task DownloadByIdAsync_WhenEntityNotFound_ReturnsNotFound()
//    {
//        // Arrange
//        ServiceCollection services = new();
//        ServiceProvider serviceProvider = services.BuildServiceProvider();

//        _repositoryMock
//            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(999L, default))
//            .ReturnsAsync((StorageEntryMetadataEntity?)null);

//        // Act
//        IActionResult result = await _controller.DownloadByIdAsync(
//            serviceProvider,
//            _repositoryMock.Object,
//            999L);

//        // Assert
//        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
//        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
//        Assert.False(responseResult.Success);
//        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
//    }

//    /// <summary>
//    /// Verifies that downloading returns a 404 NotFound response when the required storage provider is not configured.
//    /// </summary>
//    [Fact]
//    public async Task DownloadByIdAsync_WhenStorageProviderNotConfigured_ReturnsNotFound()
//    {
//        // Arrange
//        StorageEntryMetadataEntity entity = new()
//        {
//            Id = 1L,
//            ExternalIdentifier = "sweep-run-123",
//            FileName = "E_SensitivityAnalysis.csv",
//            Location = "storage/SensitivityAnalysis/sweep-run-123/E_SensitivityAnalysis.csv",
//            ContentType = "text/csv",
//            Context = "SensitivityAnalysis",
//            StorageProvider = StorageProvider.AwsS3
//        };

//        ServiceCollection services = new();
//        ServiceProvider serviceProvider = services.BuildServiceProvider();

//        _repositoryMock
//            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(1L, default))
//            .ReturnsAsync(entity);

//        // Act
//        IActionResult result = await _controller.DownloadByIdAsync(
//            serviceProvider,
//            _repositoryMock.Object,
//            1L);

//        // Assert
//        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
//        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
//        Assert.False(responseResult.Success);
//        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
//    }

//    /// <summary>
//    /// Verifies that downloading returns a 404 NotFound response when the physical file is missing from storage.
//    /// </summary>
//    [Fact]
//    public async Task DownloadByIdAsync_WhenPhysicalFileMissing_ReturnsNotFound()
//    {
//        // Arrange
//        StorageEntryMetadataEntity entity = new()
//        {
//            Id = 42L,
//            ExternalIdentifier = "run-abc",
//            FileName = "simulation.csv",
//            Location = "storage/ExperimentalData/run-abc/simulation.csv",
//            ContentType = "text/csv",
//            Context = "ExperimentalData",
//            StorageProvider = StorageProvider.Local
//        };

//        ServiceCollection services = new();
//        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object);
//        ServiceProvider serviceProvider = services.BuildServiceProvider();

//        _repositoryMock
//            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, default))
//            .ReturnsAsync(entity);

//        _fileStorageServiceMock
//            .Setup(f => f.ExistsAsync(entity.Location, default))
//            .ReturnsAsync(false);

//        // Act
//        IActionResult result = await _controller.DownloadByIdAsync(
//            serviceProvider,
//            _repositoryMock.Object,
//            42L);

//        // Assert
//        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
//        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
//        Assert.False(responseResult.Success);
//        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
//    }

//    /// <summary>
//    /// Verifies that downloading returns a FileStreamResult when the entity and physical file exist.
//    /// </summary>
//    [Fact]
//    public async Task DownloadByIdAsync_WhenFileExists_ReturnsFileStreamResult()
//    {
//        // Arrange
//        StorageEntryMetadataEntity entity = new()
//        {
//            Id = 42L,
//            ExternalIdentifier = "run-abc",
//            FileName = "simulation.csv",
//            Location = "storage/ExperimentalData/run-abc/simulation.csv",
//            ContentType = "text/csv",
//            Context = "ExperimentalData",
//            StorageProvider = StorageProvider.Local
//        };

//        using MemoryStream memoryStream = new([1, 2, 3]);

//        ServiceCollection services = new();
//        services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object);
//        ServiceProvider serviceProvider = services.BuildServiceProvider();

//        _repositoryMock
//            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, default))
//            .ReturnsAsync(entity);

//        _fileStorageServiceMock
//            .Setup(f => f.ExistsAsync(entity.Location, default))
//            .ReturnsAsync(true);

//        _fileStorageServiceMock
//            .Setup(f => f.OpenReadAsync(entity.Location, default))
//            .ReturnsAsync(memoryStream);

//        // Act
//        IActionResult result = await _controller.DownloadByIdAsync(
//            serviceProvider,
//            _repositoryMock.Object,
//            42L);

//        // Assert
//        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(result);
//        Assert.Equal("text/csv", fileResult.ContentType);
//        Assert.Equal("simulation.csv", fileResult.FileDownloadName);
//    }
//}
