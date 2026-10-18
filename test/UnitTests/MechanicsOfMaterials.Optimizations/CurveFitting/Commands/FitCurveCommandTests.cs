using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Unit tests for <see cref="FitCurveCommand"/> verifying command execution, validation short-circuiting,
/// step factory resolution, CSV file reading, and listed output collection.
/// </summary>
public sealed class FitCurveCommandTests
{
    private readonly Mock<IMechanicalModelStepFactory> _stepFactoryMock = new();
    private readonly Mock<IValidator<FitCurveRequest>> _validatorMock = new();

    /// <summary>
    /// Verifies that when request validation fails, the command short-circuits and does not invoke the step factory.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShortCircuitsWithoutCallingFactory()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<FitCurveRequest>()))
            .Returns(Result.CreateBadRequest("MechanicalModelName is required."));

        FitCurveCommand command = new(_stepFactoryMock.Object, _validatorMock.Object);
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles = []
        };

        // Act
        ListedResult<MechanicalModelCurveFitOutput> result = await command.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
        _stepFactoryMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<SegmentType>>(), It.IsAny<CurveFitterType>(), It.IsAny<double>()), Times.Never);
    }

    /// <summary>
    /// Verifies that when valid segment CSV files are provided, the CSV rows are parsed, the step factory is called,
    /// segments and sentinel are executed, and outputs are returned as a listed result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenSegmentFilesProvided_ParsesCsvAndCallsStepAndReturnsListedOutputs()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<FitCurveRequest>()))
            .Returns(Result.CreateSuccessOk());

        byte[] csvBytes = Encoding.UTF8.GetBytes("Time,Strain,Stress\n0.0,0.01,10.0\n1.0,0.01,9.0\n");
        Mock<IFormFile> formFileMock = CreateMockFormFile(csvBytes);

        Mock<IMechanicalModelCurveFitterStep> fitterStepMock = new();
        fitterStepMock
            .Setup(s => s.ExecuteAsync(It.Is<CurveSegment>(cs => cs.Type == SegmentType.Relaxation), It.IsAny<CancellationToken>()))
            .Returns(GetMockFitOutputs);

        fitterStepMock
            .Setup(s => s.ExecuteAsync(It.Is<CurveSegment>(cs => cs.Type == SegmentType.Unknown), It.IsAny<CancellationToken>()))
            .Returns(GetEmptyOutputs);

        _stepFactoryMock
            .Setup(f => f.Create("Maxwell", It.IsAny<IReadOnlyList<SegmentType>>(), CurveFitterType.Alglib, 0.99))
            .Returns(fitterStepMock.Object);

        FitCurveCommand command = new(_stepFactoryMock.Object, _validatorMock.Object);
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [formFileMock.Object])
            ],
            TargetRSquared = 0.99
        };

        // Act
        ListedResult<MechanicalModelCurveFitOutput> result = await command.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Maxwell", result.Data[0].MechanicalModelName);
        Assert.Equal(0.995, result.Data[0].RSquared);

        fitterStepMock.Verify(s => s.ExecuteAsync(It.Is<CurveSegment>(cs => cs.Type == SegmentType.Relaxation && cs.TimePoints.Length == 2), It.IsAny<CancellationToken>()), Times.Once);
        fitterStepMock.Verify(s => s.ExecuteAsync(It.Is<CurveSegment>(cs => cs.Type == SegmentType.Unknown), It.IsAny<CancellationToken>()), Times.Once);
        fitterStepMock.Verify(s => s.DisposeAsync(), Times.Once);
    }

    private static Mock<IFormFile> CreateMockFormFile(byte[] content)
    {
        Mock<IFormFile> mock = new();
        mock.Setup(f => f.Length).Returns((long)content.Length);
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return mock;
    }

    private static async IAsyncEnumerable<MechanicalModelCurveFitOutput> GetMockFitOutputs()
    {
        await Task.Yield();
        yield return new MechanicalModelCurveFitOutput(
            CurveSegments: [],
            MechanicalModelName: "Maxwell",
            ConstitutiveParameters: new MaxwellConstitutiveParameters { Stiffness = 150.0, Viscosity = 15.0 },
            AcceptedRange: AcceptedRange.Default,
            LoadResponseRelationship: LoadResponseRelationship.StressStrain,
            ViscoelasticEffect: ViscoelasticEffect.Relaxation,
            RampTimeConsideration: RampTimeConsideration.Disregard,
            RSquared: 0.995,
            FinalError: 0.005,
            Iterations: 15);
    }

    private static async IAsyncEnumerable<MechanicalModelCurveFitOutput> GetEmptyOutputs()
    {
        await Task.Yield();
        yield break;
    }
}
