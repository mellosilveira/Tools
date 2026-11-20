using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.WebApi.Authentication;
using MelloSilveiraTools.WebApi.Authentication.Services;
using MelloSilveiraTools.WebApi.Host.Controllers;
using MelloSilveiraTools.WebUi.Models;
using MelloSilveiraTools.WebUi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitTests.WebApi.Host;

public class WebApiHostAndQuickstartTests
{
    private static readonly JsonSerializerOptions _serverJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void AuthController_GenerateDemoToken_ReturnsOkResultWithToken()
    {
        AuthenticationToken expectedToken = new("jwe-token-value", TimeProvider.System.GetUtcNow().AddHours(2));
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.Generate("researcher")).Returns(expectedToken);

        AuthController controller = new(tokenServiceMock.Object);
        IActionResult actionResult = controller.GenerateDemoToken(new AuthController.DemoTokenRequest { Username = "researcher" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<AuthenticationToken> payload = Assert.IsType<Result<AuthenticationToken>>(jsonResult.Value);
        Assert.True(payload.Success);
        Assert.Equal(expectedToken, payload.Data);
    }

    [Fact]
    public async Task AuthController_RefreshAsync_WithEmptyToken_ReturnsBadRequest()
    {
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        AuthController controller = new(tokenServiceMock.Object);

        IActionResult actionResult = await controller.RefreshAsync(new AuthController.RefreshTokenRequest { Token = "   " });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task AuthController_RefreshAsync_WithValidToken_ReturnsRefreshedToken()
    {
        AuthenticationToken refreshedToken = new("new-jwe-token", TimeProvider.System.GetUtcNow().AddHours(2));
        Mock<IAuthenticationTokenService> tokenServiceMock = new();
        tokenServiceMock.Setup(s => s.RefreshAsync("old-jwe-token")).ReturnsAsync(refreshedToken);

        AuthController controller = new(tokenServiceMock.Object);
        IActionResult actionResult = await controller.RefreshAsync(new AuthController.RefreshTokenRequest { Token = "old-jwe-token" });

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

        AuthController controller = new(tokenServiceMock.Object);
        IActionResult actionResult = await controller.RefreshAsync(new AuthController.RefreshTokenRequest { Token = "bad-token" });

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status401Unauthorized, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WithMissingFile_ReturnsBadRequest()
    {
        Mock<IFileStorageService> storageMock = new();
        Mock<IRepository> repoMock = new();
        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);

        IActionResult actionResult = await controller.UploadAsync(null, null);

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

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(emptyStrain, validStress);

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

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(txtStrain, csvStress);

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

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(oversizedStrain.Object, validStress);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, jsonResult.StatusCode);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WhenPersistenceFails_PropagatesError()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", "strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));

        Mock<IRepository> repoMock = new();
        repoMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<long> { Success = false, StatusCode = MelloSilveiraTools.Core.Models.StatusCode.UnknownError, Messages = ["DB offline"] });

        byte[] content = Encoding.UTF8.GetBytes("Time,Data\n0,1\n");
        IFormFile strainForm = new FormFile(new MemoryStream(content), 0, content.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(strainForm, stressForm);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status500InternalServerError, jsonResult.StatusCode);
        Result payload = Assert.IsType<Result>(jsonResult.Value);
        Assert.False(payload.Success);
        storageMock.Verify(s => s.DeleteAsync("/storage/upload-1/ExperimentalUploads/strain.csv", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExperimentalDataUploadController_UploadAsync_WhenSecondFilePersistenceFails_CleansUpFirstFileAndDbRecord()
    {
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", "strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", "stress.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "/storage/upload-1/ExperimentalUploads/stress.csv"));

        Mock<IRepository> repoMock = new();
        repoMock
            .SetupSequence(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(201L))
            .ReturnsAsync(new Result<long> { Success = false, StatusCode = MelloSilveiraTools.Core.Models.StatusCode.UnknownError, Messages = ["DB write failed"] });

        byte[] content = Encoding.UTF8.GetBytes("Time,Data\n0,1\n");
        IFormFile strainForm = new FormFile(new MemoryStream(content), 0, content.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(content), 0, content.Length, "stressFile", "stress.csv");

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(strainForm, stressForm);

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
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", "strain.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "/storage/upload-1/ExperimentalUploads/strain.csv"));
        storageMock
            .Setup(s => s.CreateWriteAsync(It.IsAny<string>(), "ExperimentalUploads", "stress.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new MemoryStream(), "/storage/upload-1/ExperimentalUploads/stress.csv"));

        Mock<IRepository> repoMock = new();
        repoMock
            .SetupSequence(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(101L))
            .ReturnsAsync(Result.CreateSuccessOk(102L));

        byte[] strainBytes = Encoding.UTF8.GetBytes("Time,Strain\n0,0\n1,0.05\n");
        byte[] stressBytes = Encoding.UTF8.GetBytes("Time,Stress\n0,0\n1,1.25\n");
        IFormFile strainForm = new FormFile(new MemoryStream(strainBytes), 0, strainBytes.Length, "strainFile", "strain.csv");
        IFormFile stressForm = new FormFile(new MemoryStream(stressBytes), 0, stressBytes.Length, "stressFile", "stress.csv");

        ExperimentalDataUploadController controller = new(TimeProvider.System, storageMock.Object, repoMock.Object);
        IActionResult actionResult = await controller.UploadAsync(strainForm, stressForm);

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<ExperimentalDataUploadController.ExperimentalUploadResponse> payload =
            Assert.IsType<Result<ExperimentalDataUploadController.ExperimentalUploadResponse>>(jsonResult.Value);

        Assert.True(payload.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal(101L, payload.Data.StrainStorageEntryId);
        Assert.Equal(102L, payload.Data.StressStorageEntryId);
    }

    [Fact]
    public void SamplesController_GetSamples_ReturnsBundledAclSample()
    {
        SamplesController controller = new();
        IActionResult actionResult = controller.GetSamples();

        JsonResult jsonResult = Assert.IsType<JsonResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, jsonResult.StatusCode);
        Result<List<SamplesController.SampleDatasetInfo>> payload =
            Assert.IsType<Result<List<SamplesController.SampleDatasetInfo>>>(jsonResult.Value);
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
