using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Services.Encryption;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Login;
using MelloSilveiraTools.WebApi.Application.Commands.Users.RefreshToken;
using MelloSilveiraTools.WebApi.Application.Commands.Users.Register;
using MelloSilveiraTools.WebApi.Application.Commands.Users.ValidateToken;
using MelloSilveiraTools.WebApi.Application.Controllers;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Host.Commands;
using MelloSilveiraTools.WebApi.Host.Controllers;
using MelloSilveiraTools.WebUi.Models;
using MelloSilveiraTools.WebUi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            .Setup(r => r.GetAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable<UserEntity>([]));
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
        Assert.Equal(StatusCodes.Status201Created, jsonResult.StatusCode);
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
            .Setup(r => r.GetAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([user]));

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
            .Setup(r => r.GetAsync<UserEntity, UserFilter>(It.IsAny<UserFilter>(), null, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable([user]));

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
}
