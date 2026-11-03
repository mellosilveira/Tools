//using MelloSilveiraTools.Core.Models;
//using MelloSilveiraTools.Core.Validators;
//using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
//using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;
//using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
//using Moq;

//namespace UnitTests.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

///// <summary>
///// Unit tests for <see cref="ExecuteParametricSweepCommand"/> verifying command execution, validation, and pipeline delegation.
///// </summary>
//public sealed class ExecuteParametricSweepCommandTests
//{
//    private readonly Mock<IParametricSweepPipeline> _pipelineMock = new();
//    private readonly Mock<IValidator<ExecuteParametricSweepRequest>> _validatorMock = new();

//    /// <summary>
//    /// Verifies that when request validation fails, the command short-circuits and does not execute the pipeline.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenValidationFails_ShortCircuitsWithoutCallingPipeline()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ExecuteParametricSweepRequest>()))
//            .Returns(Result.CreateBadRequest("MechanicalModelName is required."));

//        ExecuteParametricSweepCommand command = new(_pipelineMock.Object, _validatorMock.Object);
//        ExecuteParametricSweepRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
//            BaseStrain = 0.05,
//            RampTimeConsideration = RampTimeConsideration.Disregard,
//            InitialTime = 0.0,
//            FinalTime = 10.0,
//            TimeStep = 0.1
//        };

//        // Act
//        Result<ExecuteParametricSweepResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.False(result.Success);
//        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
//        Assert.Contains("MechanicalModelName is required.", result.Messages);
//        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<ParametricSweepInput>(), It.IsAny<CancellationToken>()), Times.Never);
//    }

//    /// <summary>
//    /// Verifies that when the pipeline executes successfully, the command returns the generated identifier.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenPipelineSucceeds_ReturnsSuccessWithIdentifier()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ExecuteParametricSweepRequest>()))
//            .Returns(Result.CreateSuccessOk());

//        string expectedIdentifier = "sweep_run_999";
//        _pipelineMock
//            .Setup(p => p.ProcessAsync(It.IsAny<ParametricSweepInput>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(Result.CreateSuccessOk(expectedIdentifier));

//        ExecuteParametricSweepCommand command = new(_pipelineMock.Object, _validatorMock.Object);
//        ExecuteParametricSweepRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
//            ConstantVariables = new Dictionary<string, RangeParameters>
//            {
//                ["Stiffness"] = new RangeParameters { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 }
//            },
//            BaseStrain = 0.05,
//            RampTimeConsideration = RampTimeConsideration.Disregard,
//            TimeStep = 0.1,
//            InitialTime = 0.0,
//            FinalTime = 10.0
//        };

//        // Act
//        Result<ExecuteParametricSweepResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.True(result.Success);
//        Assert.Equal(StatusCode.OK, result.StatusCode);
//        Assert.NotNull(result.Data);
//        Assert.Equal(expectedIdentifier, result.Data.Identifier);
//        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<ParametricSweepInput>(), It.IsAny<CancellationToken>()), Times.Once);
//    }

//    /// <summary>
//    /// Verifies that when the pipeline fails, the command propagates the error status and messages.
//    /// </summary>
//    [Fact]
//    public async Task ExecuteAsync_WhenPipelineFails_ReturnsFailureWithPipelineStatusCodeAndMessages()
//    {
//        // Arrange
//        _validatorMock
//            .Setup(v => v.Validate(It.IsAny<ExecuteParametricSweepRequest>()))
//            .Returns(Result.CreateSuccessOk());

//        Result<string> badPipelineResult = Result.CreateBadRequest("Simulation diverging.");
//        _pipelineMock
//            .Setup(p => p.ProcessAsync(It.IsAny<ParametricSweepInput>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(badPipelineResult);

//        ExecuteParametricSweepCommand command = new(_pipelineMock.Object, _validatorMock.Object);
//        ExecuteParametricSweepRequest request = new()
//        {
//            MechanicalModelName = "Maxwell",
//            Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
//            BaseStrain = 0.05,
//            RampTimeConsideration = RampTimeConsideration.Disregard,
//            TimeStep = 0.1,
//            InitialTime = 0.0,
//            FinalTime = 10.0
//        };

//        // Act
//        Result<ExecuteParametricSweepResponse> result = await command.ExecuteAsync(request);

//        // Assert
//        Assert.False(result.Success);
//        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
//        Assert.Contains("Simulation diverging.", result.Messages);
//    }
//}
