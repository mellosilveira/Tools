using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using Xunit;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands.ProcessBatch;

public sealed class ExecuteBatchExperimentalDataPipelineValidatorTests
{
    private readonly ExecuteBatchExperimentalDataPipelineValidator _validator = new();

    [Fact]
    public void Validate_NullRequest_ReturnsBadRequest()
    {
        Result result = _validator.Validate(null!);

        Assert.False(result.Success);
        Assert.Contains("Batch experimental data processing request cannot be null.", result.Messages);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccessOk()
    {
        ExecuteBatchExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = "SimplifiedFung",
            TargetRSquared = 0.99,
            ConfidenceLevel = 0.95,
            ExecutionMode = PipelineExecutionMode.Sequential,
            Groups =
            [
                new ExperimentalDataGroupRequest
                {
                    Identifier = "Specimen_01",
                    StrainFileLocation = "path/to/strain.csv",
                    StressFileLocation = "path/to/stress.csv"
                }
            ]
        };

        Result result = _validator.Validate(request);

        Assert.True(result.Success);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Validate_EmptyGroups_ReturnsBadRequest()
    {
        ExecuteBatchExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = "SimplifiedFung",
            Groups = []
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Contains("Groups collection must not be null or empty.", result.Messages);
    }

    [Fact]
    public void Validate_InvalidRSquaredAndConfidenceLevel_ReturnsMultipleErrors()
    {
        ExecuteBatchExperimentalDataPipelineRequest request = new()
        {
            MechanicalModelName = string.Empty,
            TargetRSquared = 1.5,
            ConfidenceLevel = 0.0,
            Groups =
            [
                new ExperimentalDataGroupRequest
                {
                    StrainFileLocation = string.Empty,
                    StressFileLocation = " "
                }
            ]
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
        Assert.Contains("TargetRSquared must be within the interval (0, 1].", result.Messages);
        Assert.Contains("ConfidenceLevel must be strictly between 0 and 1 (e.g. 0.95 for 95% confidence).", result.Messages);
        Assert.Contains("Group at index 0 requires a non-empty StrainFileLocation.", result.Messages);
        Assert.Contains("Group at index 0 requires a non-empty StressFileLocation.", result.Messages);
    }
}
