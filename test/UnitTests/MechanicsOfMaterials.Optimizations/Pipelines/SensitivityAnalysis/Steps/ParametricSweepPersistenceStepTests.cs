using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Unit tests for <see cref="ParametricSweepPersistenceStep"/>, verifying database persistence of
/// <see cref="ParametricSweepEntity"/> and associated <see cref="StorageEntryMetadataEntity"/> records.
/// </summary>
public sealed class ParametricSweepPersistenceStepTests
{
    private readonly Mock<IRepository> _repositoryMock = new();

    /// <summary>
    /// Verifies that ExecuteAsync persists all generated file records, correlates each swept variable
    /// with its <see cref="RangeParameters"/> and <see cref="StorageEntryMetadata"/> inside <see cref="ParametricSweepEntity.SweptVariablesJson"/>,
    /// and returns the external identifier.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_PersistsEntityWithSweptVariablesDictionaryAndFiles()
    {
        // Arrange
        long nextFileId = 100L;
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result.CreateSuccessOk(++nextFileId));

        ParametricSweepEntity? capturedEntity = null;
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParametricSweepEntity, CancellationToken>((entity, _) => capturedEntity = entity)
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        string identifier = "sweep_pers_01";
        RangeParameters strainRange = new() { InitialPoint = 0.02, FinalPoint = 0.06, Step = 0.02 };
        RangeParameters constantRange = new() { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 };
        RangeParameters arrayRange = new() { InitialPoint = 0.1, FinalPoint = 0.3, Step = 0.1 };

        ParametricSweepPipelineInput sweepInput = new()
        {
            Identifier = identifier,
            MechanicalModelName = "SimplifiedFung",
            Baseline = new SimplifiedFungConstitutiveParameters(80.0, 2.0, new PronySeries(0.4, [0.5, -0.1])),
            ConstantVariables = new Dictionary<string, RangeParameters>
            {
                ["ElasticStressConstant"] = constantRange
            },
            ArrayVariables = new Dictionary<string, Dictionary<int, RangeParameters>>
            {
                ["ReducedRelaxationFunction"] = new Dictionary<int, RangeParameters>
                {
                    [0] = arrayRange
                }
            },
            BaseStrain = 0.05,
            StrainRange = strainRange,
            RampTimeConsideration = RampTimeConsideration.Disregard,
            InitialTime = 0.0,
            FinalTime = 10.0,
            TimeStep = 0.1,
            RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
            BaseRootFindingInput = new RootFindingInput(0.0, 1000.0, 1e-5, 1000)
        };

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object,
            sweepInput);

        StorageEntryMetadata file1 = new(StorageProvider.Local, @"C:\Data\sweep_pers_01\SensitivityAnalysis\Strain.csv", @"C:\Data\sweep_pers_01\SensitivityAnalysis", "Strain.csv", 1024L, DateTimeOffset.UtcNow);
        StorageEntryMetadata file2 = new(StorageProvider.Local, @"C:\Data\sweep_pers_01\SensitivityAnalysis\ElasticStressConstant.csv", @"C:\Data\sweep_pers_01\SensitivityAnalysis", "ElasticStressConstant.csv", 2048L, DateTimeOffset.UtcNow);
        StorageEntryMetadata file3 = new(StorageProvider.Local, @"C:\Data\sweep_pers_01\SensitivityAnalysis\ReducedRelaxationFunction[0].csv", @"C:\Data\sweep_pers_01\SensitivityAnalysis", "ReducedRelaxationFunction[0].csv", 3072L, DateTimeOffset.UtcNow);

        // Act
        string r1 = await step.ExecuteAsync(("Strain", strainRange, file1));
        string r2 = await step.ExecuteAsync(("ElasticStressConstant", constantRange, file2));
        string r3 = await step.ExecuteAsync(("ReducedRelaxationFunction[0]", arrayRange, file3));
        ParametricSweepEntity entity = await step.PersistSweepEntityAsync();

        // Assert
        Assert.Equal(identifier, r1);
        Assert.Equal(identifier, r2);
        Assert.Equal(identifier, r3);
        Assert.NotNull(capturedEntity);
        Assert.False(string.IsNullOrWhiteSpace(capturedEntity.Identifier));
        Assert.Equal(identifier, capturedEntity.ExternalIdentifier);
        Assert.Equal("SimplifiedFung", capturedEntity.MechanicalModelName);
        Assert.Equal(0.05, capturedEntity.BaseStrain);
        Assert.Equal(RampTimeConsideration.Disregard, capturedEntity.RampTimeConsideration);
        Assert.Equal(0.0, capturedEntity.InitialTime);
        Assert.Equal(10.0, capturedEntity.FinalTime);
        Assert.Equal(0.1, capturedEntity.TimeStep);
        Assert.Equal(RootFindingAlgorithm.StepByStepMethod, capturedEntity.RootFindingAlgorithm);
        Assert.False(string.IsNullOrWhiteSpace(capturedEntity.BaseRootFindingInputJson));
        Assert.False(string.IsNullOrWhiteSpace(capturedEntity.BaselineParametersJson));

        Dictionary<string, SweptVariableFileMetadata>? sweptVariables =
            JsonSerializer.Deserialize<Dictionary<string, SweptVariableFileMetadata>>(capturedEntity.SweptVariablesJson);
        Assert.NotNull(sweptVariables);
        Assert.Equal(3, sweptVariables.Count);

        Assert.Equal(strainRange, sweptVariables["Strain"].Range);
        Assert.Equal(file1.Location, sweptVariables["Strain"].FileMetadata.Location);

        Assert.Equal(constantRange, sweptVariables["ElasticStressConstant"].Range);
        Assert.Equal(file2.Location, sweptVariables["ElasticStressConstant"].FileMetadata.Location);

        Assert.Equal(arrayRange, sweptVariables["ReducedRelaxationFunction[0]"].Range);
        Assert.Equal(file3.Location, sweptVariables["ReducedRelaxationFunction[0]"].FileMetadata.Location);

        _repositoryMock.Verify(
            r => r.TryInsertAsync(
                It.Is<StorageEntryMetadataEntity>(f =>
                    f.ExternalIdentifier == identifier &&
                    f.StorageProvider == StorageProvider.Local &&
                    f.ContentType == "text/csv" &&
                    f.SizeBytes > 0),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that when the sweep entity insertion encounters a conflict (already exists),
    /// the step logs a warning and completes without throwing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEntityInsertConflicts_LogsWarningAndReturnsIdentifier()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateConflict(1L, "Entity already exists."));

        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(42L));

        string identifier = "sweep_pers_conflict";
        ParametricSweepPipelineInput sweepInput = CreateMaxwellSweepInput(identifier);

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object,
            sweepInput);

        RangeParameters range = new() { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 };
        StorageEntryMetadata file = new(StorageProvider.Local, @"C:\Data\sweep_pers_conflict\Stiffness.csv", @"C:\Data\sweep_pers_conflict", "Stiffness.csv", 512L, DateTimeOffset.UtcNow);

        // Act
        string result = await step.ExecuteAsync(("Stiffness", range, file));
        ParametricSweepEntity entity = await step.PersistSweepEntityAsync();

        // Assert
        Assert.Equal(identifier, result);
        Assert.NotNull(entity);
        _repositoryMock.Verify(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that when the sweep entity insertion fails with an error, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenEntityInsertFails_ThrowsInvalidOperationException()
    {
        // Arrange
        Result<long> badRequestResult = Result.CreateBadRequest("Database constraint violation.");
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badRequestResult);

        ParametricSweepPipelineInput sweepInput = CreateMaxwellSweepInput("sweep_fail");

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object,
            sweepInput);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => step.PersistSweepEntityAsync());
        Assert.Contains("Database constraint violation.", exception.Message);
    }

    /// <summary>
    /// Verifies that when stored file insertion fails with a non-conflict error, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenStoredFileInsertFails_ThrowsInvalidOperationException()
    {
        // Arrange
        Result<long> badRequestResult = Result.CreateBadRequest("Disk error saving metadata.");
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<StorageEntryMetadataEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(badRequestResult);

        ParametricSweepPipelineInput sweepInput = CreateMaxwellSweepInput("sweep_file_fail");

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object,
            sweepInput);

        RangeParameters range = new() { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 };
        StorageEntryMetadata file = new(StorageProvider.Local, @"C:\Data\sweep_file_fail\Stiffness.csv", @"C:\Data\sweep_file_fail", "Stiffness.csv", 512L, DateTimeOffset.UtcNow);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(("Stiffness", range, file)));
        Assert.Contains("Disk error saving metadata.", exception.Message);
    }

    /// <summary>
    /// Verifies that DisposeAsync completes cleanly without errors.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CompletesSuccessfully()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.CreateSuccessOk(1L));

        ParametricSweepPipelineInput sweepInput = CreateMaxwellSweepInput("sweep_dispose");

        ParametricSweepPersistenceStep step = new(
            NullLogger<ParametricSweepPersistenceStep>.Instance,
            _repositoryMock.Object,
            sweepInput);

        // Act & Assert
        await step.DisposeAsync();
        _repositoryMock.Verify(r => r.TryInsertAsync(It.IsAny<ParametricSweepEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ParametricSweepPipelineInput CreateMaxwellSweepInput(string identifier) => new()
    {
        Identifier = identifier,
        MechanicalModelName = "Maxwell",
        Baseline = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 10.0 },
        ConstantVariables = new Dictionary<string, RangeParameters>
        {
            ["Stiffness"] = new RangeParameters { InitialPoint = 50.0, FinalPoint = 150.0, Step = 50.0 }
        },
        ArrayVariables = [],
        BaseStrain = 0.05,
        RampTimeConsideration = RampTimeConsideration.Disregard,
        InitialTime = 0.0,
        FinalTime = 5.0,
        TimeStep = 0.1,
        RootFindingAlgorithm = RootFindingAlgorithm.StepByStepMethod,
        BaseRootFindingInput = new RootFindingInput(0.0, 500.0, 1e-4, 500)
    };
}
