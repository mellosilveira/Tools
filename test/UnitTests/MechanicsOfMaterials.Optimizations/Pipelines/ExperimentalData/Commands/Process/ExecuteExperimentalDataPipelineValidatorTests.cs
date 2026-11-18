using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands.Process;

public sealed class ExecuteExperimentalDataPipelineValidatorTests
{
    private readonly ExecuteExperimentalDataPipelineValidator _validator = new();

    [Fact]
    public void Validate_NullRequest_ReturnsBadRequest()
    {
        Result result = _validator.Validate(null!);

        Assert.False(result.Success);
        Assert.Contains("Experimental data processing request cannot be null.", result.Messages);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccessOk()
    {
        ExecuteExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = "Fung",
            TargetRSquared = 0.99,
            ConfidenceLevel = 0.95,
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                StrainFileLocation = "path/to/strain.csv",
                StressFileLocation = "path/to/stress.csv"
            }
        };

        Result result = _validator.Validate(request);

        Assert.True(result.Success);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Validate_MissingModelAndFiles_ReturnsValidationErrors()
    {
        ExecuteExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = string.Empty,
            TargetRSquared = -0.5,
            ConfidenceLevel = 1.0,
            ExperimentalDataGroupRequest = new ExperimentalDataGroupRequest
            {
                StrainFileLocation = string.Empty,
                StressFileLocation = string.Empty
            }
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
        Assert.Contains("StrainFileLocation must be provided.", result.Messages);
        Assert.Contains("StressFileLocation must be provided.", result.Messages);
        Assert.Contains("TargetRSquared must be within the interval (0, 1].", result.Messages);
        Assert.Contains("ConfidenceLevel must be strictly between 0 and 1 (e.g. 0.95 for 95% confidence).", result.Messages);
    }
}
