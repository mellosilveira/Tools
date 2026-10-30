//using MelloSilveiraTools.Core.Models;
//using MelloSilveiraTools.Core.Validators;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
//using Microsoft.AspNetCore.Http;
//using Moq;

//namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;

///// <summary>
///// Unit tests for <see cref="ProcessExperimentalDataCommand"/> verifying orchestration, validation short-circuiting,
///// pipeline delegation, and stream resource management.
///// </summary>
//public sealed class ProcessExperimentalDataCommandTests
//{
//    private readonly Mock<IExperimentalDataProcessingPipeline> _pipelineMock = new();
//    private readonly Mock<IValidator<ProcessExperimentalDataRequest>> _validatorMock = new();

//    /// <summary>
//    /// Verifies that when request validation fails, the command short-circuits and does not invoke the pipeline.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenValidationFails_ShortCircuitsWithoutCallingPipeline()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ProcessExperimentalDataRequest>()))
//            .Returns(Result.CreateBadRequest("MechanicalModelName is required."));

//        ProcessExperimentalDataCommand command = new(_pipelineMock.Object, _validatorMock.Object);
//        ProcessExperimentalDataRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            StrainFile = Mock.Of<IFormFile>(),
//            StressFile = Mock.Of<IFormFile>()
//        };

//        // Act
//        Result<ProcessExperimentalDataResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.False(result.Success);
//        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
//        Assert.Contains("MechanicalModelName is required.", result.Messages);
//        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()), Times.Never);
//    }

//    /// <summary>
//    /// Verifies that when the pipeline succeeds, the command returns a successful response containing the generated identifier.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenPipelineSucceeds_ReturnsSuccessWithIdentifier()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ProcessExperimentalDataRequest>()))
//            .Returns(Result.CreateSuccessOk());

//        string expectedIdentifier = "exp_run_12345";
//        _pipelineMock
//            .Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Result.CreateSuccessOk(expectedIdentifier));

//        ProcessExperimentalDataCommand command = new(_pipelineMock.Object, _validatorMock.Object);

//        Mock<IFormFile> strainFileMock = CreateMockFormFile([1, 2, 3]);
//        Mock<IFormFile> stressFileMock = CreateMockFormFile([4, 5, 6]);

//        ProcessExperimentalDataRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            StrainFile = strainFileMock.Object,
//            StressFile = stressFileMock.Object,
//            TargetRSquared = 0.99
//        };

//        // Act
//        Result<ProcessExperimentalDataResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.True(result.Success);
//        Assert.Equal(StatusCode.OK, result.StatusCode);
//        Assert.NotNull(result.Data);
//        Assert.Equal(expectedIdentifier, result.Data.Identifier);
//        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()), Times.Once);
//    }

//    /// <summary>
//    /// Verifies that when the pipeline returns a failure result, the command propagates the status code and messages.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenPipelineFails_ReturnsFailureWithPipelineStatusCodeAndMessages()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ProcessExperimentalDataRequest>()))
//            .Returns(Result.CreateSuccessOk());

//        Result<string> badPipelineResult = Result.CreateBadRequest("Curve fitting could not converge.");
//        _pipelineMock
//            .Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(badPipelineResult);

//        ProcessExperimentalDataCommand command = new(_pipelineMock.Object, _validatorMock.Object);

//        Mock<IFormFile> strainFileMock = CreateMockFormFile([1, 2]);
//        Mock<IFormFile> stressFileMock = CreateMockFormFile([3, 4]);

//        ProcessExperimentalDataRequest request = new()
//        {
//            MechanicalModelName = "Fung",
//            StrainFile = strainFileMock.Object,
//            StressFile = stressFileMock.Object
//        };

//        // Act
//        Result<ProcessExperimentalDataResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.False(result.Success);
//        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
//        Assert.Contains("Curve fitting could not converge.", result.Messages);
//    }

//    /// <summary>
//    /// Verifies that when FormFiles are supplied, their opened streams are properly disposed in the finally block.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenFormFilesProvided_OpensAndDisposesStreamsInFinally()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ProcessExperimentalDataRequest>()))
//            .Returns(Result.CreateSuccessOk());

//        _pipelineMock
//            .Setup(p => p.ProcessAsync(It.IsAny<ExperimentalDataProcessingInput>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Result.CreateSuccessOk("id_form_files"));

//        TrackableMemoryStream strainStream = new([1, 2, 3]);
//        TrackableMemoryStream stressStream = new([4, 5, 6]);

//        Mock<IFormFile> strainFileMock = new();
//        strainFileMock.Setup(f => f.Length).Returns(3L);
//        strainFileMock.Setup(f => f.OpenReadStream()).Returns(strainStream);

//        Mock<IFormFile> stressFileMock = new();
//        stressFileMock.Setup(f => f.Length).Returns(3L);
//        stressFileMock.Setup(f => f.OpenReadStream()).Returns(stressStream);

//        ProcessExperimentalDataCommand command = new(_pipelineMock.Object, _validatorMock.Object);
//        ProcessExperimentalDataRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            StrainFile = strainFileMock.Object,
//            StressFile = stressFileMock.Object
//        };

//        // Act
//        Result<ProcessExperimentalDataResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.True(result.Success);
//        Assert.True(strainStream.IsDisposed, "Expected strainStream to be disposed after command execution.");
//        Assert.True(stressStream.IsDisposed, "Expected stressStream to be disposed after command execution.");
//    }

//    private static Mock<IFormFile> CreateMockFormFile(byte[] content)
//    {
//        Mock<IFormFile> mock = new();
//        mock.Setup(f => f.Length).Returns((long)content.Length);
//        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
//        return mock;
//    }

//    /// <summary>
//    /// MemoryStream subclass tracking disposal state for verification in tests.
//    /// </summary>
//    private sealed class TrackableMemoryStream(byte[] buffer) : MemoryStream(buffer)
//    {
//        public bool IsDisposed { get; private set; }

//        protected override void Dispose(bool disposing)
//        {
//            base.Dispose(disposing);
//            IsDisposed = true;
//        }

//        public override ValueTask DisposeAsync()
//        {
//            IsDisposed = true;
//            return base.DisposeAsync();
//        }
//    }
//}
