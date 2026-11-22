using MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Create;
using MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Delete;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.StorageEntryMetadata;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
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
        _fileStorageServiceMock.SetupGet(s => s.Provider).Returns(StorageProvider.Local);
        _fileStorageServiceMock
            .Setup(s => s.CreateWriteAsync("exp-1", "ExperimentalData", "strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "storage/exp-1/ExperimentalData/strain.csv"));

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessCreated(77L));

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddSingleton(_fileStorageServiceMock.Object));
        AddStorageEntryCommand command = new(
            NullLogger<AddStorageEntryCommand>.Instance,
            new AddStorageEntryValidator(),
            _repositoryMock.Object,
            serviceLocator,
            TimeProvider.System);

        byte[] content = Encoding.UTF8.GetBytes("t,e\n0,0\n");
        IFormFile formFile = new FormFile(new MemoryStream(content), 0, content.Length, "file", "strain.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        IActionResult result = await _controller.CreateAsync(command, new AddStorageEntryRequest
        {
            File = formFile,
            ExternalIdentifier = "exp-1",
            Context = "ExperimentalData",
            StorageProvider = null
        });

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status201Created, jsonResult.StatusCode);
        Result<StorageEntryMetadataEntity> payload = Assert.IsType<Result<StorageEntryMetadataEntity>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(77L, payload.Data!.Id);
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

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity>(10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _fileStorageServiceMock
            .Setup(s => s.OpenWriteAsync(existing.Location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());
        _repositoryMock
            .Setup(r => r.DeleteAsync<StorageEntryMetadataEntity>(10L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB constraint failure"));

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object));
        UpdateStorageEntryFileCommand command = new(
            NullLogger<UpdateStorageEntryFileCommand>.Instance,
            new UpdateStorageEntryFileValidator(),
            _repositoryMock.Object,
            serviceLocator,
            TimeProvider.System);

        byte[] content = Encoding.UTF8.GetBytes("t,e\n0,0.1\n");
        IFormFile formFile = new FormFile(new MemoryStream(content), 0, content.Length, "file", "strain.csv");

        IActionResult result = await _controller.UpdateAsync(command, 10L, formFile);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, jsonResult.StatusCode);
        Result<StorageEntryMetadataEntity> payload = Assert.IsType<Result<StorageEntryMetadataEntity>>(jsonResult.Value);
        Assert.False(payload.Success);
        Assert.Equal("Não foi possível sobrescrever os metadados.", payload.JoinedMessages);
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

        IActionResult result = await _controller.DeleteAsync(command, null, location, null);

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

        IActionResult result = await _controller.DeleteAsync(command, 55L, location, null);

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ReadAsync_GroupsEntriesByIdentifierContextAndFileExtension()
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

        _repositoryMock
            .Setup(r => r.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(
                It.IsAny<StorageEntryMetadataFilter>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([entity1, entity2]));

        _fileStorageServiceMock
            .Setup(s => s.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ServiceLocator serviceLocator = CreateServiceLocator(services =>
            services.AddKeyedSingleton<IFileStorageService>(StorageProvider.Local, _fileStorageServiceMock.Object));
        ReadStorageEntriesCommand command = new(_repositoryMock.Object, serviceLocator);

        IActionResult result = await _controller.ReadAsync(command, new ReadStorageEntriesRequest());

        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<List<StorageEntriesResultData>> payload = Assert.IsType<Result<List<StorageEntriesResultData>>>(jsonResult.Value);
        Assert.True(payload.Success);
        StorageEntriesResultData group = Assert.Single(payload.Data!);
        Assert.Equal("exp-1", group.Identifier);
        Assert.Equal("ExperimentalData", group.Context);
        Assert.Equal(".csv", group.FileExtension);
        Assert.Equal(2, group.Items.Count);
        Assert.Equal(1L, group.Items[0].EntityId);
        Assert.Equal("strain.csv", group.Items[0].FileName);
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
