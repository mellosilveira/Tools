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
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Unit tests for <see cref="FitCurve"/> verifying command execution, validation short-circuiting,
/// step factory resolution, and output aggregation.
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

        FitCurve command = new(_stepFactoryMock.Object, _validatorMock.Object);
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Segments = []
        };

        // Act
        Result<FitCurveResultData> result = await command.ExecuteAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
        _stepFactoryMock.Verify(f => f.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<SegmentType>>(), It.IsAny<CurveFitterType>(), It.IsAny<double>()), Times.Never);
    }

    /// <summary>
    /// Verifies that when valid segments are provided, the step factory is called and outputs are aggregated.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenSegmentsProvided_CallsStepAndAggregatesTotalErrorAndParameterGroups()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<FitCurveRequest>()))
            .Returns(Result.CreateSuccessOk());

        Mock<IMechanicalModelCurveFitterStep> fitterStepMock = new();
        fitterStepMock
            .Setup(s => s.ExecuteAsync(It.IsAny<CurveSegment>(), It.IsAny<CancellationToken>()))
            .Returns(GetMockFitOutputs);

        _stepFactoryMock
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<SegmentType>>(), It.IsAny<CurveFitterType>(), It.IsAny<double>()))
            .Returns(fitterStepMock.Object);

        FitCurve command = new(_stepFactoryMock.Object, _validatorMock.Object);
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Segments =
            [
                new CurveSegment
                {
                    Type = SegmentType.Relaxation,
                    TimePoints = [0.0, 1.0],
                    ExperimentalStrain = [0.01, 0.01],
                    ExperimentalStress = [10.0, 9.0]
                }
            ],
            TargetRSquared = 0.99
        };

        // Act
        Result<FitCurveResultData> result = await command.ExecuteAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0.005, result.Data.FinalError, precision: 6);
        Assert.Single(result.Data.ParameterGroups);
        Assert.Equal(0.995, result.Data.ParameterGroups[0].RSquared);
        Assert.Equal(15, result.Data.ParameterGroups[0].Iterations);

        fitterStepMock.Verify(s => s.DisposeAsync(), Times.Once);
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
}
