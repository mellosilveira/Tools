using AcceptanceTests;
using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.SensitivityAnalysis;

/// <summary>
/// End-to-end acceptance tests for the One-At-A-Time (OAAT) Parametric Sweep sensitivity analysis pipeline.
/// Validates execution topology, forward mechanical simulations, non-throwing root finding, physical CSV output, and metadata persistence.
/// </summary>
public sealed class ParametricSweepPipelineAcceptanceTests
{
    private readonly ExecuteParametricSweepPipeline _command = GlobalServiceProvider.Provider.GetRequiredService<ExecuteParametricSweepPipeline>();
    private readonly IPipeline<ParametricSweepPipelineInput, string> _pipeline = GlobalServiceProvider.Provider.GetRequiredService<IPipeline<ParametricSweepPipelineInput, string>>();
    private readonly IFileStorageService _fileStorageService = GlobalServiceProvider.Provider.GetRequiredService<IFileStorageService>();

    /// <summary>
    /// Verifies that executing a parametric sweep for the Simplified Fung model correctly produces partitioned CSV files on disk with valid numeric results.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WithSimplifiedFung_ShouldRunFullSweepPipelineAndGenerateCsvs()
    {
        // Arrange
        string testIdentifier = $"sweep_simplified_fung_{Guid.NewGuid():N}";
        PronySeries prony = new(
            independentParameter: 0.5,
            iteratorCoefficients: [0.3, 10.0, 0.2, 50.0],
            initialVariableValue: 0.0,
            finalVariableValue: 100.0);

        SimplifiedFungConstitutiveParameters baseline = new()
        {
            ElasticStressConstant = 10.0,
            ElasticPowerConstant = 5.0,
            ReducedRelaxationFunction = prony
        };

        ParametricSweepPipelineInput input = new()
        {
            Identifier = testIdentifier,
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            Baseline = baseline,
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                [nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant)] = new()
                {
                    InitialPoint = 5.0,
                    FinalPoint = 15.0,
                    Step = 5.0
                }
            },
            ArrayVariables = [],
            StrainRange = new RangeParameters
            {
                InitialPoint = 0.02,
                FinalPoint = 0.04,
                Step = 0.02
            },
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 10.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(InitialPoint: 0.0, FinalPoint: 10.0, Tolerance: 1e-2, MaxIterations: 50)
        };

        // Act
        Result<string> result = await _pipeline.ProcessAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success, result.JoinedMessages);
        Assert.Equal(testIdentifier, result.Data);

        List<StorageEntryMetadata> generatedFiles = [];
        await foreach (StorageEntryMetadata file in _fileStorageService.ListFilesAsync(testIdentifier, "SensitivityAnalysis", CancellationToken.None))
        {
            generatedFiles.Add(file);
        }

        // Two variables were swept: ElasticStressConstant and Strain
        Assert.True(generatedFiles.Count >= 2, $"Expected at least 2 generated CSV files, found {generatedFiles.Count}");

        foreach (StorageEntryMetadata fileMetadata in generatedFiles)
        {
            Assert.True(File.Exists(fileMetadata.Location), $"File does not exist: {fileMetadata.Location}");
            Assert.True(fileMetadata.SizeBytes > 0, $"File size must be positive: {fileMetadata.Location}");

            string[] lines = await File.ReadAllLinesAsync(fileMetadata.Location);
            Assert.True(lines.Length >= 2, $"File {fileMetadata.Location} must contain at least header and one data row.");
            Assert.Equal("VariableValue,InitialStress,FinalStress,AbsoluteDeltaStress,PercentageDeltaStress,AsymptoteTime", lines[0]);

            // Validate that data rows contain 6 numeric values or NaN
            string[] columns = lines[1].Split(',');
            Assert.Equal(6, columns.Length);
            foreach (string col in columns)
            {
                bool isNumeric = double.TryParse(col, System.Globalization.CultureInfo.InvariantCulture, out _) || col == "NaN";
                Assert.True(isNumeric, $"Column '{col}' is not a valid number or NaN.");
            }
        }
    }

    /// <summary>
    /// Verifies that when root finding fails to locate a relaxation asymptote within tolerance,
    /// the pipeline does not throw an exception, completes successfully, and formats AsymptoteTime as NaN in the generated CSV.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WhenRootFindingDoesNotConverge_ShouldCompleteWithoutThrowingAndOutputNanForAsymptote()
    {
        // Arrange
        string testIdentifier = $"sweep_nonconverge_{Guid.NewGuid():N}";
        PronySeries prony = new(
            independentParameter: 0.5,
            iteratorCoefficients: [0.3, 10.0],
            initialVariableValue: 0.0,
            finalVariableValue: 100.0);

        SimplifiedFungConstitutiveParameters baseline = new()
        {
            ElasticStressConstant = 10.0,
            ElasticPowerConstant = 5.0,
            ReducedRelaxationFunction = prony
        };

        // Using an impossible tolerance and 1 iteration so asymptote detection fails to converge
        ParametricSweepPipelineInput input = new()
        {
            Identifier = testIdentifier,
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            Baseline = baseline,
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                [nameof(SimplifiedFungConstitutiveParameters.ElasticStressConstant)] = new()
                {
                    InitialPoint = 10.0,
                    FinalPoint = 10.0,
                    Step = 1.0
                }
            },
            ArrayVariables = [],
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 10.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(InitialPoint: 0.0, FinalPoint: 0.01, Tolerance: 1e-15, MaxIterations: 1)
        };

        // Act
        Result<string> result = await _pipeline.ProcessAsync(input, CancellationToken.None);

        // Assert - pipeline must NOT throw and must complete successfully
        Assert.True(result.Success, result.JoinedMessages);

        List<StorageEntryMetadata> generatedFiles = [];
        await foreach (StorageEntryMetadata file in _fileStorageService.ListFilesAsync(testIdentifier, "SensitivityAnalysis", CancellationToken.None))
        {
            generatedFiles.Add(file);
        }

        Assert.Single(generatedFiles);
        string[] lines = await File.ReadAllLinesAsync(generatedFiles[0].Location);
        Assert.True(lines.Length >= 2);
        string[] columns = lines[1].Split(',');
        Assert.Equal(6, columns.Length);
        Assert.Equal("NaN", columns[5]); // AsymptoteTime column is NaN
    }

    /// <summary>
    /// Verifies that executing a parametric sweep for the standard Fung model correctly generates sensitivity CSVs.
    /// </summary>
    [Fact]
    public async Task ProcessAsync_WithFungModel_ShouldExecuteAndGenerateSweepFiles()
    {
        // Arrange
        string testIdentifier = $"sweep_fung_{Guid.NewGuid():N}";
        FungConstitutiveParameters baseline = new(
            elasticStressConstant: 12.0,
            elasticPowerConstant: 4.5,
            reducedRelaxationFunction: new ReducedRelaxationFunction(0.15, 0.02, 80.0));

        ParametricSweepPipelineInput input = new()
        {
            Identifier = testIdentifier,
            MechanicalModelName = nameof(MechanicalModel.Fung),
            Baseline = baseline,
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                [nameof(FungConstitutiveParameters.ElasticStressConstant)] = new()
                {
                    InitialPoint = 6.0,
                    FinalPoint = 18.0,
                    Step = 6.0
                }
            },
            ArrayVariables = [],
            BaseStrain = 0.04,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.5,
            FinalTime = 10.0,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(InitialPoint: 0.0, FinalPoint: 10.0, Tolerance: 1e-2, MaxIterations: 50)
        };

        // Act
        Result<string> result = await _pipeline.ProcessAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success, result.JoinedMessages);

        List<StorageEntryMetadata> generatedFiles = [];
        await foreach (StorageEntryMetadata file in _fileStorageService.ListFilesAsync(testIdentifier, "SensitivityAnalysis", CancellationToken.None))
        {
            generatedFiles.Add(file);
        }

        Assert.Single(generatedFiles);
        Assert.True(File.Exists(generatedFiles[0].Location));
        Assert.True(generatedFiles[0].SizeBytes > 0);
    }

    /// <summary>
    /// Verifies that the application command validates requests and rejects invalid input payloads.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Command_WithInvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange - missing baseline and variables
        ExecuteParametricSweepPipelineRequest invalidRequest = new()
        {
            MechanicalModelName = string.Empty,
            Baseline = null!,
            BaseStrain = 0.05,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            TimeStep = 0.0, // Invalid: must be > 0
            FinalTime = -1.0, // Invalid: must be > InitialTime
            RootFindingAlgorithm = (RootFindingAlgorithm)999, // Invalid enum
            BaseRootFindingInput = null!
        };

        // Act
        Result<string> result = await _command.ExecuteAsync(invalidRequest, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.NotEmpty(result.Messages);
    }
}
