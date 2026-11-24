using MelloSilveiraTools.Core.Caching;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics;
using MelloSilveiraTools.MechanicsOfMaterials;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.MechanicalModels.Get;
using MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;
using MelloSilveiraTools.MechanicsOfMaterials.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Endpoints;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.Plugins.Application.Endpoints;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Login;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;
using MelloSilveiraTools.WebApi.Application.Controllers;
using MelloSilveiraTools.WebApi.Application.Endpoints;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Host.Commands;
using MelloSilveiraTools.WebApi.Host.Controllers;
using MelloSilveiraTools.WebApi.Host.Endpoints;
using MelloSilveiraTools.WebUi.Models;
using MelloSilveiraTools.WebUi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HostSettings = MelloSilveiraTools.WebApi.Host.Settings;

namespace UnitTests.WebApi.Host;

public class WebApiHostAndQuickstartTests
{
    private static ExperimentalDataSettings CreateExperimentalDataSettings() => new()
    {
        SegmenterOptions = new PipelineStepOptions(1, 64, true),
        FileWriterOptions = new PipelineStepOptions(1, 64, true),
        PersistanceOptions = new PipelineStepOptions(1, 64, true),
        SegmentBuilderOptions = new PipelineStepOptions(1, 64, true),
        CurveFitterOptions = new PipelineStepOptions(1, 64, true),
        SimulationInputBuilderOptions = new PipelineStepOptions(1, 64, true),
        IngestionBufferCapacity = 64,
        MaxCsvSizeBytes = 50L * 1024 * 1024
    };

    private static readonly JsonSerializerOptions _serverJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AuthController_RegisterAsync_CreatesUserAndReturnsCreatedToken()
    {
        AuthenticationToken expectedToken = new("jwe-token-value", TimeProvider.System.GetUtcNow().AddHours(2));
        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEntity?)null);
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessCreated(42L));

        Mock<IEncryptionService> encryptionMock = new();
        encryptionMock.Setup(e => e.GeneratePasswordHash("secret123")).Returns("hashed-secret");

        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.Generate(42L)).Returns(expectedToken);

        RegisterUserCommand command = new(new RegisterUserValidator(), repoMock.Object, encryptionMock.Object, tokenServiceMock.Object);
        AuthController controller = new();
        IActionResult actionResult = await controller.RegisterAsync(command, new RegisterUserRequest { Username = "researcher", Password = "secret123" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<AuthenticationToken> payload = Assert.IsType<Result<AuthenticationToken>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(expectedToken, payload.Data);
    }

    [Fact]
    public async Task AuthController_LoginAsync_WithValidCredentials_ReturnsOkToken()
    {
        AuthenticationToken expectedToken = new("jwe-login-token", TimeProvider.System.GetUtcNow().AddHours(2));
        UserEntity user = new() { Id = 7L, Username = "researcher", PasswordHash = "stored-hash", IsActive = true };

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Mock<IEncryptionService> encryptionMock = new();
        encryptionMock.Setup(e => e.IsPasswordValid("secret123", "stored-hash")).Returns(true);

        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.Generate(7L)).Returns(expectedToken);

        LoginCommand command = new(new LoginValidator(), repoMock.Object, encryptionMock.Object, tokenServiceMock.Object);
        AuthController controller = new();
        IActionResult actionResult = await controller.LoginAsync(command, new LoginRequest { Username = "researcher", Password = "secret123" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<AuthenticationToken> payload = Assert.IsType<Result<AuthenticationToken>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(expectedToken, payload.Data);
    }

    [Fact]
    public async Task AuthController_LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
    {
        UserEntity user = new() { Id = 7L, Username = "researcher", PasswordHash = "stored-hash", IsActive = true };

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.GetFirstOrDefaultAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), It.IsAny<SortOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Mock<IEncryptionService> encryptionMock = new();
        encryptionMock.Setup(e => e.IsPasswordValid("wrong-pass", "stored-hash")).Returns(false);

        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        LoginCommand command = new(new LoginValidator(), repoMock.Object, encryptionMock.Object, tokenServiceMock.Object);
        AuthController controller = new();
        IActionResult actionResult = await controller.LoginAsync(command, new LoginRequest { Username = "researcher", Password = "wrong-pass" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
    }

    [Fact]
    public async Task AuthController_ValidateAsync_WithValidToken_ReturnsIsValidTrue()
    {
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.IsValidAsync("valid-jwe")).ReturnsAsync(true);

        AuthController controller = new();
        IActionResult actionResult = await controller.ValidateAsync(new ValidateAuthenticationTokenCommand(tokenServiceMock.Object), new ValidateTokenRequest { Token = "valid-jwe" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result payload = Assert.IsType<Result>(jsonResult.Value);
        Assert.True(payload.Success);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task AuthController_RefreshAsync_WithEmptyToken_ReturnsBadRequest()
    {
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        AuthController controller = new();

        IActionResult actionResult = await controller.RefreshAsync(new RefreshAuthenticationTokenCommand(tokenServiceMock.Object), new RefreshTokenRequest { Token = "   " });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task AuthController_RefreshAsync_WithValidToken_ReturnsRefreshedToken()
    {
        AuthenticationToken refreshedToken = new("new-jwe-token", TimeProvider.System.GetUtcNow().AddHours(2));
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.RefreshAsync("old-jwe-token")).ReturnsAsync(refreshedToken);

        AuthController controller = new();
        IActionResult actionResult = await controller.RefreshAsync(new RefreshAuthenticationTokenCommand(tokenServiceMock.Object), new RefreshTokenRequest { Token = "old-jwe-token" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<AuthenticationToken> payload = Assert.IsType<Result<AuthenticationToken>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(refreshedToken, payload.Data);
    }

    [Fact]
    public async Task AuthController_RefreshAsync_WhenTokenIsInvalid_ReturnsUnauthorized()
    {
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.RefreshAsync("bad-token")).ThrowsAsync(new SecurityTokenException("Expired"));

        AuthController controller = new();
        IActionResult actionResult = await controller.RefreshAsync(new RefreshAuthenticationTokenCommand(tokenServiceMock.Object), new RefreshTokenRequest { Token = "bad-token" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithMissingFile_ReturnsBadRequest()
    {
        Mock<IFileStorageService> storageMock = new();
        Mock<IRepository> repoMock = new();
        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());

        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), null!, null!);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithEmptyFile_ReturnsBadRequest()
    {
        Mock<IFileStorageService> storageMock = new();
        Mock<IRepository> repoMock = new();
        IFormFile emptyStrain = new FormFile(new MemoryStream([]), 0, 0, "strainFile", "strain.csv");
        IFormFile validStress = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("Time,Stress\n0,0\n")), 0, 14, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), emptyStrain, validStress);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithNonCsvExtension_ReturnsBadRequest()
    {
        Mock<IFileStorageService> storageMock = new();
        Mock<IRepository> repoMock = new();
        byte[] content = Encoding.UTF8.GetBytes("Time,Data\n0,1\n");
        IFormFile txtStrain = new FormFile(new MemoryStream(content), 0, content.Length, "strainFile", "strain.txt");
        IFormFile csvStress = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), txtStrain, csvStress);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithOversizedFile_ReturnsBadRequest()
    {
        Mock<IFileStorageService> storageMock = new();
        Mock<IRepository> repoMock = new();
        Mock<IFormFile> oversizedStrain = new();
        oversizedStrain.Setup(f => f.Length).Returns((50L * 1024 * 1024) + 1);
        oversizedStrain.Setup(f => f.FileName).Returns("strain.csv");

        byte[] content = Encoding.UTF8.GetBytes("Time,Stress\n0,0\n");
        IFormFile validStress = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), oversizedStrain.Object, validStress);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WhenPersistenceFails_PropagatesError()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", It.Is<string>(n => n.Contains("strain")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream)new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));
        storageMock.Setup(s => s.GetMetadataAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>())).ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/upload-1/ExperimentalUploads/strain.csv", "/storage/upload-1/ExperimentalUploads", "strain.csv", 14, DateTimeOffset.UtcNow));

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<long> { Success = false, StatusCode = MelloSilveiraTools.Core.Models.StatusCode.UnknownError, Messages = ["DB offline"] });

        byte[] content = Encoding.UTF8.GetBytes("Time,Data\n0,1\n");
        IFormFile strainForm = new FormFile(new MemoryStream(content), 0, content.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), strainForm, stressForm);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, jsonResult.StatusCode);
        Result<UploadExperimentalDataResultData> payload = Assert.IsType<Result<UploadExperimentalDataResultData>>(jsonResult.Value);
        Assert.False(payload.Success);
        storageMock.Verify(s => s.DeleteAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WhenSecondFilePersistenceFails_CleansUpFirstFileAndDbRecord()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", It.Is<string>(n => n.Contains("strain")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream)new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));
        storageMock.Setup(s => s.GetMetadataAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>())).ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/upload-1/ExperimentalUploads/strain.csv", "/storage/upload-1/ExperimentalUploads", "strain.csv", 14, DateTimeOffset.UtcNow));
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", It.Is<string>(n => n.Contains("stress")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream)new MemoryStream(), "/storage/upload-1/ExperimentalUploads/stress.csv"));
        storageMock.Setup(s => s.GetMetadataAsync("/storage/upload-1/ExperimentalUploads/stress.csv", It.IsAny<CancellationToken>())).ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/upload-1/ExperimentalUploads/stress.csv", "/storage/upload-1/ExperimentalUploads", "stress.csv", 14, DateTimeOffset.UtcNow));

        Mock<IRepository> repoMock = new();
        repoMock
            .SetupSequence(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(201L))
            .ReturnsAsync(new Result<long> { Success = false, StatusCode = MelloSilveiraTools.Core.Models.StatusCode.UnknownError, Messages = ["DB write failed"] });

        byte[] content = Encoding.UTF8.GetBytes("Time,Data\n0,1\n");
        IFormFile strainForm = new FormFile(new MemoryStream(content), 0, content.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), strainForm, stressForm);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, jsonResult.StatusCode);
        storageMock.Verify(s => s.DeleteAsync("/storage/upload-1/ExperimentalUploads/stress.csv", It.IsAny<CancellationToken>()), Times.Once);
        storageMock.Verify(s => s.DeleteAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.DeleteAsync<StorageEntryMetadataEntity>(201L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithValidCsvPair_StoresFilesAndReturnsOk()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", It.Is<string>(n => n.Contains("strain")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream)new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));
        storageMock.Setup(s => s.GetMetadataAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>())).ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/upload-1/ExperimentalUploads/strain.csv", "/storage/upload-1/ExperimentalUploads", "strain.csv", 14, DateTimeOffset.UtcNow));
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", It.Is<string>(n => n.Contains("stress")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Stream)new MemoryStream(), "/storage/upload-1/ExperimentalUploads/stress.csv"));
        storageMock.Setup(s => s.GetMetadataAsync("/storage/upload-1/ExperimentalUploads/stress.csv", It.IsAny<CancellationToken>())).ReturnsAsync(new StorageEntryMetadata(StorageProvider.Local, "/storage/upload-1/ExperimentalUploads/stress.csv", "/storage/upload-1/ExperimentalUploads", "stress.csv", 14, DateTimeOffset.UtcNow));

        Mock<IRepository> repoMock = new();
        repoMock
            .SetupSequence(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(101L))
            .ReturnsAsync(Result.CreateSuccessOk(102L));

        byte[] strainBytes = Encoding.UTF8.GetBytes("Time,Strain\n0,0\n1,0.05\n");
        byte[] stressBytes = Encoding.UTF8.GetBytes("Time,Stress\n0,0\n1,1.25\n");
        IFormFile strainForm = new FormFile(new MemoryStream(strainBytes), 0, strainBytes.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(stressBytes), 0, stressBytes.Length, "stressFile", "stress.csv");

        ExperimentalDataController controller = new(Mock.Of<ILogger<ExperimentalDataController>>());
        IActionResult actionResult = await controller.UploadAsync(new UploadExperimentalDataCommand(new UploadExperimentalDataValidator(CreateExperimentalDataSettings()), storageMock.Object, repoMock.Object), strainForm, stressForm);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<UploadExperimentalDataResultData> payload =
            Assert.IsType<Result<UploadExperimentalDataResultData>>(jsonResult.Value);

        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal(101L, payload.Data.StrainStorageEntryId);
        Assert.Equal(102L, payload.Data.StressStorageEntryId);
    }

    [Fact]
    public async Task SamplesController_GetSamples_ReturnsBundledAclSample()
    {
        SamplesController controller = new();
        IActionResult actionResult = await controller.GetSamples(new GetSampleDatasetsCommand());

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<List<SampleDatasetInfo>> payload =
            Assert.IsType<Result<List<SampleDatasetInfo>>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.NotEmpty(payload.Data);
    }

    [Fact]
    public void EntitiesAndFilters_ParameterlessConstructorsAndFilterProperties_AreValid()
    {
        MechanicalModelSimulationEntity simEntity = new();
        ParametricSweepEntity sweepEntity = new();
        MechanicalModelSimulationFilter simFilter = new() { ExternalIdentifier = "exp-1", CurveFitIdentifier = "fit-1", MechanicalModelName = "Fung" };
        ParametricSweepFilter sweepFilter = new() { ExternalIdentifier = "sweep-1", MechanicalModelName = "Schapery" };

        Assert.NotNull(simEntity.Identifier);
        Assert.NotNull(sweepEntity.Identifier);
        Assert.Equal("exp-1", simFilter.ExternalIdentifier);
        Assert.Equal("sweep-1", sweepFilter.ExternalIdentifier);
    }

    [Fact]
    public void WebUiDtos_SerializeAndDeserializeCompatiblyWithServerContracts()
    {
        ExecuteExperimentalDataRequestDto clientRequest = new()
        {
            ExperimentalDataGroupRequest = new ExperimentalDataGroupDto
            {
                StrainFileLocation = "strain.csv",
                StressFileLocation = "stress.csv"
            },
            MechanicalModelName = "Fung",
            TargetSegments = ["Relaxation"],
            CurveFitterType = "Alglib",
            TargetRSquared = 0.92
        };

        string requestJson = JsonSerializer.Serialize(clientRequest, _serverJsonOptions);
        ExecuteExperimentalDataPipelineRequest? serverRequest = JsonSerializer.Deserialize<ExecuteExperimentalDataPipelineRequest>(requestJson, _serverJsonOptions);
        Assert.NotNull(serverRequest);
        Assert.Equal("Fung", serverRequest.MechanicalModelName);
        Assert.Equal(0.92, serverRequest.TargetRSquared, precision: 6);
        Assert.Contains(SegmentType.Relaxation, serverRequest.TargetSegments);

        PipelineProgressEvent serverEvent = new()
        {
            PipelineIdentifier = "run-123",
            StepName = "MechanicalModelOutputPersistenceStep",
            Description = "Persisted outputs",
            State = PipelineStepState.Completed,
            Timestamp = TimeProvider.System.GetUtcNow(),
            ReceivedItems = 4,
            ProcessedItems = 4
        };

        string eventJson = JsonSerializer.Serialize(serverEvent, _serverJsonOptions);
        PipelineProgressEventDto? clientEvent = JsonSerializer.Deserialize<PipelineProgressEventDto>(eventJson, _serverJsonOptions);
        Assert.NotNull(clientEvent);
        Assert.Equal("run-123", clientEvent.PipelineIdentifier);
        Assert.Equal("Persisted outputs", clientEvent.Description);
        Assert.Equal("Completed", clientEvent.State);
        Assert.Equal(100.0, clientEvent.ProgressPercentage, precision: 6);

        Result<string> serverResult = new() { Success = false, StatusCode = MelloSilveiraTools.Core.Models.StatusCode.BadRequest, Messages = ["Invalid input"] };
        string resultJson = JsonSerializer.Serialize(serverResult, _serverJsonOptions);
        ApiResultDto<string>? clientResult = JsonSerializer.Deserialize<ApiResultDto<string>>(resultJson, _serverJsonOptions);
        Assert.NotNull(clientResult);
        Assert.False(clientResult.Success);
        Assert.Contains("Invalid input", clientResult.JoinedMessages);
    }

    [Fact]
    public void ApiClientService_CsvParsers_ParseExperimentalAndSensitivityData()
    {
        string expCsv = "Time,Strain,Stress,SegmentType\n0.5,0.02,0.50,1\n2.0,0.05,1.20,2\n";
        List<ChartDataPoint> expPoints = ApiClientService.ParseStressStrainCsv(expCsv);

        Assert.Equal(2, expPoints.Count);
        Assert.Equal("1", expPoints[0].SegmentType);
        Assert.Equal(1.20, expPoints[1].Stress, precision: 6);

        string sweepCsv = "VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime\n0.03,1.10,0.80,-0.30,-27.27,15.0\n";
        List<SensitivityPoint> sweepPoints = ApiClientService.ParseSensitivityCsv(sweepCsv);

        Assert.Single(sweepPoints);
        Assert.Equal(0.03, sweepPoints[0].VariableValue, precision: 6);
        Assert.Equal(-27.27, sweepPoints[0].PercentageDeltaStress, precision: 6);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Docker.json")]
    public void WebApiHost_Settings_BindsCompletelyFromAppSettingsAndSecrets(string appSettingsFileName)
    {
        string hostDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MelloSilveiraTools.WebApi.Host"));
        string appSettingsPath = Path.Combine(hostDir, appSettingsFileName);
        string secretsExamplePath = Path.Combine(hostDir, "secrets.example.json");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .AddJsonFile(secretsExamplePath, optional: false)
            .Build();

        HostSettings? settings = configuration.Get<HostSettings>();
        Assert.NotNull(settings);
        settings.Validate();

        Assert.True(settings.AddMechanicalModels);
        Assert.Equal(3, settings.DefaultResiliencePipeline.MaxRetryAttempts);
        Assert.Equal(100_000, settings.Encryption.Iterations);
        Assert.Equal(StorageProvider.Local, settings.FileStorage.DefaultStorageProvider);
        if (appSettingsFileName == "appsettings.json")
            Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), settings.FileStorage.Local!.BaseDirectory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CurveFitterType.Alglib, settings.CurveFitter.DefaultCurveFitter);
        Assert.Equal(256, settings.ExperimentalData.IngestionBufferCapacity);
        Assert.Equal(256, settings.ExperimentalData.SegmenterOptions.MaxBufferSize);
        Assert.Equal(52_428_800L, settings.ExperimentalData.MaxCsvSizeBytes);
        Assert.Equal(256, settings.ParametricSweep.IngestionBufferCapacity);
        Assert.Equal(512, settings.PipelineProgressNotifier.ChannelCapacity);
        Assert.Equal("MelloSilveiraTools.WebApi.Host", settings.Jwt.Issuer);
        Assert.Equal("plugins", settings.Plugins.Directory);
    }

    [Fact]
    public void WebApiHost_Settings_Validate_ThrowsWhenSecretsAreMissing()
    {
        string hostDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "MelloSilveiraTools.WebApi.Host"));
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(hostDir, "appsettings.json"), optional: false)
            .Build();

        HostSettings? settings = configuration.Get<HostSettings>();
        Assert.NotNull(settings);
        Assert.Throws<InvalidOperationException>(() => settings.Validate());
    }

    [Fact]
    public async Task MechanicsOfMaterialsController_GetModels_ResolvesStaticAndDynamicResolversWithJsonSchemas()
    {
        ServiceCollection services = new();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddMechanicsOfMaterialsServices();

        using ServiceProvider provider = services.BuildServiceProvider();
        IDynamicServiceProvider dynamicProvider = provider.GetRequiredService<IDynamicServiceProvider>();
        Mock<IMechanicalModelTypeResolver> pluginResolver = new();
        pluginResolver.SetupGet(r => r.MechanicalModelName).Returns("CustomPluginModel");
        pluginResolver.SetupGet(r => r.Calculator).Returns(typeof(ElasticModelTypeResolver));
        pluginResolver.SetupGet(r => r.ConstitutiveParameters).Returns(typeof(ElasticConstitutiveParameters));
        pluginResolver.SetupGet(r => r.Output).Returns(typeof(ElasticModelOutput));
        dynamicProvider.AddKeyed("CustomPluginModel", pluginResolver.Object);

        GetMechanicalModelsCommand command = provider.GetRequiredService<GetMechanicalModelsCommand>();
        MechanicsOfMaterialsController controller = new();

        JsonResult modelsResult = Assert.IsType<JsonResult>(await controller.GetModelsAsync(command));
        Assert.Equal(StatusCodes.Status200OK, modelsResult.StatusCode);

        ListedResult<MechanicalModelMetadata> payload =
            Assert.IsType<ListedResult<MechanicalModelMetadata>>(modelsResult.Value);
        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal(7, payload.Data.Count());

        Assert.Contains(payload.Data, m => m.MechanicalModelName == nameof(MechanicalModel.Fung) && m.CalculatorTypeName == "IFungModelCalculator");
        Assert.Contains(payload.Data, m => m.MechanicalModelName == "CustomPluginModel");

        foreach (MechanicalModelMetadata model in payload.Data)
        {
            Assert.False(string.IsNullOrWhiteSpace(model.MechanicalModelName));
            Assert.False(string.IsNullOrWhiteSpace(model.CalculatorTypeName));
            Assert.NotNull(model.ConstitutiveParameters);
            Assert.NotNull(model.Output);
            Assert.Contains("properties", model.ConstitutiveParameters.ToJsonString());
            Assert.Contains("properties", model.Output.ToJsonString());
        }
    }

    [Fact]
    public void MinimalApiEndpoints_RegisterAllSuiteRoutes()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        using WebApplication app = builder.Build();

        app.MapAuthEndpoints();
        app.MapStorageEntryMetadataEndpoints();
        app.MapSamplesEndpoints();
        app.MapExperimentalDataEndpoints();
        app.MapParametricSweepEndpoints();
        app.MapPipelineProgressEndpoints();
        app.MapPluginEndpoints("/api/v1/plugins");
        app.MapMechanicsOfMaterialsEndpoints();

        List<string> routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToList();

        Assert.Contains("/api/v1/auth/register", routes);
        Assert.Contains("/api/v1/auth/login", routes);
        Assert.Contains("/api/v1/auth/refresh", routes);
        Assert.Contains("/api/v1/auth/validate", routes);
        Assert.Contains("/api/v1/storage-entry-metadata/{fileId:long}/download", routes);
        Assert.Contains("/api/v1/samples/", routes);
        Assert.Contains("/api/v1/experimental-data/upload", routes);
        Assert.Contains("/api/v1/experimental-data/process", routes);
        Assert.Contains("/api/v1/experimental-data/process/batch", routes);
        Assert.Contains("/api/v1/parametric-sweeps/process", routes);
        Assert.Contains("/api/v1/parametric-sweeps/process/experimental-data", routes);
        Assert.Contains("/api/v1/pipelines/{pipelineId}/progress", routes);
        Assert.Contains("/api/v1/plugins/upload", routes);
        Assert.Contains("/api/v1/mechanics/models", routes);
        Assert.Contains("/api/v1/mechanics/specimen-analysis/execute", routes);
        Assert.Contains("/api/v1/mechanics/fatigue/calculate", routes);
        Assert.Contains("/api/v1/mechanics/fatigue/calculate/endurance-limit", routes);
    }

    [Fact]
    public async Task ExecuteSpecimenAnalysisCommand_WithElasticModel_StreamsCsvAndDeletesFileAfterDownload()
    {
        MemoryStream captureStream = new();
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync(
                It.IsAny<string>(),
                nameof(MechanicalModel.Elastic),
                nameof(RampTimeConsideration.Disregard),
                FileExtensions.CommaSeparatedValues,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ((Stream)captureStream, "/storage/spec-1/Elastic/Disregard.csv"));
        storageMock
            .Setup(s => s.OpenReadAsync("/storage/spec-1/Elastic/Disregard.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(captureStream.ToArray()));
        storageMock
            .Setup(s => s.DeleteAsync("/storage/spec-1/Elastic/Disregard.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(storageMock.Object);
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        ExecuteSpecimenAnalysisCommand command = provider.GetRequiredService<ExecuteSpecimenAnalysisCommand>();
        MechanicsOfMaterialsController controller = new();

        using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 100.0}""");
        ExecuteSpecimenAnalysisRequest request = new()
        {
            MechanicalModelName = nameof(MechanicalModel.Elastic),
            AnalysisIdentifier = "spec-1",
            AnalysisParameter = new AnalysisParameter
            {
                InitialDependentMechanicalParameterValue = 0.0,
                IndependentMechanicalParameter = new MechanicalParameter(0.05)
            },
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 1.0,
            ConstitutiveParameters = constitutiveJson.RootElement.Clone()
        };

        FileStreamResult fileResult = Assert.IsType<FileStreamResult>(await controller.ExecuteSpecimenAnalysisAsync(command, request));
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("Disregard.csv", fileResult.FileDownloadName);

        using (StreamReader reader = new(fileResult.FileStream, Encoding.UTF8, leaveOpen: true))
        {
            string csvText = await reader.ReadToEndAsync();
            Assert.Contains("Time", csvText);
            Assert.True(csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length >= 4);
        }

        storageMock.Verify(s => s.DeleteAsync("/storage/spec-1/Elastic/Disregard.csv", It.IsAny<CancellationToken>()), Times.Never);

        await fileResult.FileStream.DisposeAsync();

        storageMock.Verify(s => s.DeleteAsync("/storage/spec-1/Elastic/Disregard.csv", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSpecimenAnalysisCommand_WithUnknownModel_ReturnsNotFound()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ISingleLevelCache, InMemorySingleLevelCache>();
        services.AddSingleton<IDynamicServiceProvider, InMemoryDynamicServiceProvider>();
        services.AddSingleton<ServiceLocator>();
        services.AddSingleton(Mock.Of<IFileStorageService>());
        services.AddMathematicsServices();
        services.AddMechanicsOfMaterialsServices(addMechanicalModels: true);

        using ServiceProvider provider = services.BuildServiceProvider();
        ExecuteSpecimenAnalysisCommand command = provider.GetRequiredService<ExecuteSpecimenAnalysisCommand>();

        using JsonDocument constitutiveJson = JsonDocument.Parse("""{"youngModulus": 100.0}""");
        ExecuteSpecimenAnalysisRequest request = new()
        {
            MechanicalModelName = "NonExistentModel",
            AnalysisParameter = new AnalysisParameter
            {
                InitialDependentMechanicalParameterValue = 0.0,
                IndependentMechanicalParameter = new MechanicalParameter(0.05)
            },
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = 0.5,
            FinalTime = 1.0,
            ConstitutiveParameters = constitutiveJson.RootElement.Clone()
        };

        Result<ExecuteSpecimenAnalysisResultData> result = await command.ExecuteAsync(request);
        Assert.False(result.Success);
        Assert.Equal(MelloSilveiraTools.Core.Models.StatusCode.NotFound, result.StatusCode);
    }
}
