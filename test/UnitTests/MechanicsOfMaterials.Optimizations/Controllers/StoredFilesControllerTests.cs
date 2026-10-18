using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Unit tests for <see cref="StoredFilesController"/> verifying file metadata query operations,
/// resilient physical file existence checking, and streaming download responses.
/// </summary>
public sealed class StoredFilesControllerTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly StoredFilesController _controller;

    public StoredFilesControllerTests()
    {
        _controller = new(_repositoryMock.Object, _fileStorageServiceMock.Object);
    }

    /// <summary>
    /// Verifies that querying by external identifier returns all associated stored file entities.
    /// </summary>
    [Fact]
    public async Task GetByExternalIdentifierAsync_WhenEntitiesExist_ReturnsListedResult()
    {
        // Arrange
        string externalIdentifier = "sweep-run-123";
        StoredFileEntity entity = new()
        {
            Id = 1,
            ExternalIdentifier = externalIdentifier,
            FileName = "E_SensitivityAnalysis.csv",
            Location = "storage/SensitivityAnalysis/sweep-run-123/E_SensitivityAnalysis.csv",
            ContentType = "text/csv",
            Context = "SensitivityAnalysis"
        };

        _repositoryMock
            .Setup(r => r.GetAsync<StoredFileEntity, StoredFileFilter>(It.IsAny<StoredFileFilter>(), null, default))
            .Returns(GetMockEntities([entity]));

        // Act
        ActionResult<ListedResult<StoredFileEntity>> actionResult = await _controller.GetByExternalIdentifierAsync(
            externalIdentifier);

        // Assert
        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult.Result);
        ListedResult<StoredFileEntity> listedResult = Assert.IsType<ListedResult<StoredFileEntity>>(jsonResult.Value);
        Assert.True(listedResult.Success);
        Assert.NotNull(listedResult.Data);
        Assert.Single(listedResult.Data);
        Assert.Equal("E_SensitivityAnalysis.csv", listedResult.Data[0].FileName);
    }

    /// <summary>
    /// Verifies that downloading by external identifier returns a 404 NotFound response when the database entity is absent.
    /// </summary>
    [Fact]
    public async Task DownloadByExternalIdentifierAsync_WhenEntityNotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync<StoredFileEntity, StoredFileFilter>(It.IsAny<StoredFileFilter>(), It.IsAny<SortOrder>(), default))
            .ReturnsAsync((StoredFileEntity?)null);

        // Act
        IActionResult result = await _controller.DownloadByExternalIdentifierAsync(
            "non-existent-id");

        // Assert
        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(responseResult.Success);
        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
    }

    /// <summary>
    /// Verifies that downloading by external identifier returns a 404 NotFound response when the physical file is missing from storage.
    /// </summary>
    [Fact]
    public async Task DownloadByExternalIdentifierAsync_WhenPhysicalFileMissing_ReturnsNotFound()
    {
        // Arrange
        StoredFileEntity entity = new()
        {
            Id = 42,
            ExternalIdentifier = "run-abc",
            FileName = "simulation.csv",
            Location = "storage/ExperimentalData/run-abc/simulation.csv",
            ContentType = "text/csv",
            Context = "ExperimentalData"
        };

        _repositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync<StoredFileEntity, StoredFileFilter>(It.IsAny<StoredFileFilter>(), It.IsAny<SortOrder>(), default))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.Exists(entity.Location))
            .Returns(false);

        // Act
        IActionResult result = await _controller.DownloadByExternalIdentifierAsync(
            "run-abc");

        // Assert
        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(responseResult.Success);
        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
    }

    /// <summary>
    /// Verifies that downloading by external identifier returns a FileStreamResult when the entity and physical file exist.
    /// </summary>
    [Fact]
    public async Task DownloadByExternalIdentifierAsync_WhenFileExists_ReturnsFileStreamResult()
    {
        // Arrange
        StoredFileEntity entity = new()
        {
            Id = 42,
            ExternalIdentifier = "run-abc",
            FileName = "simulation.csv",
            Location = "storage/ExperimentalData/run-abc/simulation.csv",
            ContentType = "text/csv",
            Context = "ExperimentalData"
        };

        using MemoryStream memoryStream = new([1, 2, 3]);

        _repositoryMock
            .Setup(r => r.GetFirstOrDefaultAsync<StoredFileEntity, StoredFileFilter>(It.IsAny<StoredFileFilter>(), It.IsAny<SortOrder>(), default))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.Exists(entity.Location))
            .Returns(true);

        _fileStorageServiceMock
            .Setup(f => f.OpenRead(entity.Location))
            .Returns(memoryStream);

        // Act
        IActionResult result = await _controller.DownloadByExternalIdentifierAsync(
            "run-abc");

        // Assert
        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("simulation.csv", fileResult.FileDownloadName);
    }

    /// <summary>
    /// Verifies that downloading by ID returns a 404 NotFound response when the physical file is missing from storage.
    /// </summary>
    [Fact]
    public async Task DownloadByIdAsync_WhenPhysicalFileMissing_ReturnsNotFound()
    {
        // Arrange
        StoredFileEntity entity = new()
        {
            Id = 99,
            ExternalIdentifier = "run-xyz",
            FileName = "data.csv",
            Location = "storage/SensitivityAnalysis/run-xyz/data.csv",
            ContentType = "text/csv",
            Context = "SensitivityAnalysis"
        };

        _repositoryMock
            .Setup(r => r.GetAsync<StoredFileEntity>(99, default))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.Exists(entity.Location))
            .Returns(false);

        // Act
        IActionResult result = await _controller.DownloadByIdAsync(
            "run-xyz",
            99);

        // Assert
        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase responseResult = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(responseResult.Success);
        Assert.Equal(StatusCode.NotFound, responseResult.StatusCode);
    }

    private static async IAsyncEnumerable<StoredFileEntity> GetMockEntities(IEnumerable<StoredFileEntity> entities)
    {
        await Task.Yield();
        foreach (StoredFileEntity entity in entities)
        {
            yield return entity;
        }
    }
}
