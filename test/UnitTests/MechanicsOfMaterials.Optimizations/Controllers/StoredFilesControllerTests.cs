using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Controllers;

/// <summary>
/// Unit tests for <see cref="StorageEntryMetadataController"/> and its CRUD/download commands.
/// </summary>
public sealed class StoredFilesControllerTests
{
    private readonly Mock<IRepository> _repositoryMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly Mock<IDynamicServiceProvider> _dynamicServiceProviderMock = new();
    private readonly StorageEntryMetadataController _controller = new();

    private ServiceLocator CreateServiceLocator(Action<ServiceCollection>? configure = null)
    {
        ServiceCollection services = new();
        configure?.Invoke(services);
        ServiceProvider provider = services.BuildServiceProvider();
        return new ServiceLocator(_dynamicServiceProviderMock.Object, provider);
    }

    [Fact]
    public async Task DownloadByIdAsync_WhenEntityNotFound_ReturnsNotFound()
    {
        ServiceLocator serviceLocator = CreateServiceLocator();
        DownloadStoredFileCommand command = new(_repositoryMock.Object, serviceLocator);

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(999L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity?)null);

        IActionResult result = await _controller.DownloadByIdAsync(command, 999L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

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

        ServiceLocator serviceLocator = CreateServiceLocator();
        DownloadStoredFileCommand command = new(_repositoryMock.Object, serviceLocator);

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        IActionResult result = await _controller.DownloadByIdAsync(command, 1L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

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

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object));
        DownloadStoredFileCommand command = new(_repositoryMock.Object, serviceLocator);

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.ExistsAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IActionResult result = await _controller.DownloadByIdAsync(command, 42L);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        ResultBase response = Assert.IsAssignableFrom<ResultBase>(jsonResult.Value);
        Assert.False(response.Success);
        Assert.Equal(StatusCode.NotFound, response.StatusCode);
    }

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

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object));
        DownloadStoredFileCommand command = new(_repositoryMock.Object, serviceLocator);

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _fileStorageServiceMock
            .Setup(f => f.ExistsAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(f => f.OpenReadAsync(entity.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        IActionResult result = await _controller.DownloadByIdAsync(command, 42L);

        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("simulation.csv", fileResult.FileDownloadName);
        Assert.Same(memoryStream, fileResult.FileStream);
    }

    [Fact]
    public async Task CreateAsync_WithoutStorageProvider_UsesDefaultNonKeyedServiceAndPersistsMetadata()
    {
        const string location = "storage/exp-1/ExperimentalData/strain.csv";
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        StorageEntryMetadata storedMetadata = new(StorageProvider.Local, location, "storage/exp-1/ExperimentalData", "strain.csv", 8, now);

        _fileStorageServiceMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _fileStorageServiceMock
            .Setup(s => s.CreateWriteAsync("exp-1", "ExperimentalData", "strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), location));
        _fileStorageServiceMock
            .Setup(s => s.GetMetadataAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedMetadata);

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessCreated(77L));

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddSingleton(_fileStorageServiceMock.Object));
        AddStorageEntryCommand command = new(
            NullLogger<AddStorageEntryCommand>.Instance,
            new AddStorageEntryValidator(),
            _repositoryMock.Object,
            serviceLocator);

        byte[] content = Encoding.UTF8.GetBytes("t,e\n0,0\n");
        IFormFile formFile = new FormFile(new MemoryStream(content), 0, content.Length, "file", "strain.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        IActionResult result = await _controller.CreateAsync(command, new AddStorageEntryRequest
        {
            File = formFile,
            Identifier = "exp-1",
            ExternalIdentifier = "exp-1",
            Context = "ExperimentalData",
            StorageProvider = null
        });

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<StorageEntryResultData> payload = Assert.IsType<Result<StorageEntryResultData>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(77L, payload.Data!.EntityId);
        Assert.Equal(location, payload.Data.Metadata.Location);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryThrows_LogsAndReturnsOverwriteErrorMessage()
    {
        StorageEntryMetadataEntity existing = new()
        {
            Id = 10L,
            ExternalIdentifier = "exp-1",
            FileName = "strain.csv",
            Location = "storage/exp-1/ExperimentalData/strain.csv",
            Directory = "storage/exp-1/ExperimentalData",
            ContentType = "text/csv",
            StorageProvider = StorageProvider.Local
        };
        StorageEntryMetadata updatedMetadata = new(
            StorageProvider.Local,
            existing.Location,
            existing.Directory,
            existing.FileName,
            10,
            TimeProvider.System.GetUtcNow());

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _fileStorageServiceMock
            .Setup(s => s.OpenWriteAsync(existing.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());
        _fileStorageServiceMock
            .Setup(s => s.GetMetadataAsync(existing.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedMetadata);
        _repositoryMock
            .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(10L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB constraint failure"));

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object));
        UpdateStorageEntryFileCommand command = new(
            NullLogger<UpdateStorageEntryFileCommand>.Instance,
            new UpdateStorageEntryFileValidator(),
            _repositoryMock.Object,
            serviceLocator);

        byte[] content = Encoding.UTF8.GetBytes("t,e\n0,0.1\n");
        IFormFile formFile = new FormFile(new MemoryStream(content), 0, content.Length, "file", "strain.csv");

        IActionResult result = await _controller.UpdateAsync(command, 10L, formFile);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, jsonResult.StatusCode);
        Result<StorageEntryResultData> payload = Assert.IsType<Result<StorageEntryResultData>>(jsonResult.Value);
        Assert.False(payload.Success);
        Assert.Equal("It was not possible to override metadata.", payload.JoinedMessages);
    }

    [Fact]
    public async Task DeleteAsync_WhenOnlyInStorage_DeletesIdempotentlyFromStorage()
    {
        const string location = "storage/exp-1/ExperimentalData/orphan.csv";
        _fileStorageServiceMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _fileStorageServiceMock
            .Setup(s => s.ExistsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileStorageServiceMock
            .Setup(s => s.DeleteAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddSingleton(_fileStorageServiceMock.Object));
        DeleteStorageEntryCommand command = new(
            NullLogger<DeleteStorageEntryCommand>.Instance,
            new DeleteStorageEntryValidator(),
            _repositoryMock.Object,
            serviceLocator);

        IActionResult result = await _controller.DeleteAsync(command, new DeleteStorageEntryRequest
        {
            FileId = null,
            Location = location,
            StorageProvider = null
        });

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        _fileStorageServiceMock.Verify(s => s.DeleteAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync<StorageEntryMetadataEntity>(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissingInBothStorageAndDatabase_ReturnsNotFound()
    {
        const string location = "storage/exp-1/ExperimentalData/missing.csv";
        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(55L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageEntryMetadataEntity?)null);
        _fileStorageServiceMock
            .Setup(s => s.ExistsAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddSingleton(_fileStorageServiceMock.Object));
        DeleteStorageEntryCommand command = new(
            NullLogger<DeleteStorageEntryCommand>.Instance,
            new DeleteStorageEntryValidator(),
            _repositoryMock.Object,
            serviceLocator);

        IActionResult result = await _controller.DeleteAsync(command, new DeleteStorageEntryRequest
        {
            FileId = 55L,
            Location = location,
            StorageProvider = null
        });

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_ReturnsStoredEntriesWithResolvedStorageMetadata()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        StorageEntryMetadataEntity entity1 = new()
        {
            Id = 1L,
            ExternalIdentifier = "exp-1",
            StorageProvider = StorageProvider.Local,
            Location = "storage/exp-1/ExperimentalData/strain.csv",
            Directory = "storage/exp-1/ExperimentalData",
            FileName = "strain.csv",
            SizeBytes = 120,
            CreationTimestamp = now
        };
        StorageEntryMetadataEntity entity2 = new()
        {
            Id = 2L,
            ExternalIdentifier = "exp-1",
            StorageProvider = StorageProvider.Local,
            Location = "storage/exp-1/ExperimentalData/stress.csv",
            Directory = "storage/exp-1/ExperimentalData",
            FileName = "stress.csv",
            SizeBytes = 240,
            CreationTimestamp = now
        };

        StorageEntryMetadata metadata1 = new(StorageProvider.Local, entity1.Location, entity1.Directory, entity1.FileName, entity1.SizeBytes, now);
        StorageEntryMetadata metadata2 = new(StorageProvider.Local, entity2.Location, entity2.Directory, entity2.FileName, entity2.SizeBytes, now);

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                It.IsAny<StorageEntryMetadataFilter>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([entity1, entity2]));

        _fileStorageServiceMock
            .Setup(s => s.GetMetadataAsync(entity1.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata1);
        _fileStorageServiceMock
            .Setup(s => s.GetMetadataAsync(entity2.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata2);

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddSingleton(_fileStorageServiceMock.Object));
        ReadStorageEntriesCommand command = new(_repositoryMock.Object, serviceLocator);

        IActionResult result = await _controller.ReadAsync(command, new ReadStorageEntriesRequest());

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        ListedResult<StorageEntryResultData> payload = Assert.IsType<ListedResult<StorageEntryResultData>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(2, payload.Data!.Count);
        Assert.Equal(1L, payload.Data[0].EntityId);
        Assert.Equal("strain.csv", payload.Data[0].Metadata.FileName);
        Assert.Equal(2L, payload.Data[1].EntityId);
        Assert.Equal("stress.csv", payload.Data[1].Metadata.FileName);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
