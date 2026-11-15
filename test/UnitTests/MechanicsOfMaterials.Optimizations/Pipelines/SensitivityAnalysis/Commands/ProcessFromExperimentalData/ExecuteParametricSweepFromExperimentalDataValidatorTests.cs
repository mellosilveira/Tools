using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;
using Xunit;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Commands.ProcessFromExperimentalData;

public sealed class ExecuteParametricSweepFromExperimentalDataValidatorTests
{
    private readonly ExecuteParametricSweepFromExperimentalDataValidator _validator = new();

    [Fact]
    public void Validate_NullRequest_ReturnsBadRequest()
    {
        Result result = _validator.Validate(null!);

        Assert.False(result.Success);
        Assert.Contains("Parametric sweep request cannot be null.", result.Messages);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccessOk()
    {
        ExecuteParametricSweepFromExperimentalDataRequest request = new()
        {
            ExperimentalDataIdentifier = "EXP_RUN_123",
            MechanicalModelName = "SimplifiedFung",
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
            TimeStep = 0.1,
            InitialTime = 0.0,
            FinalTime = 100.0,
            ExecutionMode = PipelineExecutionMode.Sequential,
            RootFindingAlgorithm = RootFindingAlgorithm.BrentMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 100.0, 1e-6, 100),
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["ElasticStressConstant"] = new RangeParameters { InitialPoint = 10.0, FinalPoint = 20.0, Step = 5.0 }
            }
        };

        Result result = _validator.Validate(request);

        Assert.True(result.Success);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Validate_MissingIdentifierAndInvalidTimes_ReturnsValidationErrors()
    {
        ExecuteParametricSweepFromExperimentalDataRequest request = new()
        {
            ExperimentalDataIdentifier = string.Empty,
            MechanicalModelName = "Fung",
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            TimeStep = -1.0,
            InitialTime = 10.0,
            FinalTime = 5.0,
            ExecutionMode = PipelineExecutionMode.Sequential,
            RootFindingAlgorithm = RootFindingAlgorithm.BrentMethod,
            BaseRootFindingInput = null!
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Contains("ExperimentalDataIdentifier is required.", result.Messages);
        Assert.Contains("TimeStep must be greater than zero.", result.Messages);
        Assert.Contains("FinalTime must be greater than InitialTime.", result.Messages);
        Assert.Contains("BaseRootFindingInput is required.", result.Messages);
        Assert.Contains("At least one swept variable (ConstantVariables, ArrayVariables, or StrainRange) must be provided.", result.Messages);
    }
}
